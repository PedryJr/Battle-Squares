using System;
using System.Runtime.CompilerServices; 
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static DragAndScrollMod;

public sealed class ShapeContainer : MonoBehaviour
{

    public Vector2 mousePosOnGenerate = Vector2.zero;
    public Vector2 mousePosOnRelease = Vector2.zero;

    private bool released = false;

    [SerializeField] private Material _material;
    [SerializeField] private Transform _mimicPrefab;
    [SerializeField] private ProximityPixelSenssor _editEffect;

    private LocalSnappingPoint _pointA;
    private LocalSnappingPoint _pointB;
    public DragAndScrollMod _dragMod;
    private Transform _mimicInstance;
    private PolygonCollider2D _polygonCollider;
    private ShadowCaster2D _shadowCaster;
    private TMP_Text _shapeCountDisplay;
    ShapeMimicBehaviour _shapeMimicBehaviour;
    ProximityPixelSenssor[] _mimicProximityPixels;

    const float ARENA_SIZE = 256f;

    private RenderParams _renderParams;
    private Mesh _mesh;
    private const float TRANSITION_SPEED = 20f;
    private static int _shapeCount = 0;

    private Vector3[] _vertices = Array.Empty<Vector3>();
    private Vector3[] _mimicVertices = Array.Empty<Vector3>();
    private int[] _triangleIndices = Array.Empty<int>();

    private Transform _mimicMirrorX;
    private Transform _mimicMirrorY;
    private Transform _mimicMirrorXY;

    private Vector3[] _mirrorXVertices = Array.Empty<Vector3>();
    private Vector3[] _mirrorYVertices = Array.Empty<Vector3>();
    private Vector3[] _mirrorXYVertices = Array.Empty<Vector3>();
    private Mesh _meshMirrorX;
    private Mesh _meshMirrorY;
    private Mesh _meshMirrorXY;
    private PolygonCollider2D _colliderMirrorX;
    private PolygonCollider2D _colliderMirrorY;
    private PolygonCollider2D _colliderMirrorXY;
    private ProximityPixelSenssor[] _mimicProximityPixelsX;
    private ProximityPixelSenssor[] _mimicProximityPixelsY;
    private ProximityPixelSenssor[] _mimicProximityPixelsXY;
    ShapeMimicBehaviour _shapeMimicBehaviourMirrorX;
    ShapeMimicBehaviour _shapeMimicBehaviourMirrorY;
    ShapeMimicBehaviour _shapeMimicBehaviourMirrorXY;

    public bool mirrorX;
    public bool mirrorY;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Awake()
    {
        InitializeRendering();
        CreateMimicInstance();
        CreateMirrorMimics();
        UpdateShapeCounter();
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update()
    {
        if (released) return;
        if (!overrideVertexReload)
        {
            UpdateMimicTransform();
            HandleMeshReload();
        }
        RenderMesh();
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ManualUpdate() => RenderMesh();

    bool overrideVertexReload = false;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3[] GetCurrentVertices() => _mimicVertices;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float3[] GetCurrentVerticesAsFloat3()
    {
        float3[] arr = new float3[_mimicVertices.Length];
        for (int i = 0; i < arr.Length; i++) arr[i] = _mimicVertices[i];
        return arr;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ManualVertexReload(Vector3[] overrideVerts)
    {
        UpdateMimicTransform();
        _vertices = overrideVerts;
        _mimicVertices = overrideVerts;
        UpdateMirrorMimics();
        overrideVertexReload = true;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RegenerateMesh()
    {
        Vector3[] pointsA = GetSafeLocalPoints(_pointA);
        Vector3[] pointsB = GetSafeLocalPoints(_pointB);
        Vector3[] combinedPoints = CombinePoints(pointsA, pointsB);

        Triangulate(combinedPoints, out _vertices, out _triangleIndices);

        InitializeMimicVertices();
        UpdateMirrorMimics();
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Vector3[] MirrorVertices(Vector3[] vertices, bool mirrorX, bool mirrorY)
    {
        Vector3[] mirrored = new Vector3[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 v = _mimicVertices[i];
            mirrored[i] = new Vector3(
                mirrorX ? -v.x : v.x,
                mirrorY ? -v.y : v.y,
                v.z
            );
        }
        return mirrored;
    }
    [MethodImpl(512)]
    private void CreateMirrorMimics()
    {
        if (_mimicPrefab == null) return;

        if (mirrorX)
        {
            _mimicMirrorX = Instantiate(_mimicPrefab);
            _mimicMirrorX.transform.position = Vector3.zero;
            _colliderMirrorX = _mimicMirrorX.GetComponent<PolygonCollider2D>();
            _mimicProximityPixelsX = _mimicMirrorX.GetComponentsInChildren<ProximityPixelSenssor>();
            _meshMirrorX = new Mesh();
            _meshMirrorX.MarkDynamic();
            _shapeMimicBehaviourMirrorX = _mimicMirrorX.GetComponent<ShapeMimicBehaviour>();
            _shapeMimicBehaviourMirrorX.AssignShapeContainer(this);
            EnableSensorOnMimic(_mimicProximityPixelsX);
        }

        if (mirrorY)
        {
            _mimicMirrorY = Instantiate(_mimicPrefab);
            _mimicMirrorY.transform.position = Vector3.zero;
            _colliderMirrorY = _mimicMirrorY.GetComponent<PolygonCollider2D>();
            _mimicProximityPixelsY = _mimicMirrorY.GetComponentsInChildren<ProximityPixelSenssor>();
            _meshMirrorY = new Mesh();
            _meshMirrorY.MarkDynamic();
            _shapeMimicBehaviourMirrorY = _mimicMirrorY.GetComponent<ShapeMimicBehaviour>();
            _shapeMimicBehaviourMirrorY.AssignShapeContainer(this);
            EnableSensorOnMimic(_mimicProximityPixelsY);
        }

        if (mirrorX && mirrorY)
        {
            _mimicMirrorXY = Instantiate(_mimicPrefab);
            _mimicMirrorXY.transform.position = Vector3.zero;
            _colliderMirrorXY = _mimicMirrorXY.GetComponent<PolygonCollider2D>();
            _mimicProximityPixelsXY = _mimicMirrorXY.GetComponentsInChildren<ProximityPixelSenssor>();
            _meshMirrorXY = new Mesh();
            _meshMirrorXY.MarkDynamic();
            _shapeMimicBehaviourMirrorXY = _mimicMirrorXY.GetComponent<ShapeMimicBehaviour>();
            _shapeMimicBehaviourMirrorXY.AssignShapeContainer(this);
            EnableSensorOnMimic(_mimicProximityPixelsXY);
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DisableSensorsOnMimic(ProximityPixelSenssor[] sensors)
    {
        if(sensors != null) foreach (var s in sensors) if(s) s.enabled = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnableSensorOnMimic(ProximityPixelSenssor[] sensors)
    {
        foreach (var s in sensors) s.enabled = true;
    }
    [MethodImpl(512)]
    private void UpdateMirrorMimics()
    {

        UpdateMesh(_mimicVertices, _mesh, _polygonCollider, _mimicInstance, _mimicProximityPixels);

        if (mirrorX && _mimicMirrorX != null)
        {
            _mirrorXVertices = MirrorVertices(_vertices, true, false);
            UpdateMesh(_mirrorXVertices, _meshMirrorX, _colliderMirrorX, _mimicMirrorX, _mimicProximityPixelsX);
        }

        if (mirrorY && _mimicMirrorY != null)
        {
            _mirrorYVertices = MirrorVertices(_vertices, false, true);
            UpdateMesh(_mirrorYVertices, _meshMirrorY, _colliderMirrorY, _mimicMirrorY, _mimicProximityPixelsY);
        }

        if (mirrorX && mirrorY && _mimicMirrorXY != null)
        {
            _mirrorXYVertices = MirrorVertices(_vertices, true, true);
            UpdateMesh(_mirrorXYVertices, _meshMirrorXY, _colliderMirrorXY, _mimicMirrorXY, _mimicProximityPixelsXY);
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (int, int, int, int) GetAllMimicsID() => 
        (
        GetMimicID(_shapeMimicBehaviour), 
        mirrorX ? GetMimicID(_shapeMimicBehaviourMirrorX) : -1, 
        mirrorY ? GetMimicID(_shapeMimicBehaviourMirrorY) : -1, 
        mirrorX && mirrorY ? GetMimicID(_shapeMimicBehaviourMirrorXY) : -1);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetAllMimicsID(int oID, int xID, int yID, int xyID)
    {
        SetMimicID(_shapeMimicBehaviour, oID);
        if(xID != -1) SetMimicID(_shapeMimicBehaviourMirrorX, xID);
        if (yID != -1) SetMimicID(_shapeMimicBehaviourMirrorY, yID);
        if (xyID != -1) SetMimicID(_shapeMimicBehaviourMirrorXY, xyID);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (CustomVec3, CustomVec3, CustomVec3, CustomVec3) GetAllMimicsOffsets() =>
    (
    GetMimicOffset(_shapeMimicBehaviour),
    mirrorX ? GetMimicOffset(_shapeMimicBehaviourMirrorX) : new CustomVec3(),
    mirrorY ? GetMimicOffset(_shapeMimicBehaviourMirrorY) : new CustomVec3(),
    mirrorX && mirrorY ? GetMimicOffset(_shapeMimicBehaviourMirrorXY) : new CustomVec3());
    public void SetAllMimicOffsets(CustomVec3 oOffsetPos, CustomVec3 xOffsetPos, CustomVec3 yOffsetPos, CustomVec3 xyOffsetPos)
    {
        SetMimicOffset(_shapeMimicBehaviour, oOffsetPos);
        if (_shapeMimicBehaviourMirrorX) SetMimicOffset(_shapeMimicBehaviourMirrorX, xOffsetPos);
        if (_shapeMimicBehaviourMirrorY) SetMimicOffset(_shapeMimicBehaviourMirrorY, yOffsetPos);
        if (_shapeMimicBehaviourMirrorXY) SetMimicOffset(_shapeMimicBehaviourMirrorXY, xyOffsetPos);
    }
    CustomVec3 GetMimicOffset(ShapeMimicBehaviour shapeMimic)
        => new CustomVec3
        {
            x = shapeMimic.offsetPosition.x,
            y = shapeMimic.offsetPosition.y,
            z = shapeMimic.offsetPosition.z
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetMimicOffset(ShapeMimicBehaviour shapeMimicBehaviour, CustomVec3 offset) => shapeMimicBehaviour.offsetPosition = offset.AsVector3();
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    int GetMimicID(ShapeMimicBehaviour mimic) => mimic.ShapeID;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void SetMimicID(ShapeMimicBehaviour mimic, int ID)
    {

        if (!mimic.OverrideID(ID))
        {
            ID++;
            _dragMod.AddGenerator(() =>
            {
                if (!mimic.OverrideID(ID))
                {
                    SetMimicID(mimic, ID);
                }
                ShapeMimicBehaviour.ShapeIDCounter = Math.Max(ShapeMimicBehaviour.ShapeIDCounter, ID + 1);
            });

        }
    }

    [MethodImpl(512)]
    private void UpdateMesh(Vector3[] vertices, Mesh mesh, PolygonCollider2D collider, Transform mimic, ProximityPixelSenssor[] proximityPixelSenssors)
    {
        if (mesh == null || collider == null) return;

        Vector3[] meshVerts = new Vector3[vertices.Length];
        Vector2[] colliderPoints = new Vector2[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            proximityPixelSenssors[i].transform.position = vertices[i] + mimic.position;
            meshVerts[i] = vertices[i] + mimic.position;
            colliderPoints[i] = vertices[i];
        }

        mesh.SetVertices(meshVerts);
        mesh.SetTriangles(_triangleIndices, 0);
        mesh.MarkModified();
        mesh.bounds = new Bounds(Vector3.zero, new float3(ARENA_SIZE));

        collider.SetPath(0, colliderPoints);
        collider.points = colliderPoints;
        collider.offset = new Vector2(0f, 0f);
    }

    private void OnDestroy()
    {
        CleanUpResources();
    }

    private void InitializeRendering()
    {
        _shapeCount++;
        if (mirrorX) _shapeCount++;
        if (mirrorY) _shapeCount++;
        if (mirrorX && mirrorY) _shapeCount++;

        _renderParams = new RenderParams
        {
            material = _material,
            layer = 0,
            renderingLayerMask = 0,
            camera = Camera.main,
            shadowCastingMode = ShadowCastingMode.On,
            receiveShadows = true,
        };
        _shapeCountDisplay = GameObject.Find("Shape Stat")?.GetComponent<TMP_Text>();
    }

    private void CreateMimicInstance()
    {
        if (_mimicPrefab == null) return;

        _mimicInstance = Instantiate(_mimicPrefab);
        _polygonCollider = _mimicInstance.GetComponent<PolygonCollider2D>();
        _shadowCaster = _mimicInstance.GetComponent<ShadowCaster2D>();
        _mimicProximityPixels = _mimicInstance.GetComponentsInChildren<ProximityPixelSenssor>();
        _shapeMimicBehaviour = _mimicInstance.GetComponent<ShapeMimicBehaviour>();
        EnableSensorOnMimic(_mimicProximityPixels);

        _mesh = new Mesh();
        _mesh.MarkDynamic();
        _mimicInstance.GetComponent<ShapeMimicBehaviour>().AssignShapeContainer(this);

        if (_shadowCaster != null)
        {
            _shadowCaster.castingOption = ShadowCaster2D.ShadowCastingOptions.CastAndSelfShadow;
            _shadowCaster.RegisterShadowCaster2D(_shadowCaster);
            _shadowCaster.Update();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void HandleMeshReload()
    {
        if (ShouldReloadMesh()) RegenerateMesh();
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool ShouldReloadMesh()
    {
        if (!_pointA) return false;
        if (!_pointB) return false;
        if (_pointA.HasChanged) return true;
        if (_pointB.HasChanged) return true;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Vector3[] GetSafeLocalPoints(LocalSnappingPoint point) => point.GetLocalPoints(_mimicInstance);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Vector3[] CombinePoints(Vector3[] pointsA, Vector3[] pointsB)
    {
        Vector3[] combined = new Vector3[pointsA.Length + pointsB.Length];
        pointsA.CopyTo(combined, 0);
        pointsB.CopyTo(combined, pointsA.Length);
        return combined;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Triangulate(Vector3[] points, out Vector3[] vertices, out int[] triangles) => PolygonTriangulator.Triangulate8(points, out vertices, out triangles);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void InitializeMimicVertices()
    {
        if (_mimicVertices.Length != _vertices.Length) _mimicVertices = new Vector3[_vertices.Length];
    }

    public float4x4 GetGeneratingMatrix() => float4x4.TRS(new float3(0f, 0f, -3f), quaternion.identity, new float3(1f, 1f, 1f));

    private void RenderMesh()
    {
        if (!_dragMod) return;
        if (released) return;

        float4x4 modelMatrix = GetGeneratingMatrix();

        _renderParams.layer = _dragMod.layer;
        _renderParams.renderingLayerMask = _dragMod.renderLayer;


        Render(_mesh, modelMatrix);
        if (mirrorX) Render(_meshMirrorX, modelMatrix);
        if (mirrorY) Render(_meshMirrorY, modelMatrix);
        if (mirrorX && mirrorY) Render(_meshMirrorXY, modelMatrix);

    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void Render(Mesh mesh, float4x4 modelMatrix) => Graphics.RenderMesh(_renderParams, mesh, 0, modelMatrix);

    [MethodImpl(512)]
    private void UpdateMimicTransform()
    {

        if (_mimicInstance == null) return;

        _mimicInstance.rotation = transform.rotation;
        _mimicInstance.position = transform.position;
        _mimicInstance.localScale = transform.localScale;

        Vector3 pos;

        if(mirrorX)
        {

            pos = _mimicInstance.position;
            pos.x *= -1;

            _mimicMirrorX.position = pos;
            _mimicMirrorX.rotation = _mimicInstance.rotation;
            _mimicMirrorX.localScale = _mimicInstance.localScale;
        }

        if (mirrorY)
        {


            pos = _mimicInstance.position;
            pos.y *= -1;

            _mimicMirrorY.position = pos;
            _mimicMirrorY.rotation = _mimicInstance.rotation;
            _mimicMirrorY.localScale = _mimicInstance.localScale;
        }

        if (mirrorX && mirrorY)
        {

            pos = _mimicInstance.position;
            pos.y *= -1;
            pos.x *= -1;
            _mimicMirrorXY.position = pos;
            _mimicMirrorXY.rotation = _mimicInstance.rotation;
            _mimicMirrorXY.localScale = _mimicInstance.localScale;
        }

        UpdateVertexPositions();
    }

    [MethodImpl(512)]
    private void UpdateVertexPositions()
    {

        int vertexCount = Mathf.Min(_mimicVertices.Length, _vertices.Length);
        for (int i = 0; i < vertexCount; i++)
        {
            _mimicVertices[i] = Vector3.Lerp(
                _mimicVertices[i],
                _vertices[i],
                Time.deltaTime * TRANSITION_SPEED
            );

            if (mirrorX)
            {
                Vector3 vert = _vertices[i];
                vert.x *= -1;

                _mirrorXVertices[i] = Vector3.Lerp(
                _mirrorXVertices[i],
                vert,
                Time.deltaTime * TRANSITION_SPEED
                );
            }

            if (mirrorY)
            {
                Vector3 vert = _vertices[i];
                vert.y *= -1;

                _mirrorYVertices[i] = Vector3.Lerp(
                _mirrorYVertices[i],
                vert,
                Time.deltaTime * TRANSITION_SPEED
                );
            }

            if (mirrorX && mirrorY)
            {
                Vector3 vert = _vertices[i];
                vert.x *= -1;
                vert.y *= -1;

                _mirrorXYVertices[i] = Vector3.Lerp(
                _mirrorXYVertices[i],
                vert,
                Time.deltaTime * TRANSITION_SPEED
                );
            }

            if (_dragMod != null)
            {
                _mimicVertices[i].z = _dragMod.transform.position.z;
            }
        }

        _shapeMimicBehaviour.StreamShadowVerts(_mesh.vertices, true);
        if(mirrorX) _shapeMimicBehaviourMirrorX.StreamShadowVerts(_meshMirrorX.vertices, true);
        if(mirrorY) _shapeMimicBehaviourMirrorY.StreamShadowVerts(_meshMirrorY.vertices, true);
        if(mirrorX && mirrorY) _shapeMimicBehaviourMirrorXY.StreamShadowVerts(_meshMirrorXY.vertices, true);

    }

    [MethodImpl(512)]
    public void SnapToCurrentTransform()
    {

        if (_mimicInstance == null) return;

        released = true;

        _mimicInstance.rotation = transform.rotation;
        _mimicInstance.position = transform.position;
        _mimicInstance.localScale = transform.localScale;

        int vertexCount = Mathf.Min(_mimicVertices.Length, _vertices.Length);
        for (int i = 0; i < vertexCount; i++) _mimicVertices[i] = _vertices[i];

        RegenerateMesh();
        UpdateMimicTransform();
        DisableForcefieldsOnMimics();
        MakeMimicMeshAuthor();
    }

    void MakeMimicMeshAuthor()
    {
        ReleaseMimimc(_mimicInstance);
        if (mirrorX) ReleaseMimimc(_mimicMirrorX);
        if (mirrorY) ReleaseMimimc(_mimicMirrorY);
        if (mirrorX && mirrorY) ReleaseMimimc(_mimicMirrorXY);
    }

    void ReleaseMimimc(Transform _mimic) => _mimic.GetComponent<ShapeMimicBehaviour>().RegisterRelease(_triangleIndices, _dragMod);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DisableForcefieldsOnMimics()
    {
        DisableSensorsOnMimic(_mimicProximityPixels);
        if (mirrorX) DisableSensorsOnMimic(_mimicProximityPixelsX);
        if (mirrorY) DisableSensorsOnMimic(_mimicProximityPixelsY);
        if (mirrorX && mirrorY) DisableSensorsOnMimic(_mimicProximityPixelsXY);
    }

    public void EnableForceFieldOnMimics()
    {
        EnableSensorOnMimic(_mimicProximityPixels);
        if (mirrorX) EnableSensorOnMimic(_mimicProximityPixelsX);
        if (mirrorY) EnableSensorOnMimic(_mimicProximityPixelsY);
        if (mirrorX && mirrorY) EnableSensorOnMimic(_mimicProximityPixelsXY);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AssignSnappingPoints(LocalSnappingPoint pointA, LocalSnappingPoint pointB) => (_pointA, _pointB) = (pointA, pointB);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (LocalSnappingPoint, LocalSnappingPoint) GetSnappingPoints() => (_pointA, _pointB);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AssignDragMod(DragAndScrollMod dragMod)
    {
        if (_dragMod) return;
        _dragMod = dragMod;
        //_dragMod.OnShaapeSpawn();
    }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnRelease()
    {
        SnapToCurrentTransform();
        Destroy(_pointA.gameObject);
        Destroy(_pointB.gameObject);
        
        //foreach (var item in _mimicProximityPixels) Destroy(item.gameObject);
        Destroy(_mesh);

        if(mirrorX)
        {
            //foreach (var item in _mimicProximityPixelsX) Destroy(item.gameObject);
            Destroy(_meshMirrorX);
        }
        if(mirrorY)
        {
            //foreach (var item in _mimicProximityPixelsY) Destroy(item.gameObject);
            Destroy(_meshMirrorY);
        }
        if(mirrorX && mirrorY)
        {
            //foreach (var item in _mimicProximityPixelsXY) Destroy(item.gameObject);
            Destroy(_meshMirrorXY);
        }
        enabled = false;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CleanUpMirrorResources()
    {
        DestroyMimic(_mimicMirrorX, _meshMirrorX);
        DestroyMimic(_mimicMirrorY, _meshMirrorY);
        DestroyMimic(_mimicMirrorXY, _meshMirrorXY);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CleanUpResources()
    {
        _shapeCount--;
        if (mirrorX) _shapeCount--;
        if (mirrorY) _shapeCount--;
        if (mirrorX && mirrorY) _shapeCount--;

        UpdateShapeCounter();
        CleanUpMirrorResources();

        if (_mimicInstance != null) Destroy(_mimicInstance.gameObject);

        if (_mesh != null) Destroy(_mesh);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DestroyMimic(Transform mimic, Mesh mesh)
    {
        if (mimic != null) Destroy(mimic.gameObject);
        if (mesh != null) Destroy(mesh);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateShapeCounter()
    {
        if (_shapeCountDisplay != null) _shapeCountDisplay.text = $"Shapes: {_shapeCount}";
    }

    public float scale;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void ChangeScale(float v)
    {
        scale = _pointA.AdjustScale(v);
        _pointB.AdjustScale(v);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetScale(float v)
    {
        scale = _pointA.HardAdjustScale(v);
        _pointB.HardAdjustScale(v);
    }
}