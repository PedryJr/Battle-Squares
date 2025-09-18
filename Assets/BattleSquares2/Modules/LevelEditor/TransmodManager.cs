using System.Collections.Generic;
using UnityEngine;

public sealed class TransmodManager : MonoBehaviour
{
    [SerializeField]
    float TransmodMaximumInteractionDistance = 1.5f;
    public static TransmodManager Singleton;
    public void AddTransmod(LevelTransMod instance) => transMods.Add(instance);
    public void RemoveTransmod(LevelTransMod instance) => transMods.Remove(instance);

    LevelTransMod lastUsedTransmod;
    List<LevelTransMod> transMods;
    EditActions editorInput;
    EditorMouseTool editorMouseTool;

    private void Awake()
    {
        lastUsedTransmod = null;
        Singleton = this;
        transMods = new List<LevelTransMod>();
        editorInput = new EditActions();
        editorMouseTool = new EditorMouseTool();

        editorInput.Mouse.ScreenPositionChange.performed +=
            (inputData) => editorMouseTool.UpdateRawScreenSpacePosition(inputData.ReadValue<Vector2>());

        editorInput.Mouse.ScreenPositionChange.canceled +=
            (inputData) => editorMouseTool.UpdateRawScreenSpacePosition(inputData.ReadValue<Vector2>());

        editorInput.Mouse.RightClick.performed +=
            (inputData) => editorMouseTool.UpdateRawRightClick(true);
        editorInput.Mouse.RightClick.canceled +=
            (inputData) => editorMouseTool.UpdateRawRightClick(false);

        editorInput.Mouse.LeftClick.performed +=
            (inputData) => editorMouseTool.UpdateRawLeftClick(true);
        editorInput.Mouse.LeftClick.canceled +=
            (inputData) => editorMouseTool.UpdateRawLeftClick(false);

        editorInput.Enable();
    }

    private void OnDestroy()
    {
        editorInput.Disable();
        editorInput.Dispose();
        Singleton = null;
    }

    private void Update()
    {
        ProcessEditorTool();
        RunUserAction();
    }

    void ProcessEditorTool() => editorMouseTool.Process(Time.deltaTime);

    void RunUserAction()
    {
        EditorMouseTool.ToolUsage usage = editorMouseTool.GetToolUsage;
        if (usage == EditorMouseTool.ToolUsage.Idle) return;

        Vector2 worldPos = editorMouseTool.GetMouseWorldPosition;
        LevelTransMod closestTransmod = GetClosestTransmod(worldPos);

        // Handle transmod switching and release callback
        if (closestTransmod != null && lastUsedTransmod != null &&
            closestTransmod != lastUsedTransmod)
        {
            lastUsedTransmod.RunReleaseCallback();
        }

        // Update last used transmod if we have a valid interaction
        if (closestTransmod != null)
        {
            lastUsedTransmod = closestTransmod;
        }

        // Execute appropriate callback based on tool usage
        if (closestTransmod != null)
        {
            switch (usage)
            {
                case EditorMouseTool.ToolUsage.LeftClick:
                    closestTransmod.RunLeftClickCallbackCLICK(worldPos);
                    break;
                case EditorMouseTool.ToolUsage.RightClick:
                    closestTransmod.RunRightClickCallbackCLICK(worldPos);
                    break;
                case EditorMouseTool.ToolUsage.LeftHold:
                    closestTransmod.RunLeftClickCallbackHOLD(worldPos);
                    break;
                case EditorMouseTool.ToolUsage.RightHold:
                    closestTransmod.RunRightClickCallbackHOLD(worldPos);
                    break;
            }
        }
    }

    LevelTransMod GetClosestTransmod(Vector2 worldPosition)
    {
        LevelTransMod closest = null;
        float minDistance = float.MaxValue;

        foreach (LevelTransMod transmod in transMods)
        {
            if (!transmod.CanUse) continue;
            float distance = Vector2.Distance(transmod.transform.position, worldPosition);
            if (distance <= TransmodMaximumInteractionDistance && distance < minDistance)
            {
                minDistance = distance;
                closest = transmod;
            }
        }
        return closest;
    }

    struct EditorMouseTool
    {
        const float HoldTime = 0.3f;

        public Vector2 mousePosition;
        bool leftClick;
        bool rightClick;
        float leftClickTimer;
        float rightClickTimer;
        ToolUsage toolUsage;

        public void Process(float deltaTime)
        {
            // Update tool usage state
            toolUsage = ToolUsage.Idle;

            // Process left mouse button
            if (leftClick)
            {
                float prevTimer = leftClickTimer;
                leftClickTimer += deltaTime;

                if(leftClickTimer >= HoldTime)
                {
                    toolUsage = ToolUsage.LeftHold;
                }
            }
            else
            {
                if(leftClickTimer <= HoldTime && leftClickTimer > 0)
                {
                    toolUsage = ToolUsage.LeftClick;
                }
                leftClickTimer = 0;
            }

            // Process right mouse button (only if left didn't set usage)
            if (toolUsage == ToolUsage.Idle && rightClick)
            {
                float prevTimer = rightClickTimer;
                rightClickTimer += deltaTime;

                if (rightClickTimer >= HoldTime)
                {
                    toolUsage = ToolUsage.RightHold;
                }
            }
            else if (!rightClick)
            {
                if (rightClickTimer <= HoldTime && rightClickTimer > 0)
                {
                    toolUsage = ToolUsage.RightClick;
                }
                rightClickTimer = 0;
            }
        }

        public void UpdateRawScreenSpacePosition(Vector2 newPos) => mousePosition = newPos;
        public void UpdateRawLeftClick(bool clickState) => leftClick = clickState;
        public void UpdateRawRightClick(bool clickState) => rightClick = clickState;

        public Vector2 GetMouseWorldPosition => Camera.main.ScreenToWorldPoint(mousePosition);
        public ToolUsage GetToolUsage => toolUsage;

        public enum ToolUsage
        {
            LeftClick,
            RightClick,
            LeftHold,
            RightHold,
            Idle
        }
    }
}