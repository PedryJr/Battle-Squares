// WindowManager.cs
// Drop into an "Editor" or "Runtime" folder in Unity project (so it compiles for players).
// Designed for Windows (Win32). Works with Vulkan backend by using borderless fullscreen
// to avoid swapchain destruction/recreation flicker that exclusive fullscreen sometimes causes.

using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class WindowManager : MonoBehaviour
{
    public enum WindowState
    {
        Fullscreen,   // Borderless fullscreen by default (safe for Vulkan)
        Borderless,   // Windowed borderless (not necessarily fullscreen)
        Windowed      // Normal window with chrome
    }

    // -- Public API --------------------------------------------------------
    public static WindowManager Instance { get; private set; }

    [Header("Behavior")]
    [Tooltip("If true, Fullscreen will use ExclusiveFullScreen (may flicker). Otherwise Fullscreen uses borderless fullscreen (recommended).")]
    public bool fullscreenUseExclusive = false;

    [Tooltip("If true, attempt to make the window top-most in borderless fullscreen")]
    public bool borderlessTopMost = false;

    // internal state
    public WindowState CurrentState => _initialized ? _currentState : WindowState.Windowed;

    // keep previous windowed rect to restore when leaving fullscreen/borderless
    private RectInt _savedWindowedRect;
    private bool _hasSavedWindowedRect = false;

    // state machine guards
    private WindowState _currentState = WindowState.Windowed;
    private bool _initialized = false;

    // only run on windows builds
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    private IntPtr _hwnd = IntPtr.Zero;
#endif

    // ---------------------------------------------------------------------
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(this.gameObject);

        Initialize();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Initialize()
    {
        if (_initialized) return;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        _hwnd = GetGameWindowHandle();
#endif



        // start with whatever Unity reports, but unify to our enum
        if (Application.isEditor)
        {
            _currentState = WindowState.Windowed;
        }
        else
        {

            WindowState grabState = WindowState.Windowed;

            if (Screen.fullScreenMode == FullScreenMode.ExclusiveFullScreen) { grabState = WindowState.Fullscreen; }
            if (Screen.fullScreenMode == FullScreenMode.FullScreenWindow) { grabState = WindowState.Borderless; }
            if (Screen.fullScreenMode == FullScreenMode.Windowed) { grabState = WindowState.Windowed; }
            if (Screen.fullScreenMode == FullScreenMode.MaximizedWindow) { grabState = WindowState.Windowed; }

            _currentState = grabState;
            // if using fullscreen exclusive fallback to Windowed
            //_currentState = WindowState.Windowed;
        }

        // record initial windowed rect
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        TrySaveWindowRect();
#endif

        _initialized = true;
    }

    /// <summary>
    /// Set the desired window state. If it's already the current state, nothing happens.
    /// </summary>
    public void SetState(WindowState newState, bool forceApply = false)
    {
        Initialize();

        if (!forceApply) if (_currentState == newState) return;

        if (_currentState == WindowState.Windowed)
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            TrySaveWindowRect();
#endif
        }

        // perform isolated transition
        ApplyState(newState);

        _currentState = newState;
    }

    // ----------------------- Implementation --------------------------------

    private void ApplyState(WindowState state)
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        if (_hwnd == IntPtr.Zero)
            _hwnd = GetGameWindowHandle();

        switch (state)
        {
            case WindowState.Fullscreen:
                if (fullscreenUseExclusive)
                {
                    // Try exclusive fullscreen via Unity (may recreate swapchain -> risk)
                    if (Screen.fullScreenMode != FullScreenMode.ExclusiveFullScreen)
                    {
                        Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
                    }
                }
                else
                {
                    // Safer approach for Vulkan: borderless fullscreen on monitor containing the window.
                    ApplyBorderlessFullscreen();
                }
                break;

            case WindowState.Borderless:
                ApplyBorderlessWindow();
                break;

            case WindowState.Windowed:
                ApplyWindowed();
                break;
        }
#else
        // Non Windows platforms: use Unity API best-effort
        switch (state)
        {
            case WindowState.Fullscreen:
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                break;
            case WindowState.Borderless:
                Screen.fullScreenMode = FullScreenMode.Windowed; // unity doesn't have dedicated borderless API on mac/linux
                break;
            case WindowState.Windowed:
                Screen.fullScreenMode = FullScreenMode.Windowed;
                break;
        }
#endif
    }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN

    private void ApplyWindowed()
    {
        // restore window style & position
        RestoreWindowStyleAndPosition();
        // ensure unity knows it's windowed (keeps internal state consistent)
        if (Screen.fullScreenMode != FullScreenMode.Windowed)
            Screen.fullScreenMode = FullScreenMode.Windowed;
    }

    private void ApplyBorderlessWindow()
    {
        // Make window borderless but do not resize to fullscreen bounds.
        // Use current window size and remove chrome.
        RemoveWindowChrome(_hwnd);
        if (borderlessTopMost)
            SetTopMost(_hwnd, true);
        else
            SetTopMost(_hwnd, false);

        // keep the current size and position (no resizing)
    }

    private void ApplyBorderlessFullscreen()
    {
        // Remove chrome and expand to monitor bounds (monitor that contains the window)
        var monInfo = GetMonitorInfoForWindow(_hwnd);
        if (monInfo.hasValue)
        {
            RemoveWindowChrome(_hwnd);
            SetTopMost(_hwnd, borderlessTopMost);

            // place the window exactly covering monitor work area or monitor area (we use monitor area)
            var rc = monInfo.rc.rcMonitor;
            // SetWindowPos with exact monitor bounds (no Z-order change)
            SetWindowPos(_hwnd, IntPtr.Zero, rc.left, rc.top,
                rc.right - rc.left, rc.bottom - rc.top,
                SWP_NOZORDER | SWP_FRAMECHANGED);
        }
        else
        {
            // fallback - remove chrome but do not resize
            RemoveWindowChrome(_hwnd);
        }

        // Keep Unity mode windowed so we don't force swapchain re-creation
        if (Screen.fullScreenMode != FullScreenMode.Windowed)
            Screen.fullScreenMode = FullScreenMode.Windowed;
    }

    private void RestoreWindowStyleAndPosition()
    {
        // Restore window style to normal and restore saved rect if we have it
        AddWindowChrome(_hwnd);
        SetTopMost(_hwnd, false);

        if (_hasSavedWindowedRect)
        {
            // convert our saved RectInt to integers for SetWindowPos
            int w = _savedWindowedRect.width;
            int h = _savedWindowedRect.height;
            int x = _savedWindowedRect.x;
            int y = _savedWindowedRect.y;

            SetWindowPos(_hwnd, IntPtr.Zero, x, y, w, h, SWP_NOZORDER | SWP_FRAMECHANGED);
        }
    }

    private void TrySaveWindowRect()
    {
        if (_hwnd == IntPtr.Zero) return;

        if (GetWindowRect(_hwnd, out RECT rc))
        {
            _savedWindowedRect = new RectInt(rc.left, rc.top, rc.right - rc.left, rc.bottom - rc.top);
            _hasSavedWindowedRect = true;
        }
    }

    private void RemoveWindowChrome(IntPtr hwnd)
    {
        uint style = GetWindowLongPtr(hwnd, GWL_STYLE);
        style &= ~(WS_CAPTION | WS_THICKFRAME | WS_SYSMENU | WS_MINIMIZEBOX | WS_MAXIMIZEBOX);
        SetWindowLongPtr(hwnd, GWL_STYLE, style);

        uint exStyle = GetWindowLongPtr(hwnd, GWL_EXSTYLE);
        exStyle &= ~(WS_EX_DLGMODALFRAME | WS_EX_WINDOWEDGE | WS_EX_CLIENTEDGE | WS_EX_STATICEDGE);
        SetWindowLongPtr(hwnd, GWL_EXSTYLE, exStyle);

        // frame changed apply
        SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
    }

    private void AddWindowChrome(IntPtr hwnd)
    {
        uint style = GetWindowLongPtr(hwnd, GWL_STYLE);
        style |= (WS_CAPTION | WS_THICKFRAME | WS_SYSMENU | WS_MINIMIZEBOX | WS_MAXIMIZEBOX);
        SetWindowLongPtr(hwnd, GWL_STYLE, style);

        uint exStyle = GetWindowLongPtr(hwnd, GWL_EXSTYLE);
        exStyle |= (WS_EX_WINDOWEDGE | WS_EX_CLIENTEDGE);
        SetWindowLongPtr(hwnd, GWL_EXSTYLE, exStyle);

        SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
    }

    private void SetTopMost(IntPtr hwnd, bool topMost)
    {
        IntPtr insertAfter = topMost ? HWND_TOPMOST : HWND_NOTOPMOST;
        SetWindowPos(hwnd, insertAfter, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
    }

    // ---------------------------------------------------------------------
    // Win32 interop
    // ---------------------------------------------------------------------

    private const int GWL_STYLE = -16;
    private const int GWL_EXSTYLE = -20;

    private const uint WS_CAPTION = 0x00C00000;
    private const uint WS_THICKFRAME = 0x00040000;
    private const uint WS_SYSMENU = 0x00080000;
    private const uint WS_MINIMIZEBOX = 0x00020000;
    private const uint WS_MAXIMIZEBOX = 0x00010000;

    private const uint WS_EX_DLGMODALFRAME = 0x00000001;
    private const uint WS_EX_WINDOWEDGE = 0x00000100;
    private const uint WS_EX_CLIENTEDGE = 0x00000200;
    private const uint WS_EX_STATICEDGE = 0x00020000;

    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_FRAMECHANGED = 0x0020;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int left, top, right, bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public uint cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);
    private const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    // SetWindowPos used to reposition/resize quickly without changing z order unless requested.
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr hWnd, IntPtr hWndInsertAfter,
        int X, int Y, int cx, int cy, uint uFlags);

    // GWL helpers - keep 32/64-bit compatibility
    [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
    private static extern uint GetWindowLong32(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
    private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    private static uint GetWindowLongPtr(IntPtr hWnd, int nIndex)
    {
        if (IntPtr.Size == 8)
        {
            var ptr = GetWindowLongPtr64(hWnd, nIndex);
            return unchecked((uint)ptr.ToInt64());
        }
        else
        {
            return GetWindowLong32(hWnd, nIndex);
        }
    }

    [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
    private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    private static uint SetWindowLongPtr(IntPtr hWnd, int nIndex, uint newValue)
    {
        if (IntPtr.Size == 8)
        {
            var ret = SetWindowLongPtr64(hWnd, nIndex, new IntPtr(unchecked((int)newValue)));
            return unchecked((uint)ret.ToInt64());
        }
        else
        {
            return unchecked((uint)SetWindowLong32(hWnd, nIndex, unchecked((int)newValue)));
        }
    }

    private static IntPtr GetGameWindowHandle()
    {
        // Prefer active window (player build) otherwise fall back to foreground window.
        IntPtr hwnd = GetActiveWindow();
        if (hwnd == IntPtr.Zero)
            hwnd = GetForegroundWindow();
        return hwnd;
    }

    private static (bool hasValue, MONITORINFO rc) GetMonitorInfoForWindow(IntPtr hwnd)
    {
        IntPtr hMon = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
        if (hMon == IntPtr.Zero) return (false, default);

        MONITORINFO info = new MONITORINFO();
        info.cbSize = (uint)Marshal.SizeOf(typeof(MONITORINFO));
        bool ok = GetMonitorInfo(hMon, ref info);
        return (ok, info);
    }

#endif // UNITY_STANDALONE_WIN

    // ----------------------------- Utilities --------------------------------

    /// <summary>
    /// Convenience call to toggle states (cycles windowed->borderless->fullscreen)
    /// </summary>
    public void CycleState()
    {
        var next = _currentState switch
        {
            WindowState.Windowed => WindowState.Borderless,
            WindowState.Borderless => WindowState.Fullscreen,
            WindowState.Fullscreen => WindowState.Windowed,
            _ => WindowState.Windowed
        };
        SetState(next);
    }

    // ---------------------------- Debugging ---------------------------------
    [ContextMenu("Apply Borderless Fullscreen")]
    private void Debug_ApplyBorderlessFull()
    {
        SetState(WindowState.Fullscreen);
    }

    [ContextMenu("Apply Borderless Window")]
    private void Debug_ApplyBorderless()
    {
        SetState(WindowState.Borderless);
    }

    [ContextMenu("Apply Windowed")]
    private void Debug_ApplyWindowed()
    {
        SetState(WindowState.Windowed);
    }
}
