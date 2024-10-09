using System.Collections.Generic;
using Unity.Burst;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

[BurstCompile]
public class HitMarkBehaviour : MonoBehaviour
{

    public float timer;
    float timeAlive = 15;
    public byte ownerId;
    int skipPhysicsSteps;
    bool spawned;
    float fadeOut = 0;

    [SerializeField]
    bool canExpand;

    [SerializeField]
    bool randomSpawning;

    [SerializeField]
    bool randomRotation;

    [SerializeField]
    float spawnChance;

    [SerializeField]
    bool grow;

    [SerializeField]
    public SpawnStageBehaviour[] spawnStages;

    float spawnTimerOne;
    bool spawn1;

    float spawnTimerTwo;
    bool spawn2;

    float spawnTimerThree;
    bool spawn3;

    public Color spawnColor;
    public Color fadeColor;

    [BurstCompile]
    private void Awake()
    {

        if (randomRotation)
        {

            foreach (SpawnStageBehaviour stage in spawnStages)
            {

                stage.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));

            }

        }

    }

    [BurstCompile]
    private void Update()
    {

        timer += Time.deltaTime;
        if (timer > timeAlive) fadeOut = (timer - timeAlive) * 4;

        SpawnStages();

        if(fadeOut > 0)
        {

            foreach (SpawnStageBehaviour spawnStageBehaviour in spawnStages)
            {

                foreach (SpriteRenderer spriteRenderer in spawnStageBehaviour.sprites)
                {
                    if (!spriteRenderer.enabled) continue;
                    spriteRenderer.color = Color.Lerp(spawnColor, fadeColor, fadeOut);
                }

            }

        }

        if (fadeOut > 1)
        {
            Destroy(gameObject);
        }

        transform.position = new Vector3(transform.position.x, transform.position.y, timer);

    }

    [BurstCompile]
    void SpawnStages()
    {

        for(int i = 0; i < spawnStages.Length; i++)
        {

            if (!spawnStages[i].hasSpawned)
            {

                spawnStages[i].spawnTimer += Time.deltaTime;

                if(spawnStages[i].spawnTimer > spawnStages[i].spawnTime)
                {

                    foreach (SpriteRenderer sprite in spawnStages[i].sprites)
                    {

                        if (randomSpawning)
                        {

                            float spawn = Random.Range(0f, 1f);
                            if (spawn > spawnChance) continue;

                        }

                        spawnStages[i].hasSpawned = true;
                        sprite.enabled = true;

                    }

                    if(grow) spawnStages[i].doScale = true;

                }

            }

        }

    }


}
