using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Scripting;
using static UnityEngine.InputSystem.InputAction;
[Preserve]
public sealed class DragAndScrollMod : MonoBehaviour
{

    public float rememberSplineSpeed = 0;
    public float rememberSplineOffset = 0;

    public string activeLevelName = "New Level";

    public Action<ShapeMimicBehaviour> OnShaapeSpawn = (empty) => { };
    public Action<ShapeMimicBehaviour> OnShaapeDespawn = (empty) => { };
    public Action<ShapeMimicBehaviour, int, int> OnShapeIDChange = (empty1, empty2, empty3) => { };

    const int framesGeneratingAShape = 12;

    public delegate void GenerationFunc();
    List<GenerationFunc> generators = new List<GenerationFunc>();

    public bool brokenMimicsDictionaryFlag = false;

    EditorSquareSpawn liveSquareSpawn = null;
    WorldLight liveLight = null;
    PermaLightBehvaiour permaLight = null;

    [SerializeField]
    ShapeSelector shapeSelector;

    AnimationAnchor selectedAnimationAnchor;

    List<AnimationAnchor> animators;
    List<WorldLight> worldLights = new List<WorldLight>();
    SplineDrag splineDrag;

    LevelEditorInitializer initializer;
    [SerializeField]
    TabModeObject[] tabModeObjects;

    Stack<ShapeMemory> destroyStack;
    Stack<ShapeContainer> undoStack;

    public void EnableEditInputs() => input.Enable();
    public void DisableEditInputs() => input.Disable();

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
    [SerializeField]
    WorldLight worldLightPrefab;

    EditActions input;
    Vector2 mousePos = new Vector2();
    Vector2 mouseDelta = new Vector2();
    Vector2 mousePosSS = new Vector2();
    bool isGenerating = false;
    LocalSnappingPoint startPoint;
    LocalSnappingPoint endPoint;
    ShapeContainer pivotResult;
    TMP_Text mirrorStats;
    TMP_Text nameStat;

    bool ShiftFlag = false;
    bool CtrlFlag = false;
    bool lastCtrlFlag = false;
    bool cameraPan = false;
    bool rightClick = false;
    bool leftClick = false;
    bool tabFlag = false;

    private void Awake()
    {
        shapeSelector = Instantiate(shapeSelector);
        permaLight = FindAnyObjectByType<PermaLightBehvaiour>();
        activeLevelName = "New Level";
        ShapeMimicBehaviour.sharedMesh = null;
        initializer = GetComponentInParent<LevelEditorInitializer>();

        animators = new List<AnimationAnchor>();
        mirrorStats = GameObject.Find("Mirror Stat").GetComponent<TMP_Text>();
        nameStat = GameObject.Find("Name Stat").GetComponent<TMP_Text>();
        destroyStack = new Stack<ShapeMemory>();
        undoStack = new Stack<ShapeContainer>();
        input = new EditActions();

        input.Mouse.TempSwapEdit.performed += obj =>
        {

            bool tempShapeGen = pivotResult && isGenerating;
            bool tempSplineGen = liveAnchor != null;
            liveSquareSpawn = null;

            if (tempSplineGen)
            {
                isGenerating = true;
                EndSplineGeneration();
                isGenerating = false;
            }

            if (pivotResult)
            {
                isGenerating = true;
                EndShapeGeneration();
                isGenerating = false;
            }

            shapeSelector.EndItemsDragging(mousePos);
            shapeSelector.EndItemSelecting(mousePos);
            shapeSelector.HardReset();

            if (selectedAnimationAnchor)
            {
                selectedAnimationAnchor.selected = false;
                selectedAnimationAnchor = null;
            }

            tabFlag = true;
            foreach (TabModeObject tabMode in tabModeObjects) tabMode.EnableTabMode();
        };
        input.Mouse.TempSwapEdit.canceled += obj =>
        {
            tabFlag = false;
            foreach (TabModeObject tabMode in tabModeObjects) tabMode.DisableDabMode();
        };

        input.Mouse.TimerReset.performed += obj =>
        {
            if (tabFlag) return;
            Resources.UnloadUnusedAssets();
            animators.ForEach(anim => anim.ResetTimer());
        };

        input.Mouse.Preview.performed += Preview_performed;
        input.Mouse.Preview.canceled += Preview_canceled;

        input.Mouse.ScreenPositionChange.performed += (obj) =>
        {
            if (tabFlag) return;
            mouseDelta = mousePos;
            mousePosSS = obj.ReadValue<Vector2>();

            Vector2 mouseInWorld = Camera.main.ScreenToWorldPoint(mousePosSS);

            mousePos.x = Mathf.Clamp(mouseInWorld.x, -127f, 127f);
            mousePos.y = Mathf.Clamp(mouseInWorld.y, -127f, 127f);
            mouseDelta = mousePos - mouseDelta;

            RunTransformToolFromMouseInput();
        };

        input.Mouse.Scroll.performed += Scroll_performed;

        input.Mouse.RightClick.performed += RightClickOn;
        input.Mouse.RightClick.canceled += RightClickOff;

        input.Mouse.MiddleClick.performed += obj =>
        {
            if (tabFlag) return;
            cameraPan = true;
        };
        input.Mouse.MiddleClick.canceled += obj =>
        {
            cameraPan = false;
        };

        input.Mouse.Shift.performed += obj =>
        {
            if (tabFlag) return;
            ShiftFlag = true;
        };
        input.Mouse.Shift.canceled += obj =>
        {
            ShiftFlag = false;
        };

        input.Mouse.Ctrl.performed += obj =>
        {
            if (tabFlag) return;
            CtrlFlag = true;
        };
        input.Mouse.Ctrl.canceled += obj =>
        {
            CtrlFlag = false;
        };

        input.Mouse.X.performed += obj =>
        {
            if (tabFlag) return;
            shapeContainerPrefab.mirrorX = !shapeContainerPrefab.mirrorX;
        };
        input.Mouse.Y.performed += obj =>
        {
            if (tabFlag) return;
            shapeContainerPrefab.mirrorY = !shapeContainerPrefab.mirrorY;
        };

        input.Mouse.S.performed += obj =>
        {
            if (tabFlag) return;
            initializer.CompileMap();
            ListPersistendLevels.levelPathPointer.EnsurePath(activeLevelName);
            MapStorage shapeStorage = new MapStorage(activeLevelName, this);
        };

        input.Mouse.Undo.performed += obj =>
        {
            if (tabFlag) return;
            if (!CtrlFlag) return;
            if (ShiftFlag)
            {

                if (destroyStack.TryPop(out ShapeMemory memory))
                {

                    generators.Add(() =>
                    {
                        mousePos = memory.startPos.AsVector3();
                        shapeContainerPrefab.mirrorX = memory.mirrorX;
                        shapeContainerPrefab.mirrorY = memory.mirrorY;
                        StartShapeGeneration(false);

                    });

                    for (int i = 0; i < framesGeneratingAShape; i++) generators.Add(() =>
                    {
                        shapeContainerPrefab.mirrorX = memory.mirrorX;
                        shapeContainerPrefab.mirrorY = memory.mirrorY;
                        pivotResult.SetScale(memory.scale);
                        mousePos = memory.endPos.AsVector3();
                        UpdateLiveGeneration(false);

                    });

                    generators.Add(() =>
                    {
                        shapeContainerPrefab.mirrorX = memory.mirrorX;
                        shapeContainerPrefab.mirrorY = memory.mirrorY;
                        ShapeContainer keepPivot = pivotResult;
                        EndShapeGeneration(false);
                        keepPivot.SetAllMimicOffsets(memory.oOffsetPos, memory.xOffsetPos, memory.yOffsetPos, memory.xyOffsetPos);
                    });

                }

            }
            else
            {

                if (undoStack.TryPop(out ShapeContainer result))
                {
                    RegisterDestroy(result);
                    Destroy(result.gameObject);
                }
                lastCtrlFlag = !CtrlFlag;

            }
        };

        input.Mouse.LeftClick.performed += LeftClickOn;
        input.Mouse.LeftClick.canceled += LeftClickOff;

        input.Mouse.Delta.performed += Delta_performed;

        targetOrthoSize = Camera.main.orthographicSize;
    }

    private void Start()
    {
        foreach (TabModeObject tabMode in tabModeObjects) tabMode.EnableTabMode();
    }
    public void ClearAll(bool includeStacks = false)
    {
        foreach (ShapeContainer existingShape in undoStack) if(existingShape) if (existingShape.gameObject) Destroy(existingShape.gameObject);
        foreach (AnimationAnchor animationAnchor in animators) animationAnchor.DeleteAnimation();
        foreach (WorldLight light in worldLights) light.DeleteLight();

        ShapeMimicBehaviour.ShapeMimics.Clear();
        AnimationAnchor.AnimationAnchors.Clear();
        WorldLight.WorldLights.Clear();

        ShapeMimicBehaviour.ShapeIDCounter = 0;
        AnimationAnchor.AnimationIDCounter = 0;
        WorldLight.worldLightIDCounter = 0;
        WorldLight.WorldLightCount = 0;

        animators.Clear();
        worldLights.Clear();

        if (includeStacks)
        {
            undoStack.Clear();
            destroyStack.Clear();
        }

    }

    public Vector2 GetMousePos() => mousePos;
    public bool GetTabFlag() => tabFlag;
    public bool SetTabFlag(bool flag) => tabFlag = flag;
    private bool previewing = false;
    public bool GetPreviewing() => previewing;
    private void Preview_performed(CallbackContext obj)
    {
        if (tabFlag) return;
        previewing = true;
        animators.ForEach(animator =>
        {
            animator.EnablePreview();
        });
    }
    private void Preview_canceled(CallbackContext context)
    {
        previewing = false;
        animators.ForEach(animator =>
        {
            animator.DisablePreview();
        });
    }


    float targetOrthoSize;
    Vector2 zoomScreenLockPoint;
    float zoomSpeed = 20f;
    bool isScrolling = false;

    private void Scroll_performed(InputAction.CallbackContext obj)
    {
        if (tabFlag) return;
        if (CtrlFlag)
        {
            if (initializer.GetMode() == EditorMode.Animating)
            {
                RaycastHit2D hit = GetHit2D(true);
                if(hit) if(hit.transform.TryGetComponent<AnimationAnchor>(out AnimationAnchor anim))
                    {
                        anim.splineSpeed += obj.ReadValue<float>() * 0.01f;
                        rememberSplineSpeed = anim.splineSpeed;
                        return;
                    }
                if (hit) if (hit.transform.TryGetComponent<SplineDrag>(out SplineDrag anim))
                    {
                        anim.GetAnchor().splineSpeed += obj.ReadValue<float>() * 0.01f;
                        rememberSplineSpeed = anim.GetAnchor().splineSpeed;
                        return;
                    }
            }
        }else if (ShiftFlag)
        {
            if (initializer.GetMode() == EditorMode.Animating)
            {
                RaycastHit2D hit = GetHit2D(true);
                if (hit) if (hit.transform.TryGetComponent<AnimationAnchor>(out AnimationAnchor anim))
                    {
                        anim.splineOffset += obj.ReadValue<float>() * 0.01f;
                        rememberSplineOffset = anim.splineOffset;
                        return;
                    }
                if (hit) if (hit.transform.TryGetComponent<SplineDrag>(out SplineDrag anim))
                    {
                        anim.GetAnchor().splineOffset += obj.ReadValue<float>() * 0.01f;
                        rememberSplineOffset = anim.GetAnchor().splineOffset;
                        return;
                    }
            }
        }

        if (ShiftFlag && isGenerating)
        {

            if (initializer.GetMode() == EditorMode.Shaping) pivotResult.ChangeScale(obj.ReadValue<float>());


        }
        else
        {

            Camera cam = Camera.main;

            zoomScreenLockPoint = mousePosSS; // mouse in screen pixels

            // New target zoom
            targetOrthoSize -= obj.ReadValue<float>();
            targetOrthoSize = Mathf.Clamp(targetOrthoSize, 1f, 64f);

            isScrolling = true;

        }
    }

    private void Delta_performed(InputAction.CallbackContext obj)
    {
        if (tabFlag) return;
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

        nameStat.text = $"Map: {activeLevelName}";
        if(generators.Count != 0)
        {
            try
            {
                generators[0]();
            }
            finally
            {
                generators.RemoveAt(0);
            }
        }
        else
        {
            //Fiz mimic dependencies..
            if (brokenMimicsDictionaryFlag) shapeSelector.PatchMimicsArray();
            brokenMimicsDictionaryFlag = false;
        }

            mirrorStats.text = shapeContainerPrefab.mirrorX && shapeContainerPrefab.mirrorY ? "Mirror: XY" :
                shapeContainerPrefab.mirrorX && !shapeContainerPrefab.mirrorY ? "Mirror: X" :
                shapeContainerPrefab.mirrorY && !shapeContainerPrefab.mirrorX ? "Mirror: Y" :
                "Mirror: Off";

        Camera cam = Camera.main;

        if (isScrolling)
        {
            float oldSize = cam.orthographicSize;
            float newSize = Mathf.Lerp(oldSize, targetOrthoSize, Time.deltaTime * zoomSpeed);

            if (Mathf.Abs(newSize - targetOrthoSize) < 0.001f)
            {
                newSize = targetOrthoSize;
                isScrolling = false;
            }

            Vector3 mouseWorldBefore = cam.ScreenToWorldPoint(zoomScreenLockPoint);

            cam.orthographicSize = newSize;

            Vector3 mouseWorldAfter = cam.ScreenToWorldPoint(zoomScreenLockPoint);

            Vector3 offset = mouseWorldBefore - mouseWorldAfter;
            cam.transform.position += offset;
        }

        if (isGenerating) UpdateLiveGeneration();

        if (rightClick && !isGenerating && !CtrlFlag && !ShiftFlag) RightClickEveryFrame();

        if (initializer.GetMode() == EditorMode.Animating && selectedAnimationAnchor)
        {
            if(rightClick) MarkShapesToDetatch();
            else if (leftClick) MarkShapesToAttatch();
        }

        if(initializer.GetMode() == EditorMode.Lighting && liveLight)
        {

            if (leftClick) liveLight.SetTargetPos(GetSnappedPosition(mousePos));

        }
    }

    void RunTransformToolFromMouseInput()
    {

        if (initializer.GetMode() == EditorMode.Transforming)
        {

            if (liveSquareSpawn) liveSquareSpawn.Drag(GetSnappedPosition(mousePos));
            else
            {

                if (leftClick)
                {

                    if (ShiftFlag)
                    {

                        if (shapeSelector.hasItemsSelected())
                        {
                            shapeSelector.UpdateDragging(mouseDelta);
                        }

                    }
                    else
                    {
                        shapeSelector.UpdateItemSelecting(mousePos);
                    }

                }

            }

        }

    }

    void MarkShapesToDetatch()
    {
        RaycastHit2D[] hits = GetAllHit2D(false);
        if (hits == null) return;
        foreach (RaycastHit2D hit in hits) if (hit.transform.TryGetComponent(out ShapeMimicBehaviour mimic)) selectedAnimationAnchor.DetatchMimic(mimic.ShapeID);
    }

    void MarkShapesToAttatch()
    {
        RaycastHit2D[] hits = GetAllHit2D(false);
        if (hits == null) return;
        foreach (RaycastHit2D hit in hits) if (hit.transform.TryGetComponent(out ShapeMimicBehaviour mimic)) selectedAnimationAnchor.AttatchMimic(mimic.ShapeID);
    }

    void RightClickEveryFrame()
    {

        RaycastHit2D hit = GetHit2D();
        if (!hit) return;

        if (initializer.GetMode() == EditorMode.Shaping) DeleteShape(hit);
        if (initializer.GetMode() == EditorMode.Animating) DeleteSplineMod(hit);
        if (initializer.GetMode() == EditorMode.Lighting) DeleteLight(hit);
    }

    private void DeleteLight(RaycastHit2D hit)
    {

        if (hit.transform.TryGetComponent(out WorldLight worldLight))
        {
            worldLight.DeleteLight();
            worldLights.Remove(worldLight);
        }

    }

    private void DeleteSplineMod(RaycastHit2D hit)
    {
        if (hit.transform.TryGetComponent(out SplineDrag splineDrag))
        {
            splineDrag.RemoveMe();
            this.splineDrag = null;
        }
    }

    RaycastHit2D GetHit2D(bool withSnapping = false)
    {
        Vector2 mousePos = withSnapping ? GetSnappedPosition(this.mousePos) : this.mousePos;
        Ray ray = new Ray(new Vector3(mousePos.x, mousePos.y, Camera.main.transform.position.z), Vector3.forward);
        RaycastHit2D hit;
        hit = Physics2D.GetRayIntersection(ray);
        return hit;
    }

    RaycastHit2D[] GetAllHit2D(bool withSnapping = false)
    {
        Vector2 mousePos = withSnapping ? GetSnappedPosition(this.mousePos) : this.mousePos;
        Ray ray = new Ray(new Vector3(mousePos.x, mousePos.y, Camera.main.transform.position.z), Vector3.forward);
        RaycastHit2D[] hit;
        hit = Physics2D.GetRayIntersectionAll(ray);
        return hit;
    }

    void DeleteShape(RaycastHit2D hit)
    {

        ShapeMimicBehaviour mimic = hit.transform.GetComponent<ShapeMimicBehaviour>();
        if (!mimic) return;

        ShapeContainer shape = mimic.GetShapeContainer();

        RegisterDestroy(shape);

        EraseInvalidShape(shape);

    }

    void RegisterDestroy(ShapeContainer shape)
    {

        CustomVec3 oOffsetPos, xOffsetPos, yOffsetPos, xyOffsetPos;
        (oOffsetPos, xOffsetPos, yOffsetPos, xyOffsetPos) = shape.GetAllMimicsOffsets();

        ShapeMemory shapeMemory = new ShapeMemory()
        {

            oOffsetPos = oOffsetPos,
            xOffsetPos = xOffsetPos,
            yOffsetPos = yOffsetPos,
            xyOffsetPos = xyOffsetPos,

            scale = shape.scale,
            genPos = shape.mousePosOnGenerate,
            startPos = shape.mousePosOnGenerate,
            endPos = shape.mousePosOnRelease,
            mirrorX = shape.mirrorX,
            mirrorY = shape.mirrorY,
        };

        shapeMemory.SetVerticesFromVector3Arr(shape.GetCurrentVertices());

        destroyStack.Push(shapeMemory);
    }

    void UpdateLiveGeneration(bool ignoreSnapping = false)
    {

        if (liveAnchor)
        {
            if(ShiftFlag || CtrlFlag) liveAnchor.MoveAll(ignoreSnapping ? mousePos : GetSnappedPosition(mousePos));
            else liveAnchor.ChangeAnchorPosition(ignoreSnapping ? mousePos : GetSnappedPosition(mousePos));
        }

        if (splineDrag)
        {
            if (ShiftFlag || CtrlFlag) splineDrag.GetAnchor().MoveAll(ignoreSnapping ? mousePos : GetSnappedPosition(mousePos));
            else splineDrag.DoDrag(ignoreSnapping ? mousePos : GetSnappedPosition(mousePos));
        }

        if (pivotResult)
        {
            Vector2 endPosition = GetSnappedPosition(mousePos);
            if (ignoreSnapping) endPosition = mousePos;
            endPoint.AssignrawWorldPositionPosition(endPosition);
        }
    }

    private void LeftClickOn(CallbackContext obj)
    {
        if (tabFlag) return;
        leftClick = true;
        switch (initializer.GetMode())
        {
            case EditorMode.Shaping:
                StartShapeGeneration();
                break;
            case EditorMode.Animating:
                StartSplineGeneration();
                break;
            case EditorMode.Lighting:
                SrtartLightGeneration();
                break;
            case EditorMode.Transforming:
                StartSelectionGeneration();
                break;
        }
    }

    private void StartSelectionGeneration()
    {

        bool foundSquareSpawn = false;

        RaycastHit2D[] hits = GetAllHit2D();
        foreach (var item in hits)
        {
            if (item.transform.TryGetComponent(out EditorSquareSpawn squareSpawn))
            {
                liveSquareSpawn = squareSpawn;
                foundSquareSpawn = true;
            }
        }
        if (foundSquareSpawn) return;
        if (!ShiftFlag) shapeSelector.StartItemSelecting(mousePos);
        else shapeSelector.StartItemsDragging(mousePos);

    }

    private void SrtartLightGeneration()
    {
        Vector2 mousePos = GetSnappedPosition(this.mousePos);
        RaycastHit2D hit = GetHit2D();
        if (!hit)
        {
            WorldLight worldLight = Instantiate(worldLightPrefab);
            worldLight.SetTargetPos(mousePos);
            liveLight = worldLight;
            worldLights.Add(worldLight);
        }
        else
        {
            if (hit.transform.TryGetComponent(out WorldLight worldLight)) liveLight = worldLight;
        }
    }

    [SerializeField] AnimationAnchor animationAnchorPrefab;
    AnimationAnchor liveAnchor;

    private void StartSplineGeneration()
    {
        Vector2 mousePos = GetSnappedPosition(this.mousePos);
        isGenerating = true;
        RaycastHit2D hit = GetHit2D();
        if (!hit)
        {

            if (selectedAnimationAnchor)
            {
                selectedAnimationAnchor.selected = false;
                selectedAnimationAnchor = null;
            }
            else
            {
                AnimationAnchor anchor = Instantiate(animationAnchorPrefab);
                anchor.ChangeAnchorPosition(mousePos);
                anchor.transform.position = new Vector3(mousePos.x, mousePos.y, -10);
                animators.Add(anchor);
                liveAnchor = anchor;
                liveAnchor.AddSplineMod();
                if (selectedAnimationAnchor) selectedAnimationAnchor.selected = false;
                selectedAnimationAnchor = liveAnchor;
                selectedAnimationAnchor.selected = true;
                anchor.splineSpeed = rememberSplineSpeed;
                anchor.splineOffset = rememberSplineOffset;
                anchor.RefreshRenderers();
            }
        }
        else
        {

            if (hit.transform.TryGetComponent(out ShapeMimicBehaviour mimic))
            {
                if (selectedAnimationAnchor) selectedAnimationAnchor.AttatchMimic(mimic.ShapeID);
            }
            else
            {

                if (ShiftFlag)
                {
                    if (hit.transform.TryGetComponent(out AnimationAnchor liveAnchor))
                    {
                        SelectAnchor(liveAnchor);
                        this.liveAnchor = liveAnchor;
                    }
                }
                if (CtrlFlag)
                {
                    if (hit.transform.TryGetComponent(out AnimationAnchor liveAnchor))
                    {
                        bool isNew = SelectAnchor(liveAnchor);
                        if (!isNew) liveAnchor.AddSplineMod();
                    }
                    else if (hit.transform.TryGetComponent(out SplineDrag splineDrag))
                    {
                        SelectAnchor(splineDrag.GetAnchor());
                    }
                }
                else
                {
                    if (hit.transform.TryGetComponent(out SplineDrag splineDrag))
                    {
                        bool isNew = SelectAnchor(splineDrag.GetAnchor());
                        if (!isNew) this.splineDrag = splineDrag;
                    }
                    else if (hit.transform.TryGetComponent(out AnimationAnchor liveAnchor))
                    {
                        bool isNew = SelectAnchor(liveAnchor);
                        if(!isNew) this.liveAnchor = liveAnchor;
                    }
                }

            }
        }

        bool SelectAnchor(AnimationAnchor anchor)
        {
            bool isNew = true;
            if (selectedAnimationAnchor)
            {
                if(selectedAnimationAnchor == anchor) isNew = false;
                selectedAnimationAnchor.selected = false;
            }
            selectedAnimationAnchor = anchor;
            selectedAnimationAnchor.selected = true;
            rememberSplineSpeed = selectedAnimationAnchor.splineSpeed;
            rememberSplineOffset = selectedAnimationAnchor.splineOffset;
            return isNew;
        }

    }

    private void StartShapeGeneration(bool ignoreSnapping = false)
    {
        Vector2 startPosition = GetSnappedPosition(mousePos);
        //if (ignoreSnapping) startPosition = mousePos;
        startPoint = Instantiate(point, mousePos, Quaternion.identity);

        Vector2 endPosition = GetSnappedPosition(mousePos);
        //if (ignoreSnapping) endPosition = mousePos;
        endPoint = Instantiate(point, mousePos, Quaternion.identity);

        Vector3 pivotPosition = (Vector3)startPosition;
        pivotPosition.z = 0;
        pivotResult = Instantiate(shapeContainerPrefab, null);
        pivotResult.transform.position = pivotPosition;
        pivotResult.AssignSnappingPoints(startPoint, endPoint);
        pivotResult.AssignDragMod(this);
        pivotResult.mousePosOnGenerate = this.mousePos;

        startPoint.AssignShapeContainer(pivotResult);
        startPoint.AssignLookatTarget(endPoint.transform);
        startPoint.AssignrawWorldPositionPosition(startPoint.transform.position);
        startPoint.AssignSnapping(snapping);
        startPoint.AssignStart(true);

        endPoint.AssignShapeContainer(pivotResult);
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

    private void LeftClickOff(CallbackContext obj)
    {

        leftClick = false;
        switch (initializer.GetMode())
        {
            case EditorMode.Shaping:
                destroyStack.Clear();
                EndShapeGeneration();
                break;
            case EditorMode.Animating:
                EndSplineGeneration();
                break;
            case EditorMode.Lighting:
                EndLightGeneration();
                break;
            case EditorMode.Transforming:
                EndSelectionGeneration();
                break;
        }
    }

    private void EndSelectionGeneration()
    {
        liveSquareSpawn = null;
        shapeSelector.EndItemSelecting(mousePos);
        shapeSelector.EndItemsDragging(mousePos);
    }

    private void EndLightGeneration()
    {
        liveLight = null;
        isGenerating = false;
    }

    private void EndSplineGeneration()
    {
        isGenerating = false;
        liveAnchor = null;
        splineDrag = null;
    }

    private void RightClickOn(CallbackContext obj)
    {
        if (tabFlag) return;
        rightClick = true;
        if(initializer.GetMode() == EditorMode.Animating)
        {
            RaycastHit2D hit = GetHit2D();
            if (hit)
            {
                if (CtrlFlag)
                {
                    if (hit.transform.TryGetComponent<AnimationAnchor>(out AnimationAnchor animationAnchor))
                    {
                        if (liveAnchor == animationAnchor)
                        {
                            liveAnchor = null;
                        }
                        animators.Remove(animationAnchor);
                        animationAnchor.DeleteAnimation();
                    }
                }
                else
                {
                    if (hit.transform.TryGetComponent<SplineDrag>(out SplineDrag splineDrag))
                    {
                        if (splineDrag == this.splineDrag) this.splineDrag = null;
                        splineDrag.RemoveMe();
                    }
                }
            }
        }
    }

    void RightClickOff(CallbackContext obj) => rightClick = false;

    private void EndShapeGeneration(bool ignoreSnapping = false)
    {

        if (!isGenerating)
        {
            pivotResult = null;
            return;
        }

        Vector2 endPosition = GetSnappedPosition(mousePos);
        if (ignoreSnapping) endPosition = mousePos;

        startPoint.transform.SetParent(pivotResult.transform, true);
        endPoint.transform.SetParent(pivotResult.transform, true);

        pivotResult.OnRelease();
        pivotResult.mousePosOnRelease = this.mousePos;

        undoStack.Push(pivotResult);

        isGenerating = false;
        pivotResult = null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    Vector2 GetSnappedPosition(Vector2 rawPosition) => new Vector2(Mathf.Round(rawPosition.x / snapping) * snapping, Mathf.Round(rawPosition.y / snapping) * snapping);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnEnable() => EnableInputs();
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnDisable() => DisableInputs();
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void EnableInputs() => input.Enable();
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void DisableInputs() => input.Disable();
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AddGenerator(GenerationFunc value) => generators.Add(value);
    [Preserve]
    public class MapStorage
    {
        [JsonConstructor]
        public MapStorage() { }

        public ShapeMemory[] shapeMemories;
        public AnimationMemory[] animationMemories;
        public LightMemory[] lightMemories;
        public SpawnMemory[] spawnMemories;

        static JsonSerializerSettings GetJsonSettings()
        {
            JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings();

            jsonSerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            jsonSerializerSettings.Formatting = Formatting.Indented;
            jsonSerializerSettings.FloatFormatHandling = FloatFormatHandling.String;
            jsonSerializerSettings.FloatParseHandling = FloatParseHandling.Decimal;
            jsonSerializerSettings.NullValueHandling = NullValueHandling.Ignore;

            return jsonSerializerSettings;
        }


        public MapStorage(string name)
        {
            //Load from file
            string contentAsJson = LevelFilePaths.LoadLevel(name);

            MapStorage mapStorage = JsonConvert.DeserializeObject<MapStorage>(contentAsJson, GetJsonSettings());
            this.shapeMemories = mapStorage.shapeMemories;
            this.animationMemories = mapStorage.animationMemories;
            this.lightMemories = mapStorage.lightMemories;
            this.spawnMemories = mapStorage.spawnMemories;
            if(this.spawnMemories == null)
            {
                spawnMemories = new SpawnMemory[0];
            }
        }

        public MapStorage(string name, DragAndScrollMod mod)
        {
            //Write to file
            EditorSquareSpawn[] editorSquareSpawns = FindObjectsByType<EditorSquareSpawn>(FindObjectsSortMode.InstanceID);
            List<ShapeContainer> undoStack = mod.undoStack.ToList();
            int shapeMemoryLength = undoStack.Count;
            int animationMemoryLength = mod.animators.Count;
            int lightMemoryLength = mod.worldLights.Count;
            int spawnMemoryLength = editorSquareSpawns.Length;

            shapeMemories = new ShapeMemory[shapeMemoryLength];
            animationMemories = new AnimationMemory[animationMemoryLength];
            lightMemories = new LightMemory[lightMemoryLength];
            spawnMemories = new SpawnMemory[spawnMemoryLength];


            for (int i = 0; i < shapeMemoryLength; i++)
            {
                ShapeContainer shape = undoStack[i];
                LocalSnappingPoint a, b;
                int oID, xID, yID, xyID;
                CustomVec3 oOffsetPos, xOffsetPos, yOffsetPos, xyOffsetPos;

                (a, b) = shape.GetSnappingPoints();
                (oID, xID, yID, xyID) = shape.GetAllMimicsID();
                (oOffsetPos, xOffsetPos, yOffsetPos, xyOffsetPos) = shape.GetAllMimicsOffsets();

                ShapeMemory shapeMemory = new ShapeMemory()
                {
                    scale = shape.scale,
                    oID = oID,
                    xID = xID,
                    yID = yID,
                    xyID = xyID,

                    oOffsetPos = oOffsetPos,
                    xOffsetPos = xOffsetPos,
                    yOffsetPos = yOffsetPos,
                    xyOffsetPos = xyOffsetPos,

                    startPos = shape.mousePosOnGenerate,
                    endPos = shape.mousePosOnRelease,
                    genPos = shape.mousePosOnGenerate,

                    mirrorX = shape.mirrorX,
                    mirrorY = shape.mirrorY,
                };
                shapeMemory.SetVerticesFromVector3Arr(shape.GetCurrentVertices());

                shapeMemories[i] = shapeMemory;
            }

            for (int i = 0; i < animationMemoryLength; i++)
            {
                
                int animationID = mod.animators[i].AnimationID;
                int[] attachedMimicIDs = mod.animators[i].GetAttatchedMimicIDs();
                Vector2 startPos = mod.animators[i].TargetPos;
                CustomVec2[] splineModSegments = new CustomVec2[mod.animators[i].splineMods.Count * 3];
                float speed = mod.animators[i].splineSpeed;
                float offset = mod.animators[i].splineOffset;
                for (int j = 0; j < splineModSegments.Length; j += 3)
                {
                    splineModSegments[j + 0] = mod.animators[i].splineMods[j / 3].GetEndMod().GetTarget();
                    splineModSegments[j + 1] = mod.animators[i].splineMods[j / 3].GetLeftMod().GetTarget();
                    splineModSegments[j + 2] = mod.animators[i].splineMods[j / 3].GetRightMod().GetTarget();
                }

                animationMemories[i] = new AnimationMemory()
                {
                    attachedMimicIDs = attachedMimicIDs,
                    animationId = animationID,
                    segments = splineModSegments,
                    startSegment = startPos,
                    speed = speed,
                    offset = offset
                };
            }

            for (int i = 0; i < lightMemoryLength; i++)
            {
                WorldLight light = mod.worldLights[i];
                LightMemory lightMemory = new LightMemory 
                { 
                    lightId = light.worldLightID,
                    position = light.transform.position,
                };
                lightMemories[i] = lightMemory;
            }

            for (int i = 0; i < spawnMemoryLength; i++)
            {
                spawnMemories[i] = new SpawnMemory
                {
                    pos = editorSquareSpawns[i].targetPosition,
                };
            }

            string contentAsJson = JsonConvert.SerializeObject(this, GetJsonSettings());
            LevelFilePaths.StoreLevel(name, contentAsJson);
        }

        public void UseShapeStorage(DragAndScrollMod mod)
        {

            EditorMode keepEditMode = mod.initializer.GetMode();

            shapeMemories = shapeMemories.Reverse().ToArray();

            mod.generators.Add(() =>
            {
                mod.input.Disable();
                EditorSquareSpawn[] editorSquareSpawns = FindObjectsByType<EditorSquareSpawn>(FindObjectsSortMode.InstanceID);
                foreach (EditorSquareSpawn item in editorSquareSpawns) item.targetPosition = item.originalPosition;
            });

            mod.generators.Add(() =>
            {
                mod.ClearAll(true);
                mod.permaLight.SetActive(WorldLight.WorldLights.Count == 0);
            });

            mod.generators.Add(() =>
            {
                mod.tabFlag = false;
                mod.CtrlFlag = false;
                mod.ShiftFlag = false;
                mod.isScrolling = false;
                mod.rightClick = false;
                mod.leftClick = false;
                mod.cameraPan = false;
                mod.isGenerating = false;

                mod.startPoint = null;
                mod.endPoint = null;
                mod.pivotResult = null;
                mod.liveAnchor = null;
                mod.splineDrag = null;

            });

            mod.generators.Add(() =>
            {
                mod.initializer.SetMode(EditorMode.Shaping);
            });

            foreach (ShapeMemory memory in shapeMemories)
            {
                mod.generators.Add(() => 
                {
                    
                    mod.mousePos = memory.StartPos;
                    mod.shapeContainerPrefab.mirrorX = memory.mirrorX;
                    mod.shapeContainerPrefab.mirrorY = memory.mirrorY;
                    mod.StartShapeGeneration(false);
                    mod.isGenerating = true;
                });

                for (int i = 0; i < framesGeneratingAShape; i++) mod.generators.Add(() =>
                {
                    mod.isGenerating = true;
                    mod.shapeContainerPrefab.mirrorX = memory.mirrorX;
                    mod.shapeContainerPrefab.mirrorY = memory.mirrorY;
                    mod.pivotResult.SetScale(memory.scale);
                    mod.mousePos = memory.EndPos;
                    mod.UpdateLiveGeneration(false);

                });

                mod.generators.Add(() =>
                {
                    mod.shapeContainerPrefab.mirrorX = memory.mirrorX;
                    mod.shapeContainerPrefab.mirrorY = memory.mirrorY;
                    mod.EndShapeGeneration(false);
                    mod.isGenerating = false;
                });

            }

            mod.generators.Add(() =>
            {
                mod.initializer.SetMode(EditorMode.Animating);
            });

            foreach (AnimationMemory memory in animationMemories)
            {
                mod.generators.Add(() =>
                {
                    AnimationAnchor anchor = Instantiate(mod.animationAnchorPrefab);
                    anchor.BuildFromData(memory);
                    mod.animators.Add(anchor);
                    if (mod.selectedAnimationAnchor) mod.selectedAnimationAnchor.selected = false;
                    mod.selectedAnimationAnchor = anchor;
                    anchor.selected = true;
                    anchor.splineSpeed = memory.speed;
                    anchor.splineOffset = memory.offset;
                });
            }

            mod.generators.Add(() =>
            {
                mod.initializer.SetMode(EditorMode.Lighting);
            });

            foreach (LightMemory memory in lightMemories)
            {
                mod.generators.Add(() =>
                {

                    WorldLight light = Instantiate(mod.worldLightPrefab);
                    light.SetTargetPos(memory.position.AsVector2());
                    mod.worldLights.Add(light);

                });
            }


            mod.generators.Add(() =>
            {

                EditorSquareSpawn[] editorSquareSpawns = FindObjectsByType<EditorSquareSpawn>(FindObjectsSortMode.InstanceID);
                ShapeMimicBehaviour.ShapeMimics.Clear();
                AnimationAnchor.AnimationAnchors.Clear();
                WorldLight.WorldLights.Clear();

                ShapeMimicBehaviour.ShapeIDCounter = 0;
                AnimationAnchor.AnimationIDCounter = 0;
                WorldLight.worldLightIDCounter = 0;
                
                List<ShapeContainer> helperListSC = mod.undoStack.Reverse().ToList();

                mod.initializer.SetMode(EditorMode.Shaping);
                for (int i = 0; i < shapeMemories.Length; i++)
                {
                    helperListSC[i].SetAllMimicsID(shapeMemories[i].oID, shapeMemories[i].xID, shapeMemories[i].yID, shapeMemories[i].xyID);
                    
                    helperListSC[i].SetAllMimicOffsets(shapeMemories[i].oOffsetPos, shapeMemories[i].xOffsetPos, shapeMemories[i].yOffsetPos, shapeMemories[i].xyOffsetPos);
                    ShapeMimicBehaviour.ShapeIDCounter = Math.Max(shapeMemories[i].oID, Math.Max(shapeMemories[i].xID, Math.Max(shapeMemories[i].yID, Math.Max(shapeMemories[i].xyID, ShapeMimicBehaviour.ShapeIDCounter))));
                }

                mod.initializer.SetMode(EditorMode.Lighting);
                for (int i = 0; i < lightMemories.Length; i++)
                {
                    mod.worldLights[i].OverrideID(lightMemories[i].lightId);
                    WorldLight.worldLightIDCounter = Math.Max(lightMemories[i].lightId, WorldLight.worldLightIDCounter);
                }
                
                mod.initializer.SetMode(EditorMode.Animating);
                for (int i = 0; i < animationMemories.Length; i++)
                {
                    mod.animators[i].OverrideID(animationMemories[i].animationId);
                    if (animationMemories[i].attachedMimicIDs != null) for (int j = 0; j < animationMemories[i].attachedMimicIDs.Length; j++) mod.animators[i].AttatchMimic(animationMemories[i].attachedMimicIDs[j]);
                    AnimationAnchor.AnimationIDCounter = Math.Max(animationMemories[i].animationId, AnimationAnchor.AnimationIDCounter);
                }

                for (int i = 0; i < spawnMemories.Length; i++) editorSquareSpawns[i].targetPosition = spawnMemories[i].pos.AsVector3();

                WorldLight.WorldLightCount = WorldLight.WorldLights.Count;
                WorldLight.worldLightIDCounter++;
                ShapeMimicBehaviour.ShapeIDCounter++;
                AnimationAnchor.AnimationIDCounter++;

                mod.permaLight.SetActive(WorldLight.WorldLights.Count == 0);

            });

            mod.generators.Add(() =>
            {

                mod.shapeSelector.PatchNativeMimicsArray();

                mod.input.Enable();
                mod.initializer.SetMode(keepEditMode);
                if (mod.selectedAnimationAnchor)
                {
                    mod.selectedAnimationAnchor.selected = false;
                    mod.selectedAnimationAnchor = null;
                }

            });

        }

    }

    [Preserve]
    [Serializable]
    public struct ShapeMemory
    {
        public int oID, xID, yID, xyID;
        public bool mirrorX, mirrorY;
        public CustomVec3 oOffsetPos, xOffsetPos, yOffsetPos, xyOffsetPos;
        public CustomVec3 startPos, endPos, genPos;
        public CustomVec3[] vertices;
        public float scale;

        [JsonIgnore]
        public Vector3 StartPos => startPos.AsVector3();
        [JsonIgnore]
        public Vector3 EndPos => endPos.AsVector3();
        [JsonIgnore]
        public Vector3 GenPos => genPos.AsVector3();

        public Vector3[] GetVerticesAsVector3Arr()
        {
            Vector3[] arr = new Vector3[vertices.Length];
            for (int i = 0; i < arr.Length; i++) arr[i] = vertices[i].AsVector3();
            return arr;
        }
        public void SetVerticesFromVector3Arr(Vector3[] arr)
        {
            vertices = new CustomVec3[arr.Length];
            for (int i = 0; i < arr.Length; i++) vertices[i] = arr[i];
        }
    }

    [Preserve]
    [Serializable]
    public struct AnimationMemory
    {
        public int animationId;
        public CustomVec2 startSegment;
        public CustomVec2[] segments;
        public int[] attachedMimicIDs;
        public float speed;
        public float offset;

        public Vector2[] GetSegmentsAsVector2Arr()
        {
            Vector2[] arr = new Vector2[segments.Length];
            for (int i = 0; i < arr.Length; i++) arr[i] = segments[i].AsVector2();
            return arr;
        }
        public void SetSegmentsFromVector3Arr(Vector2[] arr)
        {
            segments = new CustomVec2[arr.Length];
            for (int i = 0; i < arr.Length; i++) segments[i] = arr[i];
        }

    }

    [Preserve]
    [Serializable]
    public struct SpawnMemory
    {
        public CustomVec3 pos;
    }

    [Preserve]
    [Serializable]
    public struct LightMemory
    {
        public int lightId;
        public CustomVec2 position;
    }

    [Preserve]
    public struct CustomVec2
    {
        public float x;
        public float y;
        public Vector2 AsVector2() => new Vector2(x, y);
        public static implicit operator CustomVec2(Vector2 value) => new CustomVec2 { x = value.x, y = value.y };
        public static implicit operator CustomVec2(Vector3 value) => new CustomVec2 { x = value.x, y = value.y };
    }

    [Preserve]
    public struct CustomVec3
    {
        public float x;
        public float y;
        public float z;
        public Vector3 AsVector3() => new Vector3(x, y, z);
        public static implicit operator CustomVec3(Vector3 value) => new CustomVec3 { x = value.x, y = value.y, z = value.z};
        public static implicit operator CustomVec3(Vector2 value) => new CustomVec3 { x = value.x, y = value.y, z = 0 };
    }
    [Preserve]
    public struct MemoryKeeps
    {

        DragAndScrollMod mod;

        bool keepMirrorX, keepMirrorY, keepIsGenerating;
        Vector2 keepMousePos;
        LocalSnappingPoint keepStartPoint, keepEndPoint;
        ShapeContainer keepPivot;
        EditorMode editorMode;

        public MemoryKeeps(DragAndScrollMod mod)
        {
            this.mod = mod;
            keepMirrorX = false;
            keepMirrorY = false;
            keepIsGenerating = false;
            keepMousePos = new Vector2();
            keepStartPoint = null;
            keepEndPoint = null;
            keepPivot = null;
            editorMode = EditorMode.Shaping;
        }

        public void AssignKeeps()
        {
            editorMode = mod.initializer.GetMode();
            keepMirrorX = mod.shapeContainerPrefab.mirrorX;
            keepMirrorY = mod.shapeContainerPrefab.mirrorY;
            keepMousePos = mod.mousePos;
            if (mod.isGenerating)
            {
                keepIsGenerating = mod.isGenerating;
                keepStartPoint = mod.startPoint;
                keepEndPoint = mod.endPoint;
                keepPivot = mod.pivotResult;
            }
        }
        public void ApplyKeeps()
        {
            mod.initializer.SetMode(editorMode);
            mod.shapeContainerPrefab.mirrorX = keepMirrorX;
            mod.shapeContainerPrefab.mirrorY = keepMirrorY;
            if (keepIsGenerating)
            {
                mod.isGenerating = keepIsGenerating;
                mod.mousePos = keepMousePos;
                mod.startPoint = keepStartPoint;
                mod.endPoint = keepEndPoint;
                mod.pivotResult = keepPivot;
            }
        }

    }
}
