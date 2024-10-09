using UnityEngine;

public class PlayerFragmentBehaviour : MonoBehaviour
{

    Vector3 targetScale;
    Vector3 targetPosition;
    Vector3 startPosition;

    [SerializeField]
    public AnimationCurve slottingAnimation;

    [SerializeField]
    float buildingTime;
    float timer;

    [SerializeField]
    float randomDistance;

    public Color startColor;

    [SerializeField]
    public bool darkerColor;

    [SerializeField]
    bool finalFragment;

    PlayerSpawnEffectBehaviour spawnEffect;

    public void Init(PlayerSpawnEffectBehaviour spawnEffect, Transform playerConstructor)
    {

        startColor = GetComponent<SpriteRenderer>().color;
        targetScale = transform.localScale;
        targetPosition = transform.position;
        transform.localScale = Vector3.zero;

        startPosition = playerConstructor.position + new Vector3(Random.Range(-randomDistance, randomDistance), 0, 0);
        startPosition.z = 0;
        transform.position = startPosition;

        this.spawnEffect = spawnEffect;

    }

    private void Start()
    {

        transform.position = startPosition;

    }

    private void Update()
    {

        timer += Time.deltaTime;
        timer = Mathf.Clamp(timer, 0, buildingTime);

        transform.position = Vector2.Lerp(startPosition, targetPosition, MyExtentions.EaseOutQuad(timer / buildingTime));
        transform.localScale = Vector2.Lerp(Vector3.zero, targetScale, Mathf.SmoothStep(0, 1, timer / buildingTime));

        float slottingLerp = slottingAnimation.Evaluate(timer / buildingTime);
        transform.localPosition = transform.localPosition + (transform.localPosition.normalized * slottingLerp);

        if (finalFragment && timer >= buildingTime && !spawnEffect.deleteFragmentBehaviours) spawnEffect.deleteFragmentBehaviours = true;

    }

}
