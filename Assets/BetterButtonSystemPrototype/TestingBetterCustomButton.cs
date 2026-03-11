using UnityEngine;
using UnityEngine.EventSystems;

public class TestingBetterCustomButton : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        if(eventData.button == PointerEventData.InputButton.Left) Debug.Log("Left");
        if(eventData.button == PointerEventData.InputButton.Right) Debug.Log("Right");
        if(eventData.button == PointerEventData.InputButton.Middle) Debug.Log("Middle");
    }

    void Start()
    {
        
    }

    void Update()
    {
        
    }
}
