using UnityEngine;

[ExecuteInEditMode]
public class ParentWidthToPosX : MonoBehaviour
{

    [SerializeField]
    bool useParentWidth = true;
    [SerializeField]
    float padding = 0;

    [SerializeField]
    bool flipX;

    RectTransform rectTransform;
    RectTransform parent;
    private void Update()
    {
        parent = (RectTransform)transform.parent;
        rectTransform = (RectTransform)transform;

        Vector3 pos = rectTransform.anchoredPosition;

        float pW = useParentWidth ? parent.rect.width : 0;
        pW += padding;

        pos.x = flipX ? pW : -pW;

        rectTransform.anchoredPosition = pos;

    }

}
