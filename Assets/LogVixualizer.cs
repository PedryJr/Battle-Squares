using UnityEngine;

public class LogVixualizer : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    float timer = 0;
    int incr = 0;
    // Update is called once per frame
    void Update()
    {
        
        timer += Time.deltaTime;

        if(timer > 2)
        {
            incr++;
            timer = 0;
            VLog.Log($"&eTesting log: &a[&r{incr}&a]" ,6);
        }

    }
}
