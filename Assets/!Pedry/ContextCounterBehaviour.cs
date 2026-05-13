using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ContextCounter : MonoBehaviour
{

    public enum CounterType
    {
        TextElements, TextLetters, Images
    }

    [Header("Optional")]
    [SerializeField] GameObject contextParent;
    const string IgnoreTag = "Ignore";
    [Header("Required")]
    [SerializeField] CounterType counterType;

    public int GetCount => counterType switch
    {
        CounterType.TextElements => CountTextElements(),
        CounterType.TextLetters => CountTextLetters(),
        CounterType.Images => CountImages(),
        _ => -1
    };

    void ForeachComp<T>(Action<T> func) where T : Component
    {
        T aComp = GetComponent<T>();
        if (aComp) if (!IgnoreTag.Equals(aComp.gameObject.tag)) func(aComp);
        T[] comps = GetComponentsInChildren<T>();
        for (int i = 0; i < comps.Length; i++) if(comps[i]) if (!IgnoreTag.Equals(comps[i].gameObject.tag)) func(comps[i]);
    }

    public int CountTextElements()
    {
        if (!contextParent) contextParent = gameObject;
        int count = 0;
        ForeachComp<TMP_Text>((textComp) => { count++; });
        return count;
    }

    public int CountTextLetters()
    {
        if (!contextParent) contextParent = gameObject;
        int count = 0;
        ForeachComp<TMP_Text>((textComp) => { count+= MyExtentions.RemoveInvisibleChars(textComp.text).Length; });
        return count;
    }

    public int CountImages()
    {
        if (!contextParent) contextParent = gameObject;
        int count = 0;
        ForeachComp<Image>((imageComp) => { count++; });
        return count;
    }

    private void Awake()
    {
        if (!contextParent) contextParent = gameObject;
    }

    void Update()
    {
        
    }
}
