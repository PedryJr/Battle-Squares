using TMPro;
using Unity.Mathematics;
using UnityEngine;

public sealed class ChatBubbleBehaviour : MonoBehaviour
{
    private float scaleYRef = 12.288f;
    private float scaleXRef = 1.6666f;
    public float timer = 0;
    private float screenSizeY;
    private float scaleX;
    private float scaleY;
    private float lerp;
    public AnimationState state = AnimationState.Expanding;

    private delegate void Func();

    private Func updateFunc = () => { };
    private Vector2 currentSize;
    private Vector2 targetSize;
    private Vector2 originPos;
    private Vector2 targetPos;
    private Vector2 currentPos;

    [SerializeField]
    private float timeToExpand;

    [SerializeField]
    private float timeToExist;

    [SerializeField]
    private float timeToShrink;

    [SerializeField]
    private float scale;

    private RectTransform rectTransform;
    private TMP_Text messageField;
    private PlayerBehaviour playerBehaviour;
    private Material material;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        messageField = GetComponentInChildren<TMP_Text>();
        material = Instantiate(messageField.fontSharedMaterial);
        messageField.fontSharedMaterial = material;

        currentSize = new Vector2();

        UpdateScreenMeasurements();
    }

    private void Update()
    {
        UpdateScreenMeasurements();

        if (playerBehaviour) UpdatePosition();

        timer += Time.deltaTime;

        updateFunc =
            state == AnimationState.Expanding ? UpdateExpand :
            state == AnimationState.Existing ? UpdateStay :
            state == AnimationState.Shrinking ? UpdateShrink :
            () => { };

        rectTransform.sizeDelta = currentSize * scale;
        rectTransform.anchoredPosition = currentPos;

        updateFunc();
    }

    private void UpdateExpand()
    {
        lerp = Mathf.SmoothStep(0, 1, timer / timeToExpand);
        currentSize = Vector2.Lerp(Vector2.zero, targetSize, lerp);

        if (timer > timeToExpand)
        {
            timer = 0;
            state = AnimationState.Existing;
        }

        currentPos = Vector2.Lerp(originPos, targetPos, math.smoothstep(0, 1, lerp));
    }

    private void UpdateStay()
    {
        if (timer > timeToExist)
        {
            timer = 0;
            state = AnimationState.Shrinking;
        }

        currentPos = Vector2.Lerp(currentPos, targetPos, Time.deltaTime * 30);
    }

    private void UpdateShrink()
    {
        lerp = Mathf.SmoothStep(0, 1, timer / timeToShrink);
        currentSize = Vector2.Lerp(targetSize, Vector2.zero, lerp);

        if (timer > timeToShrink)
        {
            Destroy(gameObject);
        }

        currentPos = Vector2.Lerp(targetPos, originPos, Mathf.SmoothStep(0, 1, lerp));
    }

    private void UpdateScreenMeasurements()
    {
        screenSizeY = Screen.height;
        scaleY = screenSizeY / scaleYRef;
        scaleX = scaleY * scaleXRef;
        targetSize = new Vector2(scaleX, scaleY);
    }

    private void UpdatePosition()
    {
        originPos = Camera.main.WorldToScreenPoint(playerBehaviour.transform.position);
        targetPos = Camera.main.WorldToScreenPoint(playerBehaviour.transform.position + new Vector3(0, 2.45f, 0));

        originPos.y = Mathf.Clamp(originPos.y, targetSize.y, Screen.height - (targetSize.y));
        originPos.x = Mathf.Clamp(originPos.x, targetSize.x, Screen.width - (targetSize.x));

        targetPos.y = Mathf.Clamp(targetPos.y, targetSize.y, Screen.height - (targetSize.y));
        targetPos.x = Mathf.Clamp(targetPos.x, targetSize.x, Screen.width - (targetSize.x));
    }

    public void ApplyMessage(string message, PlayerBehaviour attatchedPlayer)
    {
        Color messageColor = attatchedPlayer.PlayerColor.ChatBoxColor;
        messageColor.a = 1;

        messageField.color = messageColor;

        playerBehaviour = attatchedPlayer;
        messageField.text = message;

        float addedTime = message.Length / 5;
        timeToExist += addedTime;

        if (playerBehaviour.chatBubbleBehaviour)
        {
            playerBehaviour.chatBubbleBehaviour.state = AnimationState.Shrinking;
            playerBehaviour.chatBubbleBehaviour.timer = 0;
        }

        playerBehaviour.chatBubbleBehaviour = this;
    }

    private void OnDestroy()
    {
        if (playerBehaviour.chatBubbleBehaviour)
        {
            if (playerBehaviour.chatBubbleBehaviour == this)
            {
                playerBehaviour.chatBubbleBehaviour = null;
            }
        }
    }

    public enum AnimationState
    {
        Expanding,
        Existing,
        Shrinking
    }
}