using UnityEngine;

public class Test : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [SerializeField]
    Transform boom;

    TestInput input;


    float strenthFunnyT = 0;
    float strenthGoofyT = 0;

    float strenthFunny = 0;
    float strenthGoofy = 0;

    void Awake()
    {

        input = new TestInput();
        input.MousePos.Call.performed += Call_performed;
        input.MousePos.Call.canceled += Call_performed;
        input.MousePos.InteractBig.performed += (e) => { strenthFunny = 5; Instantiate(boom, transform.position, transform.rotation, null); };
        input.MousePos.InteractBig.canceled += (e) => { strenthFunny = 0; };
        input.MousePos.InteractSmall.performed += (e) => { strenthGoofy = -5; };
        input.MousePos.InteractSmall.canceled += (e) => { strenthGoofy = 0; };
        input.Enable();

    }

    private void OnDestroy()
    {
        input.Dispose();
    }

    private void Update()
    {
        strenthFunnyT = Mathf.Lerp(strenthFunnyT, strenthFunny, Time.deltaTime * 20);
        strenthGoofyT = Mathf.Lerp(strenthGoofyT, strenthGoofy, Time.deltaTime * 20);
    }

    Vector3 lastPosition = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
    double accumulatedDelta = 0f;

    private void Call_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        if (!this) return;
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(obj.ReadValue<Vector2>());
        mousePos.z = transform.position.z;
        transform.position = mousePos;

        if (lastPosition == new Vector3(float.MaxValue, float.MaxValue, float.MaxValue))
        {
            lastPosition = transform.position;
        }
        else
        {

            accumulatedDelta += Vector3.Distance(transform.position, lastPosition) * 30;
            lastPosition = transform.position;
            transform.rotation = Quaternion.Euler(0, 0, (float) accumulatedDelta);

        }

    }

}
