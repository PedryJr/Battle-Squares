using TMPro;
using UnityEngine;

public class ChatToggleBehaviour : MonoBehaviour
{

    [SerializeField]
    TextMeshProUGUI placeHolderField;

    [SerializeField]
    TextMeshProUGUI contentField;

    [SerializeField]
    TMP_InputField inputField;

    bool flipFlop;
    float animationTimer;

    private void OnEnable()
    {

        placeHolderField.text = "";
        inputField.text = "";

    }

    private void Update()
    {
        
        if(inputField.text == "")
        {
            AnimateEmptyText();
        }
        else
        {
            placeHolderField.text = "";
        }

        contentField.text = inputField.text;
        animationTimer += Time.deltaTime;

    }

    void AnimateEmptyText()
    {

        if (animationTimer > 0.37f)
        {
            flipFlop = !flipFlop;

            if (flipFlop)
            {
                placeHolderField.text = "|";
            }
            else
            {
                placeHolderField.text = "";
            }
            animationTimer = 0;
        }

    }

}
