using BattleSquaresSDK;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using TMPro;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using UnityEngine;

[BurstCompile(CompileSynchronously = compileSynchronously, Debug = debug, DisableDirectCall = disableDirectCall,
    DisableSafetyChecks = disableSafetyChecks, FloatMode = floatMode,
    FloatPrecision = floatPrecision, OptimizeFor = optimizeFor)]
public class VLog : MonoBehaviour
{
    public const bool compileSynchronously = false;
    public const bool debug = false;
    public const bool disableDirectCall = false;
    public const bool disableSafetyChecks = true;
    public const FloatMode floatMode = FloatMode.Fast;
    public const FloatPrecision floatPrecision = FloatPrecision.Low;
    public const OptimizeFor optimizeFor = OptimizeFor.Performance;

    [SerializeField]
    LogElementAnimation logElement;

    [SerializeField]
    int maxLogs = 10;

    static VLog instance;
    static Queue<LogElementAnimation> activeElements = new Queue<LogElementAnimation>();

    private void Awake()
    {
        instance = this;
        DontDestroyOnLoad(transform.parent);
    }

    public static void Log(string message)
    {
        while (activeElements.Count >= instance.maxLogs)
        {
            LogElementAnimation oldest = activeElements.Dequeue();
            if (oldest != null) oldest.PrematureFadeout();
        }

        LogElementAnimation newLog = Instantiate(instance.logElement, instance.transform);
        newLog.text.text = Format(message);
        newLog.onExpire = ElementExpired;
        activeElements.Enqueue(newLog);

        Debug.Log(message);
    }

    public static void Log(string message, float duration)
    {

        while (activeElements.Count >= instance.maxLogs)
        {
            LogElementAnimation oldest = activeElements.Dequeue();
            if (oldest != null) oldest.PrematureFadeout();
        }

        LogElementAnimation newLog = Instantiate(instance.logElement, instance.transform);
        newLog.text.text = Format(message);
        newLog.onExpire = ElementExpired;
        newLog.timeToStay = duration;
        activeElements.Enqueue(newLog);

        Debug.Log(message);
    }

    static void ElementExpired(LogElementAnimation element)
    {
        if (activeElements.Contains(element))
        {
            Queue<LogElementAnimation> temp = new Queue<LogElementAnimation>();
            while (activeElements.Count > 0)
            {
                LogElementAnimation item = activeElements.Dequeue();
                if (item != element) temp.Enqueue(item);
            }
            activeElements = temp;
        }
    }

    /*    private static readonly Dictionary<char, string> ColorMap = new()
        {
            ['0'] = "#000000", ['1'] = "#0000AA", ['2'] = "#00AA00", ['3'] = "#00AAAA",
            ['4'] = "#AA0000", ['5'] = "#AA00AA", ['6'] = "#FFAA00", ['7'] = "#AAAAAA",
            ['8'] = "#555555", ['9'] = "#5555FF", ['a'] = "#55FF55", ['b'] = "#55FFFF",
            ['c'] = "#FF5555", ['d'] = "#FF55FF", ['e'] = "#FFFF55", ['f'] = "#FFFFFF",
            ['g'] = "#DDD605", ['h'] = "#E3D4D1", ['i'] = "#CECACA", ['j'] = "#443A3B",
            ['m'] = "#971607", ['n'] = "#B4684D", ['p'] = "#DEB12D", ['q'] = "#47A036",
            ['s'] = "#2CBAA8", ['t'] = "#21497B", ['u'] = "#9A5CC6", ['v'] = "#EB7114"
        };

        public static string Format(string input)
        {

            if (string.IsNullOrEmpty(input))
                return input;

            var sb = new StringBuilder(input.Length * 2);
            var openTags = new Stack<string>(8);

            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == '§' && i + 1 < input.Length)
                {
                    char code = input[++i];

                    if (code == 'r')
                    {
                        while (openTags.Count > 0)
                            sb.Append(openTags.Pop());
                    }
                    else if (ColorMap.TryGetValue(code, out string hex))
                    {
                        sb.Append("<color=").Append(hex).Append('>');
                        openTags.Push("</color>");
                    }
                    else
                    {
                        var tag = code switch
                        {
                            'l' => ("</b>", "<b>"),
                            'm' => ("</s>", "<s>"),
                            'n' => ("</u>", "<u>"),
                            'o' => ("</i>", "<i>"),
                            _ => (null, null)
                        };

                        if (tag.Item1 != null)
                        {
                            sb.Append(tag.Item2);
                            openTags.Push(tag.Item1);
                        }
                    }
                }
                else
                {
                    sb.Append(input[i]);
                }
            }

            while (openTags.Count > 0)
                sb.Append(openTags.Pop());

            return sb.ToString();
        }*/

    public static unsafe string Format(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        int inputLen = input.Length;

        char* output = stackalloc char[inputLen * 32];
        char* closeTags = stackalloc char[inputLen * 8];

        int outPos = 0;
        int closePos = 0;

        fixed (char* inputPtr = input) FormatPtr(inputLen, (ushort*)output, (ushort*)closeTags, (ushort*)inputPtr, ref outPos, ref closePos);

        return new string(output, 0, outPos);
    }

    [BurstCompile(CompileSynchronously = compileSynchronously, Debug = debug, DisableDirectCall = disableDirectCall,
        DisableSafetyChecks = disableSafetyChecks, FloatMode = floatMode,
        FloatPrecision = floatPrecision, OptimizeFor = optimizeFor)]
    private static unsafe void FormatPtr(int inputLen, ushort* output, ushort* closeTags, ushort* inputPtr, ref int outPos, ref int closePos)
    {
        for (int i = 0; i < inputLen; i++)
        {
            ushort c = inputPtr[i];

            if ((c == '§' || c == '&') && i + 1 < inputLen)
            {
                ushort code = inputPtr[++i];

                if (code == 'r')
                {
                    for (int j = closePos - 1; j >= 0; j--) output[outPos++] = closeTags[j]; closePos = 0;
                }
                else
                {
                    bool isColor = false;

                    switch (code)
                    {
                        case '0':
                            WriteColor(output, ref outPos, '#', '0', '0', '0', '0', '0', '0');
                            isColor = true;
                            break;
                        case '1':
                            WriteColor(output, ref outPos, '#', '0', '0', '0', '0', 'A', 'A');
                            isColor = true;
                            break;
                        case '2':
                            WriteColor(output, ref outPos, '#', '0', '0', 'A', 'A', '0', '0');
                            isColor = true;
                            break;
                        case '3':
                            WriteColor(output, ref outPos, '#', '0', '0', 'A', 'A', 'A', 'A');
                            isColor = true;
                            break;
                        case '4':
                            WriteColor(output, ref outPos, '#', 'A', 'A', '0', '0', '0', '0');
                            isColor = true;
                            break;
                        case '5':
                            WriteColor(output, ref outPos, '#', 'A', 'A', '0', '0', 'A', 'A');
                            isColor = true;
                            break;
                        case '6':
                            WriteColor(output, ref outPos, '#', 'F', 'F', 'A', 'A', '0', '0');
                            isColor = true;
                            break;
                        case '7':
                            WriteColor(output, ref outPos, '#', 'A', 'A', 'A', 'A', 'A', 'A');
                            isColor = true;
                            break;
                        case '8':
                            WriteColor(output, ref outPos, '#', '5', '5', '5', '5', '5', '5');
                            isColor = true;
                            break;
                        case '9':
                            WriteColor(output, ref outPos, '#', '5', '5', '5', '5', 'F', 'F');
                            isColor = true;
                            break;
                        case 'a':
                            WriteColor(output, ref outPos, '#', '5', '5', 'F', 'F', '5', '5');
                            isColor = true;
                            break;
                        case 'b':
                            WriteColor(output, ref outPos, '#', '5', '5', 'F', 'F', 'F', 'F');
                            isColor = true;
                            break;
                        case 'c':
                            WriteColor(output, ref outPos, '#', 'F', 'F', '5', '5', '5', '5');
                            isColor = true;
                            break;
                        case 'd':
                            WriteColor(output, ref outPos, '#', 'F', 'F', '5', '5', 'F', 'F');
                            isColor = true;
                            break;
                        case 'e':
                            WriteColor(output, ref outPos, '#', 'F', 'F', 'F', 'F', '5', '5');
                            isColor = true;
                            break;
                        case 'f':
                            WriteColor(output, ref outPos, '#', 'F', 'F', 'F', 'F', 'F', 'F');
                            isColor = true;
                            break;
                        case 'g':
                            WriteColor(output, ref outPos, '#', 'D', 'D', 'D', '6', '0', '5');
                            isColor = true;
                            break;
                        case 'h':
                            WriteColor(output, ref outPos, '#', 'E', '3', 'D', '4', 'D', '1');
                            isColor = true;
                            break;
                        case 'i':
                            WriteColor(output, ref outPos, '#', 'C', 'E', 'C', 'A', 'C', 'A');
                            isColor = true;
                            break;
                        case 'j':
                            WriteColor(output, ref outPos, '#', '4', '4', '3', 'A', '3', 'B');
                            isColor = true;
                            break;
                        case 'm':
                            WriteColor(output, ref outPos, '#', '9', '7', '1', '6', '0', '7');
                            isColor = true;
                            break;
                        case 'n':
                            WriteColor(output, ref outPos, '#', 'B', '4', '6', '8', '4', 'D');
                            isColor = true;
                            break;
                        case 'p':
                            WriteColor(output, ref outPos, '#', 'D', 'E', 'B', '1', '2', 'D');
                            isColor = true;
                            break;
                        case 'q':
                            WriteColor(output, ref outPos, '#', '4', '7', 'A', '0', '3', '6');
                            isColor = true;
                            break;
                        case 's':
                            WriteColor(output, ref outPos, '#', '2', 'C', 'B', 'A', 'A', '8');
                            isColor = true;
                            break;
                        case 't':
                            WriteColor(output, ref outPos, '#', '2', '1', '4', '9', '7', 'B');
                            isColor = true;
                            break;
                        case 'u':
                            WriteColor(output, ref outPos, '#', '9', 'A', '5', 'C', 'C', '6');
                            isColor = true;
                            break;
                        case 'v':
                            WriteColor(output, ref outPos, '#', 'E', 'B', '7', '1', '1', '4');
                            isColor = true;
                            break;
                    }

                    if (isColor)
                    {
                        closeTags[closePos++] = '>';
                        closeTags[closePos++] = 'r';
                        closeTags[closePos++] = 'o';
                        closeTags[closePos++] = 'l';
                        closeTags[closePos++] = 'o';
                        closeTags[closePos++] = 'c';
                        closeTags[closePos++] = '/';
                        closeTags[closePos++] = '<';
                    }
                    else
                    {
                        switch (code)
                        {
                            case 'l':
                                output[outPos++] = '<';
                                output[outPos++] = 'b';
                                output[outPos++] = '>';
                                closeTags[closePos++] = '>';
                                closeTags[closePos++] = 'b';
                                closeTags[closePos++] = '/';
                                closeTags[closePos++] = '<';
                                break;

                            case 'o':
                                output[outPos++] = '<';
                                output[outPos++] = 'i';
                                output[outPos++] = '>';
                                closeTags[closePos++] = '>';
                                closeTags[closePos++] = 'i';
                                closeTags[closePos++] = '/';
                                closeTags[closePos++] = '<';
                                break;
                            default: output[outPos++] = c; break;
                        }
                    }
                }
            }
            else output[outPos++] = c;
        }

        for (int j = closePos - 1; j >= 0; j--) output[outPos++] = closeTags[j];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void WriteColor(ushort* buffer, ref int pos, ushort c0, ushort c1, ushort c2, ushort c3, ushort c4, ushort c5, ushort c6)
        {
            v256* asV256 = (v256*)(buffer + pos);
            asV256->ULong0 = 0x006C006F0063003CUL;
            asV256->ULong1 = ((ulong)c0 << 48) | 0x00003D0072006FUL;
            asV256->ULong2 = ((ulong)c4 << 48) | ((ulong)c3 << 32) | ((ulong)c2 << 16) | c1;
            asV256->ULong3 = ((uint)c6 << 16) | c5;
            buffer[pos + 14] = 62;
            pos += 15;
        }
    }


     
}