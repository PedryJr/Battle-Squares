using UnityEngine;

public sealed class TestModTool : LevelTransMod
{

    SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    protected override void InheritStart()
    {

        leftClickCallback += LeftClickCallback;
        leftHoldCallback += LeftHoldCallback;
        rightClickCallback += RightClickCallback;
        rightHoldCallback += RightHoldCallback;
        releaseCallback += ReleaseCallback;
    }

    protected override void InheritDestroy()
    {
        leftClickCallback -= LeftClickCallback;
        leftHoldCallback -= LeftHoldCallback;
        rightClickCallback -= RightClickCallback;
        rightHoldCallback -= RightHoldCallback;
        releaseCallback -= ReleaseCallback;
    }

    void LeftClickCallback(Vector2 position) => spriteRenderer.color = Color.red;
    void LeftHoldCallback(Vector2 position) => spriteRenderer.color = Color.yellow;
    void RightClickCallback(Vector2 position) => spriteRenderer.color = Color.green;
    void RightHoldCallback(Vector2 position) => spriteRenderer.color = Color.blue;

    void ReleaseCallback(bool releaseState)
    {
        spriteRenderer.color = Color.white;
    }

}
