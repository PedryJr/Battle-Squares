using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public sealed unsafe class EditorTabMenu : MonoBehaviour
{
    DragAndScrollMod dragAndScrollMod;

    [SerializeField]
    float animationSpeed;

    EditorFadeCell[] editorFadeCells;

    NativeArray<float> fadeValues;
    NativeArray<float3> anchoredOnPositions;
    NativeArray<float3> anchoredOffPositions;
    NativeArray<float3> anchoredOnScales;
    NativeArray<float3> anchoredOffScales;
    NativeArray<float3> anchoredPositions;
    NativeArray<float3> anchoredScales;
    NativeArray<bool> onOff;
    NativeArray<float> deltaTime;
    NativeArray<float> animationSpeedNative;

    private void Awake()
    {
        dragAndScrollMod = FindAnyObjectByType<DragAndScrollMod>();
        editorFadeCells = GetComponentsInChildren<EditorFadeCell>();

        fadeValues = new NativeArray<float>(editorFadeCells.Length, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        anchoredOnPositions = new NativeArray<float3>(editorFadeCells.Length, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        anchoredOffPositions = new NativeArray<float3>(editorFadeCells.Length, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        anchoredOnScales = new NativeArray<float3>(editorFadeCells.Length, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        anchoredOffScales = new NativeArray<float3>(editorFadeCells.Length, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        anchoredPositions = new NativeArray<float3>(editorFadeCells.Length, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        anchoredScales = new NativeArray<float3>(editorFadeCells.Length, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        onOff = new NativeArray<bool>(1, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        deltaTime = new NativeArray<float>(1, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        animationSpeedNative = new NativeArray<float>(1, Allocator.Persistent, NativeArrayOptions.ClearMemory);

    }

    private void Start()
    {

        for (int i = 0; i < editorFadeCells.Length; i++)
        {
            anchoredOnPositions[i] = editorFadeCells[i].GetOnPosition();
            anchoredOnScales[i] = editorFadeCells[i].GetOnSize();

            float distanceToTop = Vector3.Distance(new Vector3(0, anchoredOnPositions[i].y, 0), new Vector3(0, -500f, 0));

            anchoredOffPositions[i] = math.lerp(new float3(0, 0, 0), anchoredOnPositions[i], 0.5f);
            anchoredOffScales[i] = new float3(0, 0, 0);
            anchoredPositions[i] = anchoredOffPositions[i];
            anchoredScales[i] = anchoredOffScales[i];

        }

    }

    private void OnDestroy()
    {
        fadeValues.Dispose();
        anchoredOffPositions.Dispose();
        anchoredOnPositions.Dispose();
    }


    private void Update()
    {

        animationSpeedNative[0] = animationSpeed;
        deltaTime[0] = Time.deltaTime;
        onOff[0] = dragAndScrollMod.GetTabFlag();

        CalculateFadersJob calculateFadersJob = new CalculateFadersJob()
        {
            animationSpeedNative = animationSpeedNative,
            deltaTime = deltaTime,
            onOff = onOff,

            fadeValues = fadeValues,
            anchoredOnPositions = anchoredOnPositions,
            anchoredOffPositions = anchoredOffPositions,
            anchoredPositions = anchoredPositions,
            anchoredOffScales = anchoredOffScales,
            anchoredOnScales = anchoredOnScales,
            anchoredScales = anchoredScales,
        };

        calculateFadersJob.Schedule(editorFadeCells.Length, 8).Complete();

        for (int i = 0; i < editorFadeCells.Length; i++)
        {
            editorFadeCells[i].SetFade(fadeValues[i]);
            editorFadeCells[i].SetPosition(anchoredPositions[i]);
            editorFadeCells[i].SetSize(anchoredScales[i]);
        }

    }


    public struct CalculateFadersJob : IJobParallelFor
    {
        internal NativeArray<float> fadeValues;
        internal NativeArray<float3> anchoredOnPositions;
        internal NativeArray<float3> anchoredOffPositions;
        internal NativeArray<float3> anchoredPositions;
        internal NativeArray<float3> anchoredScales;

        [NativeDisableParallelForRestriction]
        internal NativeArray<bool> onOff;
        [NativeDisableParallelForRestriction]
        internal NativeArray<float> deltaTime;
        [NativeDisableParallelForRestriction]
        internal NativeArray<float> animationSpeedNative;

        internal NativeArray<float3> anchoredOnScales;
        internal NativeArray<float3> anchoredOffScales;

        public void Execute(int index)
        {

            float3 targetScale = onOff[0] ? anchoredOnScales[index] : anchoredOffScales[index];
            float3 targetPosition = onOff[0] ? anchoredOnPositions[index] : anchoredOffPositions[index];
            float targetFade = onOff[0] ? 0.9137255f : 0f;

            fadeValues[index] = math.lerp(fadeValues[index], targetFade, deltaTime[0] * animationSpeedNative[0]);
            anchoredPositions[index] = math.lerp(anchoredPositions[index], targetPosition, deltaTime[0] * animationSpeedNative[0]);
            anchoredScales[index] = math.lerp(anchoredScales[index], targetScale, deltaTime[0] * animationSpeedNative[0]);

        }

    }


}
