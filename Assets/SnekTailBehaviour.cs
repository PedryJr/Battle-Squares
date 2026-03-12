using System;
using System.Collections.Generic;
using UnityEngine;

public class SnekTailBehaviour : MonoBehaviour
{
    [SerializeField] SnekSegmentBehaviour segmentPrefab;
    [SerializeField] public PlayerBehaviour owner;
    List<SnekSegmentBehaviour> snekSegments;

    public Transform nextTarget 
    {
        get
        {
            Transform target = owner.transform;
            bool targetIsNextSegment = snekSegments.Count != 0;
            int nextSegmentIndex = snekSegments.Count - 1;
            if (targetIsNextSegment) target = snekSegments[snekSegments.Count - 1].transform;
            return target;
        } 
    }

    private void Awake()
    {
        snekSegments = new List<SnekSegmentBehaviour>();
    }

    public void SpawnSegment(Vector2 segmentSpawnPosition)
    {

        FixLinks();

        SnekSegmentBehaviour newSegment;
        newSegment = AutoPooledPool<SnekSegmentBehaviour>.Spawn(segmentPrefab, segmentSpawnPosition, Quaternion.identity);
        newSegment.Initialize(this);

        snekSegments.Add(newSegment);
        newSegment.SetState(SnekSegmentBehaviour.SnekSegmentState.Spawned);
    }

    public void RemoveASegment()
    {

    }

    void Start()
    {
        
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePosition = Input.mousePosition;
            Vector2 mousePosInWS = Camera.main.ScreenToWorldPoint(mousePosition, Camera.MonoOrStereoscopicEye.Left);
            SpawnSegment(mousePosInWS);
        }
    }

    internal void FixLinks()
    {
        for (int i = snekSegments.Count - 1; i >= 0; i--)
        {
            if (!snekSegments[i]) snekSegments.RemoveAt(i);
        }

        for (int i = 0; i < snekSegments.Count; i++)
        {
            if(i == 0) snekSegments[i].RelinkSegment(owner.transform);
            else snekSegments[i].RelinkSegment(snekSegments[i - 1].transform);
        }
    }
}
