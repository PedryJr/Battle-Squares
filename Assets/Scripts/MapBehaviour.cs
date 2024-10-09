using UnityEngine;
using UnityEngine.Rendering.Universal;

public sealed class MapBehaviour : MonoBehaviour
{

    [SerializeField]
    public Sprite icon;

    [SerializeField]
    Transform lightPos;

    [SerializeField]
    public string arenaName;

    public string size;
    public string difficulty;

    private void Awake()
    {

        FindAnyObjectByType<Light2D>().transform.position = lightPos.position;

    }

}
