using TMPro;
using UnityEngine;

public sealed class ChatToggleBehaviour : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI placeHolderField;

    [SerializeField]
    private TextMeshProUGUI contentField;

    [SerializeField]
    private TMP_InputField inputField;

    private bool flipFlop;
    private float animationTimer;

    private void OnEnable()
    {
        placeHolderField.text = "";
        inputField.text = "";
    }

    private void Update()
    {
        if (inputField.text == "")
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

    private void AnimateEmptyText()
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