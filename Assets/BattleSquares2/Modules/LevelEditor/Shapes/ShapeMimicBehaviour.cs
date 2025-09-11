using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static BinaryVectors;
using static ShapeMimicBehaviour;

public sealed class ShapeMimicBehaviour : MonoBehaviour
{
    public static Mesh sharedMesh = null;
    public static Dictionary<int, ShapeMimicBehaviour> ShapeMimics = new Dictionary<int, ShapeMimicBehaviour>(2048);
    public static int ShapeIDCounter = 0;

    private int shapeID;

    [SerializeField]
    Color mimicColor = Color.white;

    Color normalColor = Color.white;
    Color pingedColor = new Color(0.8f, 0.8f, 0.8f);

    public Vector3 originalPosition = Vector3.zero;
    public Vector3 offsetPosition = Vector3.zero;

    DragAndScrollMod _dragMod;
    Transform cachedTransform;
    MeshRenderer meshRenderer;
    PolygonCollider2D polygonCollider2D;
    ShapeContainer shapeContainer;
    AnimationAnchor animationAnchor;
    MaterialPropertyBlock propertyBlock;
    ShadowCaster2DController shadowCasterController;
    ShadowCaster2D shadowCaster;
    MeshFilter meshFilter;

    private void Awake()
    {

        shadowCaster = GetComponent<ShadowCaster2D>();
        shadowCasterController = GetComponent<ShadowCaster2DController>();
        meshRenderer = GetComponent<MeshRenderer>();
        meshFilter = GetComponent<MeshFilter>();
        polygonCollider2D = GetComponent<PolygonCollider2D>();

        propertyBlock = new MaterialPropertyBlock();
        cachedTransform = transform;
    }
    public void StreamShadowVerts(Vector3[] arr, bool subtractPosition = false)
    {
        if(subtractPosition) for (int i = 0; i < arr.Length; i++) arr[i] = arr[i] - cachedTransform.position;
        shadowCasterController.UpdateShadowFromPoints(arr);
    }

    Vector2[] points;
    [SerializeField]
    Vector3[] vertices;
    Vector3[] scam;
    [SerializeField]
    ushort[] meshIndices;

 
    bool lastStaticState = false;
    public bool staticShape = true;
    public int ShapeID => shapeID;
    private bool pinged = false;
    public bool OverrideID(int ID)
    {
        bool sucess = true;
        if (ShapeMimics.ContainsKey(ID))
        {
            sucess = false;
            _dragMod.brokenMimicsDictionaryFlag = true;
        }
        else
        {
            int oldID = shapeID;
            shapeID = ID;
            ShapeMimics.Add(shapeID, this);
            _dragMod.OnShapeIDChange(this, oldID, ShapeID);
        }
        return sucess;
    }

    public void RegisterRelease(int[] triangles, DragAndScrollMod dragmod)
    {
        _dragMod = dragmod;
        shapeID = ShapeIDCounter;
        if(ShapeMimics.ContainsKey(shapeID)) ShapeMimics[shapeID] = this;
        else ShapeMimics.Add(shapeID, this);

        ShapeIDCounter++;

        points = polygonCollider2D.points;
        vertices = new Vector3[points.Length];
        scam = new Vector3[points.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = points[i];
            scam[i] = points[i] * 0.0001f;
        }

        shadowCasterController.UpdateShadowFromPoints(points);

        if (!sharedMesh)
        {
            sharedMesh = new Mesh();
            sharedMesh.vertices = scam;
            sharedMesh.triangles = triangles;
            sharedMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1000);
        }

        meshIndices = new ushort[triangles.Length];
        for (int i = 0; i < meshIndices.Length; i++) meshIndices[i] = (ushort) triangles[i];

        meshFilter.sharedMesh = sharedMesh;

        reorderedVerts = new ReorderedVerts[8]
        {
            new ReorderedVerts { pos = new Vector4(points[0].x, points[0].y, -2.5f, 1f), index = 0 },
            new ReorderedVerts { pos = new Vector4(points[1].x, points[1].y, -2.5f, 1f), index = 1 },
            new ReorderedVerts { pos = new Vector4(points[2].x, points[2].y, -2.5f, 1f), index = 2 },
            new ReorderedVerts { pos = new Vector4(points[3].x, points[3].y, -2.5f, 1f), index = 3 },
            new ReorderedVerts { pos = new Vector4(points[4].x, points[4].y, -2.5f, 1f), index = 4 },
            new ReorderedVerts { pos = new Vector4(points[5].x, points[5].y, -2.5f, 1f), index = 5 },
            new ReorderedVerts { pos = new Vector4(points[6].x, points[6].y, -2.5f, 1f), index = 6 },
            new ReorderedVerts { pos = new Vector4(points[7].x, points[7].y, -2.5f, 1f), index = 7 },
        };

        for (int i = 0; i < reorderedVerts.Length; i++)
        {
            propertyBlock.SetVector($"_Pos{i}", reorderedVerts[i].pos);
        }
        meshRenderer.SetPropertyBlock(propertyBlock);

        enabled = true;
        normalColor = mimicColor;
        originalPosition = transform.position;
        dragmod.OnShaapeSpawn(this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AssignShapeContainer(ShapeContainer shapeContainer) => this.shapeContainer = shapeContainer;
    public void AssignAnimationAnchor(AnimationAnchor animationAnchor) => this.animationAnchor = animationAnchor;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ShapeContainer GetShapeContainer() => shapeContainer;

    [Serializable]
    public struct ReorderedVerts
    {
        public Vector4 pos;
        public int index;
    }

    [SerializeField]
    ReorderedVerts[] reorderedVerts;

    Color oldColor = Color.white;

    [SerializeField]
    LayerMask worldLayer;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update()
    {

        VisualizePinged();
        ApplyMimicColor();
        UpdateColor();
        UpdateStaticState();
        if(_dragMod) UpdatePositionWithOffset();

    }

    private void UpdatePositionWithOffset()
    {
        if (animationAnchor)
        {
            if (animationAnchor.previewEnabled) return;
        }
        Vector3 currentPosition = transform.position;
        Vector3 targetPosition = GetSnappedPosition(originalPosition + offsetPosition);
        Vector3 moveToPosition = Vector3.Lerp(currentPosition, targetPosition, Time.deltaTime * AnimationAnchor.animationSpeed);
        transform.position = moveToPosition;

    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    Vector2 GetSnappedPosition(Vector2 rawPosition) => new Vector2(Mathf.Round(rawPosition.x / _dragMod.Snapping) * _dragMod.Snapping, Mathf.Round(rawPosition.y / _dragMod.Snapping) * _dragMod.Snapping);

    public void ValidateShadow()
    {

    }

    void UpdateStaticState()
    {
        staticShape = animationAnchor == null;
        if(lastStaticState != staticShape)
        {

            if (staticShape) SetStatic();
            else SetDynamic();

            lastStaticState = staticShape;
        }
        Vector3 keepSpace = cachedTransform.position;
        keepSpace.z = 0;
        cachedTransform.position = keepSpace;
    }

    public void SetStatic()
    {
        cachedTransform.SetParent(LevelEditorInitializer.StaticShapeParent, true);
        ValidateShadow();
    }

    Vector2[] GetValidShadowPoints()
    {

        float resolution = 10;

        List<Vector2> validShadowPoints = new List<Vector2>();
        Vector2 a, b;

        for (int i = 1; i < points.Length; i++)
        {
            a = points[i - 1];
            b = points[i];
            if (!ArePointsOccluded(a, b))
            {
                if(!validShadowPoints.Contains(a)) validShadowPoints.Add(a);
                if(!validShadowPoints.Contains(b)) validShadowPoints.Add(b);
            }
        }

        a = points[points.Length - 1];
        b = points[0];
        if (!ArePointsOccluded(a, b))
        {
            if (!validShadowPoints.Contains(a)) validShadowPoints.Add(a);
            if (!validShadowPoints.Contains(b)) validShadowPoints.Add(b);
        }

        bool ArePointsOccluded(Vector2 a, Vector2 b)
        {

            int tempResolution = Mathf.FloorToInt(Vector2.Distance(a, b) * resolution);

            for(float step = 0; step < 1f; step += 1f / tempResolution)
            {

                Vector2 testPoint = Vector2.Lerp(a, b, step) + (Vector2)transform.position;
                Vector2 toLight1 = new Vector2(0f, 10f) - testPoint;
                Vector2 toLight2 = new Vector2(14f, 10f) - testPoint;
                Vector2 toLight3 = new Vector2(-14f, 10f) - testPoint;

                RaycastHit2D[] hits1 = Physics2D.RaycastAll(testPoint, toLight1, toLight1.magnitude, worldLayer);
                RaycastHit2D[] hits2 = Physics2D.RaycastAll(testPoint, toLight2, toLight2.magnitude, worldLayer);
                RaycastHit2D[] hits3 = Physics2D.RaycastAll(testPoint, toLight3, toLight3.magnitude, worldLayer);

                if (!(DidHitOtherMimic(hits1) && DidHitOtherMimic(hits2) && DidHitOtherMimic(hits3))) return false;
            }

            bool DidHitOtherMimic(RaycastHit2D[] hits)
            {
                if(hits == null) { return false; };
                if(hits.Length == 0) { return false; };
                foreach (var item in hits) if (item.transform != transform) if (item.transform.TryGetComponent(out ShapeMimicBehaviour foundMimic)) if(foundMimic.staticShape) return true;
                return false;

            }

            return true;

        }

        return validShadowPoints.ToArray();

    }

    public void SetDynamic()
    {
        cachedTransform.SetParent(null, true);
        ValidateShadow();
    }

    void UpdateColor()
    {
        if (oldColor != mimicColor) meshRenderer.SetPropertyBlock(propertyBlock);
        oldColor = mimicColor;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnDestroy()
    {
        EnsureNoAnimation();
        _dragMod.OnShaapeDespawn(this);
        ShapeMimics.Remove(shapeID);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PingSelected() => pinged = true;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void VisualizePinged()
    {
        mimicColor = Color.Lerp(mimicColor, pinged ? pingedColor : normalColor, Time.deltaTime * AnimationAnchor.animationSpeed);
        if (pinged) pinged = false;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void ApplyMimicColor() => propertyBlock.SetColor("_MyColor", mimicColor);
    public void EnsureNoAnimation()
    {
        if (!animationAnchor) return;
        animationAnchor.EnsureDetatchMimic(shapeID);
        animationAnchor = null;
    }


    [SerializeField]
    Vector2[] shapeAsOctagon;

    public Mesh GenerateWorldspaceMesh()
    {

        Mesh mesh = new Mesh();

        Vector3[] worldspaceVertices = new Vector3[8];
        for (int i = 0; i < worldspaceVertices.Length; i++) worldspaceVertices[i] = (Vector2)reorderedVerts[i].pos + (Vector2)transform.position;

        mesh.SetVertices(worldspaceVertices);
        mesh.SetTriangles(sharedMesh.triangles, 0);

        return mesh;

    }

    public bool useNoAlloc = false;

    public const float GetMinRot = -180f;
    public const float GetMaxRot = 180f;
    public const float GetMinLength = 0f;
    public const float GetMaxLength = 360.62445f;
    public const float GetMinWidth = 0f;
    public const float GetMaxWidth = 32f;
    public const byte GetRotBytes = 2;
    public const byte GetLenBytes = 2;
    public const byte GetWidBytes = 2;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Byte3 GetEmptyByte3() => new Byte3();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SimplifiedShapeData ConvertFromUSimplifiedShapeData(USimplifiedShapeData uSimplifiedShapeData)
    {

        return new SimplifiedShapeData
        {
            coord = uSimplifiedShapeData.coord,
            param = new SByte3()
            {
                min = new Vector3(GetMinRot, GetMinLength, GetMinWidth),
                max = new Vector3(GetMaxRot, GetMaxLength, GetMaxWidth),
                xBytes = GetRotBytes,
                yBytes = GetLenBytes,
                zBytes = GetWidBytes,
                byteVec = uSimplifiedShapeData.param
            }
        };

    }

    public static USimplifiedShapeData ConvertFromSimplifiedShapeData(SimplifiedShapeData uSimplifiedShapeData)
    {

        return new USimplifiedShapeData
        {
            coord = uSimplifiedShapeData.coord,
            param = uSimplifiedShapeData.param.byteVec
        };

    }

    public SimplifiedShapeData GetSimplifiedShapeData()
    {

        SimplifiedShapeData simplifiedShapeData = new SimplifiedShapeData();

        Vector2 shapeH = Vector2.zero;
        Vector2 shapeV = Vector2.zero;

        Vector2 ignoreH = Vector2.zero;
        Vector2 ignoreV = Vector2.zero;

        shapeH = points[7] - points[0];
        shapeV = points[2] - points[1];
        ignoreH = (shapeAsOctagon[7] - shapeAsOctagon[0]);
        ignoreV = (shapeAsOctagon[2] - shapeAsOctagon[1]);

        float rot, len, wid;
        rot = Mathf.Atan2(shapeH.y, shapeH.x) * Mathf.Rad2Deg;
        len = shapeH.magnitude - ignoreH.magnitude;
        wid = shapeV.magnitude - ignoreV.magnitude;
        byte xBytes = GetRotBytes;
        byte yBytes = GetLenBytes;
        byte zBytes = GetWidBytes;

        Vector3 paramMin = new Vector3(GetMinRot, GetMinLength, GetMinWidth);
        Vector3 paramMax = new Vector3(GetMaxRot, GetMaxLength, GetMaxWidth);
        SByte3 compressed = new SByte3()
        {
            min = paramMin,
            max = paramMax,
            xBytes = xBytes,
            yBytes = yBytes,
            zBytes = zBytes,
            byteVec = GetEmptyByte3()
        };

        compressed.SetFromVec3(new Vector3(rot, len, wid));

        simplifiedShapeData.coord.SetPosition(transform.position);
        simplifiedShapeData.param = compressed;

        return simplifiedShapeData;
    }

    [SerializeField]
    bool reset;

    [ContextMenu("CorrentShapeRotTest")]
    void CSRT()
    {

        if (reset)
        {
            polygonCollider2D.points = shapeAsOctagon;
            return;
        }

        SimplifiedShapeData simplifiedShapeData = GetSimplifiedShapeData();
        inside = simplifiedShapeData;

        Vector3 param = simplifiedShapeData.param.GetVec3();
        float rot, len, wid;
        rot = param.x;
        len = param.y;
        wid = param.z;

        Vector2[] correctedPoints = new Vector2[points.Length];
        for (int i = 0; i < correctedPoints.Length; i++)
        {
            
            float yToAdd = 0;
            float xToAdd = 0;

            if (i == 0 || i == 1 || i == 6 || i == 7) yToAdd = wid / 2f;
            if (i == 2 || i == 3 || i == 4 || i == 5) yToAdd = -wid / 2f;
            if (i == 4 || i == 5 || i == 6 || i == 7) xToAdd = len;

            Vector2 pointNoRotation = (Vector2)BuiltShapeBehaviour.GetOctagonalVerticesVec3[i] + new Vector2(xToAdd, yToAdd);
            float pointBaseRotation = Mathf.Atan2(pointNoRotation.y, pointNoRotation.x) * Mathf.Rad2Deg;

            float rotationAccum = pointBaseRotation + rot;
            Vector2 pointAsRotated = new Vector2(Mathf.Cos(rotationAccum), Mathf.Sin(rotationAccum)).normalized;

            //No pos offset since transform is used for collider
            correctedPoints[i] = rotate(pointNoRotation, rot * Mathf.Deg2Rad) + (Vector2)(simplifiedShapeData.coord.GetPosition() - transform.position);

        }

        polygonCollider2D.points = correctedPoints;

    }

    public Vector2[] GetMimicPoints()
    {
        Vector2[] duplicate = new Vector2[points.Length];
        for (int i = 0; i < duplicate.Length; i++) duplicate[i] = points[i];
        return duplicate;
    }

    [SerializeField] SimplifiedShapeData inside;

    [Serializable]
    public struct SimplifiedShapeData
    {
        [SerializeField]
        public ByteCoord coord;
        [SerializeField]
        public SByte3 param;

        public int GetSize() => param.xBytes + param.yBytes + param.zBytes + 2;

        public Mesh GenerateWorldspaceMesh()
        {
            Mesh mesh = new Mesh();
            Vector3 paramC = param.GetVec3();

            Vector3[] correctedPoints = new Vector3[8];
            for (int i = 0; i < correctedPoints.Length; i++)
            {

                float yToAdd = 0;
                float xToAdd = 0;

                if (i == 0 || i == 1 || i == 6 || i == 7) yToAdd = paramC.z / 2f;
                if (i == 2 || i == 3 || i == 4 || i == 5) yToAdd = -paramC.z / 2f;
                if (i == 4 || i == 5 || i == 6 || i == 7) xToAdd = paramC.y;

                Vector2 pointNoRotation = (Vector2)BuiltShapeBehaviour.GetOctagonalVerticesVec3[i] + new Vector2(xToAdd, yToAdd);
                float pointBaseRotation = Mathf.Atan2(pointNoRotation.y, pointNoRotation.x) * Mathf.Rad2Deg;

                float rotationAccum = pointBaseRotation + paramC.x;
                Vector2 pointAsRotated = new Vector2(Mathf.Cos(rotationAccum), Mathf.Sin(rotationAccum)).normalized;

                correctedPoints[i] = rotate(pointNoRotation, paramC.x * Mathf.Deg2Rad) + (Vector2)coord.GetPosition();
            }
            mesh.vertices = correctedPoints;
            mesh.triangles = BuiltShapeBehaviour.GetOctagonalIndices;
            return mesh;
        }
    }
    [Serializable]
    public struct USimplifiedShapeData : INetworkSerializable
    {
        [SerializeField]
        public ByteCoord coord;
        [SerializeField]
        public Byte3 param;
        public int GetSize() => (param.data != null ? param.data.Length : 0) + 2;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref coord);
            serializer.SerializeValue(ref param);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Mesh GenerateWorldspaceMesh() => ShapeMimicBehaviour.ConvertFromUSimplifiedShapeData(this).GenerateWorldspaceMesh();
    }

    [Serializable]
    public struct ByteCoord : INetworkSerializable
    {
        public byte[] data;

        [JsonIgnore]
        public byte x 
        {
            get
            {
                if(data == null) data = new byte[2];
                return data[0];
            }
            set
            {
                if(data == null) data = new byte[2];
                data[0] = value;
            }
        }

        [JsonIgnore]
        public byte y 
        {
            get
            {
                if (data == null) data = new byte[2];
                return data[1];
            }
            set
            {
                if (data == null) data = new byte[2];
                data[1] = value;
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 GetPosition() => new Vector3(x - 128, y - 128);

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetPosition(Vector3 position) => (x, y) = ((byte)Mathf.RoundToInt(position.x + 128f), (byte)Mathf.RoundToInt(position.y + 128f));
        public static int GetSize() => 2;
    }

    public static Vector2 rotate(Vector2 v, float delta)
    {
        return new Vector2(
            v.x * Mathf.Cos(delta) - v.y * Mathf.Sin(delta),
            v.x * Mathf.Sin(delta) + v.y * Mathf.Cos(delta)
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void LockInOffset() => offsetPosition = GetSnappedPosition(offsetPosition);
}
