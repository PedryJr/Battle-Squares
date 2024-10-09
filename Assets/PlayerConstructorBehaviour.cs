using UnityEngine;

public class PlayerConstructorBehaviour : MonoBehaviour
{


    [SerializeField]
    Transform door;

    [SerializeField]
    Transform shutPosition;

    [SerializeField]
    Transform openPosition;

    Vector3 fromPos;
    Vector3 toPos;

    float initDelay;
    float timer;

    DoorState state = DoorState.Closing;
    public bool open;

    const float InitDelay = 0.3f;

    private void Awake()
    {

        EndConstruction();

    }

    public void StartNewConstruction()
    {
        state = DoorState.Opening;
        toPos = openPosition.position;
        ResetFromPos();
    }

    public void EndConstruction()
    {
        state = DoorState.Closing;
        toPos = shutPosition.position;
        ResetFromPos();
    }

    void ResetFromPos()
    {
        timer = 0;
        fromPos = door.transform.position;
        initDelay = 0;
    }

    private void Update()
    {

        if(state == DoorState.Closing) initDelay = InitDelay;
        if (state == DoorState.Opening) initDelay += Time.deltaTime;

        if (initDelay > InitDelay) initDelay = InitDelay;
        if (initDelay != InitDelay) return;

        if (timer < 1) timer += Time.deltaTime * 2.5f;
        if (timer > 1) timer = 1;

        if (state == DoorState.Opening) door.transform.position = Vector3.Lerp(fromPos, toPos, MyExtentions.EaseOutQuad(timer));
        if (state == DoorState.Closing) door.transform.position = Vector3.Lerp(fromPos, toPos, MyExtentions.EaseInQuad(timer));

        if (timer == 1 && state == DoorState.Opening) open = true;
        if (timer == 1 && state == DoorState.Closing) open = false;

    }


    enum DoorState
    {
        Opening, Closing
    }

}
