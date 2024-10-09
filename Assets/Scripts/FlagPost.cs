using UnityEngine;
using UnityEngine.UIElements;

public sealed class FlagPost : MonoBehaviour, IObjective
{

    public ulong ownerId = 18446749073;

    public FlagBehaviour[] flags;
    MapSynchronizer mapSync;

    public float rotation;

    [SerializeField]
    Vector3 playerSpawnOffset;

    public bool localFlag = false;

    public Transform spawn;

    [SerializeField]
    bool transformSpawnPos;
    [SerializeField]
    Transform potential;

    public void DisableOnLoad()
    {

        gameObject.SetActive(false);

    }

    public void EnableWithOwner(PlayerSynchronizer.PlayerData player)
    {

        if (FindFirstObjectByType<ScoreManager>().gameMode != ScoreManager.Mode.CTF) return;

        foreach (FlagBehaviour flag in flags)
        {
            flag.ownerId = player.id;
            flag.color = player.square.playerColor;
            flag.darkColor = player.square.playerDarkerColor;

            flag.rb.simulated = true;
            flag.spriteRenderer.enabled = true;
            flag.flagParticleSystem.Play();
        }

        ownerId = player.id;

        if (player.square.isLocalPlayer)
        {
            if (transformSpawnPos)
            {
                spawn.position = potential.position;
            }
            else
            {

                spawn = GameObject.FindGameObjectWithTag("Spawn").transform;
                spawn.position = transform.position + playerSpawnOffset;

            }
            player.square.transform.position = GameObject.FindGameObjectWithTag("Spawn").transform.position;
            localFlag = true;
        
        }

        gameObject.SetActive(true);

    }

    private void Awake()
    {

        mapSync = GameObject.FindGameObjectWithTag("Sync").GetComponent<MapSynchronizer>();
        flags = GetComponentsInChildren<FlagBehaviour>();
    }

    private void Update()
    {

        rotation = mapSync.repeat5S * 360;
        transform.rotation = Quaternion.Euler(0, 0, rotation);

    }

}
