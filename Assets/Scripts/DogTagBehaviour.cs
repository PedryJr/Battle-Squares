using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static PlayerSynchronizer;

public sealed class DogTagBehaviour : MonoBehaviour
{

    [SerializeField]
    Transform dogTagParticleTransform;
    [SerializeField]
    ParticleSystemRenderer dogTagParticles;
    [SerializeField]
    public Rigidbody2D rb;
    [SerializeField]
    SpriteRenderer spriteRenderer;
    [SerializeField]
    ParticleBehaviour collectParticles;

    List<PlayerBehaviour> players = new List<PlayerBehaviour>();
    public PlayerBehaviour owningPlayer;

    PlayerSynchronizer playerSynchronizer;
    MapSynchronizer mapSynchronizer;

    Vector2 target = Vector2.zero;

    [SerializeField]
    float force;

    float updateTimer;

    public int dogTagId;

    public bool isCollected;

    public void Init(byte playerId, Vector2 startVelocity, int newDogTagId)
    {
        dogTagId = newDogTagId;
        playerSynchronizer = FindAnyObjectByType<PlayerSynchronizer>();
        mapSynchronizer = FindAnyObjectByType<MapSynchronizer>();
        foreach (PlayerData player in playerSynchronizer.playerIdentities)
        {
            if ( (byte) player.id == playerId) owningPlayer = player.square;
            players.Add(player.square);
        }

        spriteRenderer.color = owningPlayer.playerColor;

        Material particleMaterial = dogTagParticles.material;
        dogTagParticles.material = particleMaterial;
        dogTagParticles.material.color = owningPlayer.playerDarkerColor;

        rb.linearVelocity = startVelocity;

    }

    private void Update()
    {

        if (playerSynchronizer.localSquare.id == owningPlayer.id) UpdateSync();

        target = Vector2.zero;

        foreach (PlayerBehaviour player in players)
        {
            if (!player) continue;
            Vector2 toTarget = player.rb.position - rb.position;
            if (toTarget.magnitude < 6f) target += toTarget;

            if(playerSynchronizer.localSquare.id == owningPlayer.id)
            {
                if (toTarget.magnitude < 0.6f && !isCollected)
                {
                    mapSynchronizer.CollectDogTag(dogTagId, (byte) player.id);
                    isCollected = true;
                }
            }

        }

    }

    private void LateUpdate()
    {

        float rotationToTarget = Mathf.Rad2Deg * Mathf.Atan2((rb.linearVelocity.normalized).y, (rb.linearVelocity.normalized).x);
        dogTagParticleTransform.rotation = Quaternion.Euler(0, 0, rotationToTarget);

    }

    void UpdateSync()
    {
        updateTimer += Time.deltaTime * 15f;

        if (updateTimer > 1)
        {
            updateTimer = 0;

            mapSynchronizer.SyncDogTags(dogTagId, rb);
        }
    }

    private void FixedUpdate()
    {

        ApplyForceToTarget();

    }

    void ApplyForceToTarget()
    {

        rb.AddForce(target * force, ForceMode2D.Force);

        Vector2 clampedPosition = rb.position;
        clampedPosition.x = Mathf.Clamp(clampedPosition.x, -64, 64);
        clampedPosition.y = Mathf.Clamp(clampedPosition.y, -64, 64);

        Vector2 clampedVelocity = rb.linearVelocity;
        clampedVelocity.x = Mathf.Clamp(clampedVelocity.x, -40, 40);
        clampedVelocity.y = Mathf.Clamp(clampedVelocity.y, -40, 40);

        rb.position = clampedPosition;
        rb.linearVelocity = clampedVelocity;
        rb.angularVelocity = Mathf.Clamp(rb.angularVelocity, -1000, 1000);
        rb.rotation = Mathf.Repeat(rb.rotation, 360);

    }

    public void RunCollected(byte playerId)
    {

        PlayerBehaviour player = null;
        player = playerSynchronizer.GetPlayerById(playerId);
        if (player)
        {
            Color particleColor = player.playerDarkerColor;
            Vector3 position = player.rb.position;
            position.z = transform.position.z;

            ParticleBehaviour newParticle = Instantiate(collectParticles, position, transform.rotation, null);

            foreach (ParticleSystemRenderer particle in newParticle.GetComponentsInChildren<ParticleSystemRenderer>())
            {
                Material particleMaterial = Instantiate(particle.material);
                particle.material = particleMaterial;
                particle.material.color = particleColor;
            }
        }

        Destroy(gameObject);

    }

}
