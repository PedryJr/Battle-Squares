using UnityEngine;
using UnityEngine.UI;

public sealed class LobbyPlayerDisplayBehaviour : MonoBehaviour
{

    public PlayerBehaviour assignedPlayer = null;

    [SerializeField]
    Image border;

    [SerializeField]
    Image pfp;

    float timer;
    float readyLerp;

    [SerializeField]
    Color unreadyColor = Color.gray;
    [SerializeField]
    Color readyColor = Color.white;
    [SerializeField]
    PreviewMode previewMode = PreviewMode.None;


    bool init = false;

    private void Awake()
    {

        border.color = new Color(0, 0, 0, 0);
        pfp.color = new Color(0, 0, 0 , 0);

    }

    public void Init(PlayerBehaviour assignedPlayer)
    {

        init = true;
        this.assignedPlayer = assignedPlayer;

    }

    private void Update()
    {

        if (!init) return;

        if(previewMode == PreviewMode.Ready)
        {
            pfp.color = readyColor;
            border.color = assignedPlayer.PlayerColor.PfpBorderIsReadyColor;
            return;
        }
        else if (previewMode == PreviewMode.NotReady)
        {
            pfp.color = unreadyColor;
            border.color = assignedPlayer.PlayerColor.PfpBorderNotReadyColor;
            return;
        }

        if (assignedPlayer.ready) timer += Time.deltaTime * 2;
        else timer -= Time.deltaTime * 2;
        timer = Mathf.Clamp01(timer);

        readyLerp = MyExtentions.EaseOutQuad(timer);

        pfp.color = Color.Lerp(unreadyColor, readyColor, readyLerp);
        border.color = Color.Lerp(assignedPlayer.PlayerColor.PfpBorderNotReadyColor, assignedPlayer.PlayerColor.PfpBorderIsReadyColor, readyLerp);

        if (assignedPlayer.pfp) pfp.sprite = assignedPlayer.pfp;

    }

    enum PreviewMode
    {
        None = 0,
        Ready = 1,
        NotReady = 2,
    }

}
