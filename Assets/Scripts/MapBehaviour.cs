using UnityEngine;
using UnityEngine.Rendering.Universal;

public class MapBehaviour : MonoBehaviour
{

    [SerializeField]
    public Sprite icon;

    [SerializeField]
    Transform lightPos;

    private void Awake()
    {

        FindAnyObjectByType<Light2D>().transform.position = lightPos.position;

    }

}
