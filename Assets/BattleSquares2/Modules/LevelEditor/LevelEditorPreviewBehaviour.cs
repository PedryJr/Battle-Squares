using UnityEngine;

public class LevelEditorPreviewBehaviour : MonoBehaviour
{

    DragAndScrollMod _dragMod;
    SpriteRenderer spriteRenderer;

    Color targetColor;
    Color previewColor = new Color(1f, 1f, 1f, 0f);
    public Color normalColor;

    public Color GetTargetColor() => targetColor;

    private void Awake()
    {
        _dragMod = FindAnyObjectByType<DragAndScrollMod>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if(spriteRenderer) normalColor = spriteRenderer.color;
        else normalColor = Color.white;
    }

    private void Update()
    {
        if(_dragMod.GetPreviewing()) targetColor = previewColor;
        else targetColor = normalColor;

        if(spriteRenderer) spriteRenderer.color = Color.Lerp(spriteRenderer.color, targetColor, Time.deltaTime * 20f);
    }

}
