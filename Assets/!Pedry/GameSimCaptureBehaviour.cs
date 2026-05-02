#if UNITY_EDITOR
using System.IO;
using UnityEngine;

public class GameSimCaptureBehaviour : MonoBehaviour
{
    [SerializeField][Range(0f, 1f)] public float simSpeed;
    [SerializeField] public string captureDestination = "Screenshots/";
    public void AddToSimSpeed(float toAdd) => simSpeed = Mathf.Clamp01(Mathf.Round(((simSpeed + toAdd) * 100)) / 100);
    public void DecrementSimSpeed() => AddToSimSpeed(-0.1f);
    public void IncrementSimSpeed() => AddToSimSpeed(0.1f);
    public void CaptureScreen()
    {
        string path = Path.Combine(Application.dataPath, captureDestination);
        if(!Directory.Exists(path)) Directory.CreateDirectory(path);
        path = Path.Combine(path,  $"{System.DateTime.Now.Hour}_{System.DateTime.Now.Minute}_{System.DateTime.Now.Second}.png");
        ScreenCapture.CaptureScreenshot(path);
    }

    void Start()
    {
        
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {

        }
        Time.timeScale = simSpeed;
    }
}
#endif