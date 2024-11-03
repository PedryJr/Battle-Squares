using UnityEngine;

public class SceneInitBehaviour : MonoBehaviour
{

    [SerializeField]
    GameObject sceneRoot;

    private void Awake()
    {
       
        GameObject instantiatedRoot = Instantiate(sceneRoot);
        sceneRoot = instantiatedRoot;

    }

}
