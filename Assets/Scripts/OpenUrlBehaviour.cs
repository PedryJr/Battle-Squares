using UnityEngine;

public sealed class OpenUrlBehaviour : MonoBehaviour
{

    [SerializeField]
    string url;

    public void PERFORMURL()
    {

        Application.OpenURL(url);

    }

}
