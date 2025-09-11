/*using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;

public class EditorEventHandler : MonoBehaviour
{

    Stack<ShapeContainer> undoStack;

    [SerializeField]
    float snapping;
    public float Snapping => snapping;

    [SerializeField]
    public bool Render = false;
    [SerializeField]
    public int layer = 0;
    [SerializeField]
    public uint renderLayer = 0;

    [SerializeField]
    LocalSnappingPoint point;
    [SerializeField]
    ShapeContainer shapeContainerPrefab;

    EditActions input;
    Vector2 mousePos = new Vector2();
    Vector2 mousePosSS = new Vector2();
    bool isGenerating = false;
    LocalSnappingPoint startPoint;
    LocalSnappingPoint endPoint;
    ShapeContainer currentGeneration;
    TMP_Text mirrorStats;

    bool ShiftFlag = false;
    bool CtrlFlag = false;
    bool lastCtrlFlag = false;
    bool cameraPan = false;

    private void Awake()
    {
        mirrorStats = GameObject.Find("Mirror Stat").GetComponent<TMP_Text>();
        undoStack = new Stack<ShapeContainer>();
        input = new EditActions();

        input.Mouse.ScreenPositionChange.performed += (obj) =>
        {

            mousePosSS = obj.ReadValue<Vector2>();
            mousePos = Camera.main.ScreenToWorldPoint(mousePosSS);

        };

        input.Mouse.Scroll.performed += Scroll_performed;

        input.Mouse.RightClick.performed += obj => cameraPan = true;
        input.Mouse.RightClick.canceled += obj => cameraPan = false;

        input.Mouse.RightClick.performed += obj => cameraPan = true;
        input.Mouse.RightClick.canceled += obj => cameraPan = false;

        input.Mouse.Shift.performed += obj => ShiftFlag = true;
        input.Mouse.Shift.canceled += obj => ShiftFlag = false;

        input.Mouse.Ctrl.performed += obj => CtrlFlag = true;
        input.Mouse.Ctrl.canceled += obj => CtrlFlag = false;

        input.Mouse.X.performed += obj => shapeContainerPrefab.mirrorX = !shapeContainerPrefab.mirrorX;
        input.Mouse.Y.performed += obj => shapeContainerPrefab.mirrorY = !shapeContainerPrefab.mirrorY;

        input.Mouse.Undo.performed += obj =>
        {
            if (!CtrlFlag) return;
            if (undoStack.TryPop(out ShapeContainer result))
            {
                Destroy(result.gameObject);
            }
            lastCtrlFlag = !CtrlFlag;
        };

        input.Mouse.LeftClick.performed += StartGeneration;
        input.Mouse.LeftClick.canceled += EndGeneration;

        input.Mouse.Delta.performed += Delta_performed;

        targetOrthoSize = Camera.main.orthographicSize;
    }

    float targetOrthoSize;
    Vector2 zoomScreenLockPoint;
    float zoomSpeed = 20f;
    bool isZooming = false;

    private void Scroll_performed(InputAction.CallbackContext obj)
    {

        if (ShiftFlag && isGenerating)
        {
            float newScale = obj.ReadValue<float>();
            //Apply scaling to shape.

        }
        else
        {

            //Update camera zoom.

            Camera cam = Camera.main;

            zoomScreenLockPoint = mousePosSS;
            targetOrthoSize -= obj.ReadValue<float>();
            targetOrthoSize = Mathf.Clamp(targetOrthoSize, 1f, 20f);
            isZooming = true;

        }
    }


    private void Delta_performed(InputAction.CallbackContext obj)
    {
        if (!cameraPan) return;
        Camera cam = Camera.main;

        Vector2 mousePixelDelta = obj.ReadValue<Vector2>();

        float unitsPerPixel = cam.orthographicSize * 2f / cam.pixelHeight;

        Vector3 move = new Vector3(mousePixelDelta.x * unitsPerPixel, mousePixelDelta.y * unitsPerPixel, 0f);

        cam.transform.position -= move;
    }

    private void OnDestroy() => input.Dispose();

    private void Update()
    {

        mirrorStats.text = shapeContainerPrefab.mirrorX && shapeContainerPrefab.mirrorY ? "Mirror: XY" :
            shapeContainerPrefab.mirrorX && !shapeContainerPrefab.mirrorY ? "Mirror: X" :
            shapeContainerPrefab.mirrorY && !shapeContainerPrefab.mirrorX ? "Mirror: Y" :
            "Mirror: Off";

        Camera cam = Camera.main;

        // --- ZOOM ---
        if (isZooming)
        {
            float oldSize = cam.orthographicSize;
            float newSize = Mathf.Lerp(oldSize, targetOrthoSize, Time.deltaTime * zoomSpeed);

            if (Mathf.Abs(newSize - targetOrthoSize) < 0.001f)
            {
                newSize = targetOrthoSize;
                isZooming = false;
            }

            // Mouse world before zoom change
            Vector3 mouseWorldBefore = cam.ScreenToWorldPoint(zoomScreenLockPoint);

            cam.orthographicSize = newSize;

            // Mouse world after zoom change
            Vector3 mouseWorldAfter = cam.ScreenToWorldPoint(zoomScreenLockPoint);

            // Move camera so mouse stays over same point
            Vector3 offset = mouseWorldBefore - mouseWorldAfter;
            cam.transform.position += offset;
        }

        if (isGenerating) UpdateLiveGeneration();

        if (CtrlFlag != lastCtrlFlag)
        {
            if (undoStack.TryPeek(out ShapeContainer result))
            {
                if (!CtrlFlag) result.DisableForcefieldsOnMimic();
            }
            lastCtrlFlag = CtrlFlag;
        }

    }

    void UpdateLiveGeneration()
    {
        if (!currentGeneration) return;
    }

    private void StartGeneration(CallbackContext obj)
    {
        Vector2 startPosition = GetSnappedPosition(mousePos);
        startPoint = Instantiate(point, mousePos, Quaternion.identity);

        Vector2 endPosition = GetSnappedPosition(mousePos);
        endPoint = Instantiate(point, mousePos, Quaternion.identity);

        currentGeneration = Instantiate(shapeContainerPrefab, null);
        currentGeneration.transform.position = Vector3.Lerp(startPoint.transform.position, endPoint.transform.position, 0.5f);
        currentGeneration.AssignSnappingPoints(startPoint, endPoint);
        //pivotResult.AssignDragMod(this);


        startPoint.AssignShapeContainer(currentGeneration);
        startPoint.AssignLookatTarget(endPoint.transform);
        startPoint.AssignrawWorldPositionPosition(startPoint.transform.position);
        startPoint.AssignSnapping(snapping);
        startPoint.AssignStart(true);

        endPoint.AssignShapeContainer(currentGeneration);
        endPoint.AssignLookatTarget(startPoint.transform);
        endPoint.AssignrawWorldPositionPosition(endPoint.transform.position);
        endPoint.AssignSnapping(snapping);
        endPoint.AssignStart(false);

        isGenerating = true;
    }

    public void EraseInvalidShape(ShapeContainer invalidShape)
    {
        List<ShapeContainer> stackToList = undoStack.ToList();
        stackToList.Remove(invalidShape);
        undoStack.Clear();
        for (int i = stackToList.Count - 1; i >= 0; i--) undoStack.Push(stackToList[i]);
        Destroy(invalidShape.gameObject);
    }

    private void EndGeneration(CallbackContext obj)
    {
        Vector2 endPosition = GetSnappedPosition(mousePos);

        startPoint.transform.SetParent(currentGeneration.transform, true);
        endPoint.transform.SetParent(currentGeneration.transform, true);

        currentGeneration.OnRelease();

        undoStack.Push(currentGeneration);

        isGenerating = false;
    }

    Vector2 GetSnappedPosition(Vector2 rawPosition)
    {

        float x, y;
        x = Mathf.Round(rawPosition.x / snapping) * snapping;
        y = Mathf.Round(rawPosition.y / snapping) * snapping;

        return new Vector2(x, y);
    }

    private void OnEnable()
    {
        EnableInputs();
    }

    private void OnDisable()
    {
        DisableInputs();
    }

    void EnableInputs()
    {
        input.Enable();
    }

    void DisableInputs()
    {
        input.Disable();
    }
}
*/