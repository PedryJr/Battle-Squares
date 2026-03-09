using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static BinaryVectors; 

public sealed class ShapeMimicBehaviour : MonoBehaviour
{
    public static Mesh sharedMesh = null;
    public static Dictionary<int, ShapeMimicBehaviour> ShapeMimics = new Dictionary<int, ShapeMimicBehaviour>(2048);
    public static int ShapeIDCounter = 0;

    private int shapeID;
    float snappingOnGenerate = 1f;

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

    public void RegisterRelease(int[] triangles, DragAndScrollMod dragmod, float snappingOnGenerate)
    {
        this.snappingOnGenerate = snappingOnGenerate;
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
    Vector2 GetSnappedPosition(Vector2 rawPosition) => new Vector2(Mathf.Round(rawPosition.x / snappingOnGenerate) * snappingOnGenerate, Mathf.Round(rawPosition.y / snappingOnGenerate) * snappingOnGenerate);

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
    public const float GetMinLength = -360.62445f;
    public const float GetMaxLength = 360.62445f;
    public const float GetMinWidth = -32f;
    public const float GetMaxWidth = 32f;
    public const float GetMinSnapping = 0f;
    public const float GetMaxSnapping = 1f;
    public const byte GetRotBytes = 2;
    public const byte GetLenBytes = 2;
    public const byte GetWidBytes = 2;
    public const byte GetSnaBytes = 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SByte4 GetShapeCompressor() => GetShapeCompressor(GetEmptyByte4());
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SByte4 GetShapeCompressor(Byte4 predefinedData) => new SByte4()
    {
        min = new Vector4(GetMinRot, GetMinLength, GetMinWidth, GetMinSnapping),
        max = new Vector4(GetMaxRot, GetMaxLength, GetMaxWidth, GetMaxSnapping),
        xBytes = GetRotBytes,
        yBytes = GetLenBytes,
        zBytes = GetWidBytes,
        wBytes = GetSnaBytes,
        byteVec = predefinedData
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Byte3 GetEmptyByte3() => new Byte3();
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Byte4 GetEmptyByte4() => new Byte4() { data = new byte[GetRotBytes + GetLenBytes + GetWidBytes + GetSnaBytes] };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SimplifiedShapeData ConvertFromUSimplifiedShapeData(USimplifiedShapeData uSimplifiedShapeData)
    {

        return new SimplifiedShapeData
        {
            coord = uSimplifiedShapeData.coord,
            param = GetShapeCompressor(uSimplifiedShapeData.param)
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

        //THOUGHTS
        //Points are malformed from the snapping grid, we need to restore them to the octagonal shape first
        //Then store the snapping as a separate parameter for rebuilding the shape later
        //Perhaps the shapeAsOctagon can be resized to the snapped size, then the difference between the two shapes can be stored as the length and width parameters
        //That difference can then be reapplied to the octagonal shape when reconstructing it later after applying space size to the octagonal shape

        shapeH = points[7] - points[0];
        shapeV = points[2] - points[1] ;
/*        ignoreH = (shapeAsOctagon[7] - shapeAsOctagon[0]);
        ignoreV = (shapeAsOctagon[2] - shapeAsOctagon[1]);*/

        ignoreH = ((shapeAsOctagon[7] * snappingOnGenerate) - (shapeAsOctagon[0] * snappingOnGenerate));
        ignoreV = ((shapeAsOctagon[2] * snappingOnGenerate) - (shapeAsOctagon[1] * snappingOnGenerate));

        float rot, len, wid, sna;
        rot = Mathf.Atan2(shapeH.y, shapeH.x) * Mathf.Rad2Deg;
        len = shapeH.magnitude - ignoreH.magnitude;
        wid = shapeV.magnitude - ignoreV.magnitude;
        sna = snappingOnGenerate;


        //SByte4 compressed = GetShapeCompressor();

        //compressed.SetFromVec4(new Vector4(rot, len, wid, sna));

        simplifiedShapeData.coord.SetPosition(transform.position);

        simplifiedShapeData.param = GetShapeCompressor();
        simplifiedShapeData.param.SetFromVec4(new Vector4(rot, len, wid, sna));
        Debug.Log("SNAPPING RAW: " + sna);
        Debug.Log("SNAPPING STORED: " + simplifiedShapeData.param.GetVec4().w);
        return simplifiedShapeData;
    }

    [SerializeField]
    bool reset;



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
        public SByte4 param;

        public int GetSize() => param.xBytes + param.yBytes + param.zBytes + 2;

        public Mesh GenerateWorldspaceMesh()
        {
            Mesh mesh = new Mesh();
            Vector4 paramC = param.GetVec4();

            Vector3[] correctedPoints = new Vector3[8];
            for (int i = 0; i < correctedPoints.Length; i++)
            {

                float yToAdd = 0;
                float xToAdd = 0;

                if (i == 0 || i == 1 || i == 6 || i == 7) yToAdd = paramC.z / 2f;
                if (i == 2 || i == 3 || i == 4 || i == 5) yToAdd = -paramC.z / 2f;
                if (i == 4 || i == 5 || i == 6 || i == 7) xToAdd = paramC.y;

                Vector2 pointNoRotation = ((Vector2)BuiltShapeBehaviour.GetOctagonalVerticesVec3[i] * paramC.w) + new Vector2(xToAdd, yToAdd);
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
        public Byte4 param;
        public int GetSize() => (param.data != null ? param.data.Length : 0) + 2;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref coord);
            serializer.SerializeValue(ref param);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Mesh GenerateWorldspaceMesh() => ShapeMimicBehaviour.ConvertFromUSimplifiedShapeData(this).GenerateWorldspaceMesh();
    }

    const byte testB = 2;
    const float testMin = 0;
    const float testMax = 255;

    [Serializable]
    public struct ByteCoord : INetworkSerializable
    {
        public byte[] data;

        [JsonIgnore]
        public float x 
        {
            get
            {
                if (data == null) data = new byte[testB * 2];

                SByte2 temp = new SByte2();
                temp.SetXBytes(testB);
                temp.SetYBytes(testB);
                temp.SetMin(testMin);
                temp.SetMax(testMax);
                temp.byteVec = new Byte2() { data = data };

                return temp.GetVec2().x;
            }
            set
            {
                if (data == null) data = new byte[testB * 2];

                SByte2 temp = new SByte2();
                temp.SetXBytes(testB);
                temp.SetYBytes(testB);
                temp.SetMin(testMin);
                temp.SetMax(testMax);
                temp.byteVec = new Byte2() { data = data };
                temp.SetFromfloat2(new Vector2(value, temp.GetVec2().y));

                //data = temp.byteVec.data;
                //data[0] = (byte) value;
            }
        }

        [JsonIgnore]
        public float y 
        {
            get
            {
                if (data == null) data = new byte[testB * 2];
                SByte2 temp = new SByte2();
                temp.SetXBytes(testB);
                temp.SetYBytes(testB);
                temp.SetMin(testMin);
                temp.SetMax(testMax);
                temp.byteVec = new Byte2() { data = data };

                return temp.GetVec2().y;
            }
            set
            {
                if (data == null) data = new byte[testB * 2];

                SByte2 temp = new SByte2();
                temp.SetXBytes(testB);
                temp.SetYBytes(testB);
                temp.SetMin(testMin);
                temp.SetMax(testMax);
                temp.byteVec = new Byte2() { data = data };
                temp.SetFromfloat2(new Vector2(temp.GetVec2().x, value));

                //data = temp.byteVec.data;

            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 GetPosition() => new Vector3(x - 128, y - 128);

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetPosition(Vector3 position) => (x, y) = (position.x + 128f, position.y + 128f);
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
