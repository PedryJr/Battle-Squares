using UnityEngine;
using UnityEngine.UI;

public class LobbyPlayerDisplayBehaviour : MonoBehaviour
{

    public PlayerBehaviour assignedPlayer = null;

    [SerializeField]
    Image border;

    [SerializeField]
    Image pfp;

    float timer;
    float readyLerp;

    Color unreadyColor = Color.gray;
    Color readyColor = Color.white;

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

        if (assignedPlayer.ready)
        {
            if(timer < 1) timer += Time.deltaTime * 2;
            if (timer > 1) timer = 1;
        }
        else
        {
            if (timer > 0) timer -= Time.deltaTime * 2;
            if (timer < 0) timer = 0;
        }

        readyLerp = MyExtentions.EaseOutQuad(timer);

        pfp.color = Color.Lerp(unreadyColor * 0.9f, readyColor * 0.9f, readyLerp);
        border.color = Color.Lerp(assignedPlayer.playerDarkerColor * 0.9f, assignedPlayer.playerColor * 0.9f, readyLerp);

        if (assignedPlayer.pfp) pfp.sprite = assignedPlayer.pfp;

    }

}
