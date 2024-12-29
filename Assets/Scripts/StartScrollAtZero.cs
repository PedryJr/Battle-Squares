using UnityEngine;
using UnityEngine.UI;

public sealed class StartScrollAtZero : MonoBehaviour
{

    ScrollRect scrollRect;

    private void Start()
    {

        scrollRect = GetComponent<ScrollRect>();
        if (scrollRect.horizontal) scrollRect.horizontalNormalizedPosition = 0;
        if (scrollRect.vertical) scrollRect.verticalNormalizedPosition = 0;

    }

}
