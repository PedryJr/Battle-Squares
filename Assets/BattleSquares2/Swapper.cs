using UnityEngine;
using UnityEngine.SceneManagement;

public class Swapper : MonoBehaviour
{

    EditorExit input;

    [SerializeField]
    int sceneIndex = 0;

    private void Awake()
    {
        input = new EditorExit();
        input.Enable();

        // Subscribe to the action
        input.MousePos.SceneSwapTest.performed += SceneSwapTest_performed;
    }

    private void SceneSwapTest_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        input.Dispose();
        SceneManager.LoadSceneAsync(sceneIndex, LoadSceneMode.Single);
        Resources.UnloadUnusedAssets();
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
