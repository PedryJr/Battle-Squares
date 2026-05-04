using System;
using UnityEngine;

public class PointZoneBehaviour : MonoBehaviour
{

    [SerializeField]
    float zoneRadius;



    void Start()
    {
    }

    void Update()
    {
        
    }

    private void OnDrawGizmos()
    {
        MyExtentions.GizmoDrawCircle(transform.position, zoneRadius, Color.green);
    }

    [Serializable]
    public struct ZoneAnimationParams
    {
        [SerializeField]
        Transform[] perimiterPoints;

        public void Animate(float time, Vector2 center)
        {
            foreach (var item in perimiterPoints)
            {
                
            }
        }
    }

}
