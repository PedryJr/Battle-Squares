using TMPro;
using UnityEngine;

public class TEMPTESTBROWSE : MonoBehaviour
{

    [SerializeField] ContentScanner scanner;

    public void TEST(string path)
    {
        scanner.rootDirectory = path;
        scanner.UpdateContent();
    }

}
