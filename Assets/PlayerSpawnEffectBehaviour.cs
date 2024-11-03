using UnityEngine;

public class PlayerSpawnEffectBehaviour : MonoBehaviour
{


    [SerializeField]
    public AnimationCurve slottingAnimation;

    [SerializeField]
    AnimationCurve colorAnimation;

    SpriteRenderer[] fragments;

    PlayerBehaviour playerBehaviour;
    PlayerFragmentBehaviour[] fragmentBehaviours;

    PlayerConstructorBehaviour playerConstructor;

    [SerializeField]
    float maxTime;

    public float timer = 0;

    [SerializeField]
    Vector2 fromScale;

    [SerializeField]
    Vector2 toScale;

    private void Awake()
    {

        playerConstructor = FindAnyObjectByType<PlayerConstructorBehaviour>();
        fragments = GetComponentsInChildren<SpriteRenderer>(true);
        fragmentBehaviours = GetComponentsInChildren<PlayerFragmentBehaviour>(true);

        foreach (var fragment in fragmentBehaviours)
        {
            fragment.Init(this, playerConstructor.transform);
        }

    }

    public void Init(PlayerBehaviour player)
    {
        
        playerBehaviour = player;
        playerConstructor.StartNewConstruction(player);

    }

    int fragmentIndex = -1;
    int lastFragmentIndex;

    bool constructionBegun = false;

    public bool deleteFragmentBehaviours;

    private void Update()
    {

        if (!playerBehaviour) return;

        timer += Time.deltaTime;

        fragmentIndex = Mathf.FloorToInt(timer * fragments.Length / (maxTime / 2f));

        if(lastFragmentIndex != fragmentIndex && !constructionBegun)
        {
            lastFragmentIndex = fragmentIndex;

            try
            {

                fragments[fragmentIndex].gameObject.SetActive(true);

            }
            catch
            {

                if (!constructionBegun)
                {

                    playerConstructor.EndConstruction();
                    constructionBegun = true;

                }

            }

        }

        if (deleteFragmentBehaviours) 
        {
        
            deleteFragmentBehaviours = false;

            foreach (var fragment in fragmentBehaviours)
            {
                Destroy(fragment);
            }
        
        }

        for (int i = 0; i < fragments.Length; i++)
        {

            if (fragmentBehaviours[i].darkerColor)
            {

                fragments[i].color = Color.Lerp(fragmentBehaviours[i].startColor, playerBehaviour.playerDarkerColor, colorAnimation.Evaluate(timer / maxTime));

            }
            else
            {

                fragments[i].color = Color.Lerp(fragmentBehaviours[i].startColor, playerBehaviour.playerColor, colorAnimation.Evaluate(timer / maxTime));

            }

        }

        transform.localScale = Vector3.Lerp(fromScale, toScale, colorAnimation.Evaluate(timer / maxTime));

        if (timer > maxTime)
        {
            playerBehaviour.RevivePlayer();
            Destroy(gameObject);
        }

    }

}
