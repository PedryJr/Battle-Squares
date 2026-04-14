using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using TMPro;
using UnityEngine;

public class ContentListing : MonoBehaviour
{
    [SerializeField] TMP_Text textField;

    ContentScanner scanner;

/*    private void OnDestroy()
    {
        scanner.activeListings.Remove(this);
    }*/

    public void Initialize(ContentNode node, ContentScanner scanner, List<bool> isLastFlags)
    {
        //scanner.activeListings.Add(this);

        this.scanner = scanner;

        int indentWidth = scanner.IndentWidth;
        int flagCount = isLastFlags.Count;

        int extensionLength = node.isFile && node.extension != null ? node.extension.Length : 0;

        int capacity = flagCount * (indentWidth + 2)
                     + scanner.BranchHorizontalCount + 4
                     + scanner.ExtraSpacing
                     + node.name.Length
                     + extensionLength
                     + 10;

        StringBuilder sb = new StringBuilder(capacity);

        for (int i = 0; i < flagCount - 1; i++)
        {
            if (isLastFlags[i])
            {
                sb.Append(' ', indentWidth + scanner.ChainedIndentPadding);
            }
            else
            {
                AppendColor(sb, scanner.SeparatorColor);
                sb.Append(scanner.Vertical);
                sb.Append(' ', indentWidth - 1);
            }
        }

        if (flagCount > 0)
        {
            AppendColor(sb, scanner.SeparatorColor);
            sb.Append(isLastFlags[^1] ? scanner.BranchEnd : scanner.BranchMid);

            for (int i = 0; i < scanner.BranchHorizontalCount; i++)
                sb.Append(scanner.Horizontal);

            sb.Append(' ', scanner.ExtraSpacing);
        }

        if (node.isFile)
        {
            AppendColor(sb, scanner.FileNameColor);
            sb.Append(node.name);

            AppendColor(sb, scanner.ExtensionColor);
            AppendUpper(sb, node.extension);
        }
        else
        {
            AppendColor(sb, node.isRoot ? scanner.RootMarkerColor : scanner.DirectoryColor);
            sb.Append(node.name);
        }

        textField.text = MyExtentions.Format(sb.ToString());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void AppendColor(StringBuilder sb, ColorOption color)
    {
        sb.Append('&');
        sb.Append((char)color);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void AppendUpper(StringBuilder sb, string s)
    {
        foreach (char c in s)
            sb.Append(char.ToUpperInvariant(c));
    }

    [SerializeField] RectTransform topBReference;
    [SerializeField] RectTransform botBReference;

    public void Update()
    {
        float itemTop = topBReference.position.y;
        float itemBot = botBReference.position.y;

        float maskTop = scanner.TopBorder.position.y;
        float maskBot = scanner.BotBorder.position.y;

        float fadeTop = maskTop - scanner.FadeDistance;
        float fadeBot = maskBot + scanner.FadeDistance;

        Color c = textField.color;
        float alpha;

        if (itemTop < maskBot || itemBot > maskTop)
        {
            alpha = 0f;
        }
        else if (itemTop > fadeTop)
        {
            float t = (maskTop - itemTop) / scanner.FadeDistance;
            alpha = scanner.FadeAnimation.Evaluate(t);
        }
        else if (itemBot < fadeBot)
        {
            float t = (itemBot - maskBot) / scanner.FadeDistance;
            alpha = scanner.FadeAnimation.Evaluate(t);
        }
        else
        {
            alpha = 1f;
        }

        c.a = alpha;
        textField.color = c;
    }
}