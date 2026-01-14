using BattleSquaresSDK;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class VLog : MonoBehaviour
{
    [SerializeField]
    LogElementAnimation logElement;

    [SerializeField]
    int maxLogs = 10;

    static VLog instance;
    static Queue<LogElementAnimation> activeElements = new Queue<LogElementAnimation>();

    private void Awake()
    {
        instance = this;
        DontDestroyOnLoad(transform.parent);
    }

    public static void Log(string message)
    {
        while (activeElements.Count >= instance.maxLogs)
        {
            LogElementAnimation oldest = activeElements.Dequeue();
            if (oldest != null) oldest.PrematureFadeout();
        }

        LogElementAnimation newLog = Instantiate(instance.logElement, instance.transform);
        newLog.text.text = Format(message);
        newLog.onExpire = ElementExpired;
        activeElements.Enqueue(newLog);

        Debug.Log(message);
    }

    public static void Log(string message, float duration)
    {

        while (activeElements.Count >= instance.maxLogs)
        {
            LogElementAnimation oldest = activeElements.Dequeue();
            if (oldest != null) oldest.PrematureFadeout();
        }

        LogElementAnimation newLog = Instantiate(instance.logElement, instance.transform);
        newLog.text.text = Format(message);
        newLog.onExpire = ElementExpired;
        newLog.timeToStay = duration;
        activeElements.Enqueue(newLog);

        Debug.Log(message);
    }

    static void ElementExpired(LogElementAnimation element)
    {
        if (activeElements.Contains(element))
        {
            Queue<LogElementAnimation> temp = new Queue<LogElementAnimation>();
            while (activeElements.Count > 0)
            {
                LogElementAnimation item = activeElements.Dequeue();
                if (item != element) temp.Enqueue(item);
            }
            activeElements = temp;
        }
    }

    private static readonly Dictionary<char, string> ColorMap = new()
    {
        ['0'] = "#000000", ['1'] = "#0000AA", ['2'] = "#00AA00", ['3'] = "#00AAAA",
        ['4'] = "#AA0000", ['5'] = "#AA00AA", ['6'] = "#FFAA00", ['7'] = "#AAAAAA",
        ['8'] = "#555555", ['9'] = "#5555FF", ['a'] = "#55FF55", ['b'] = "#55FFFF",
        ['c'] = "#FF5555", ['d'] = "#FF55FF", ['e'] = "#FFFF55", ['f'] = "#FFFFFF",
        ['g'] = "#DDD605", ['h'] = "#E3D4D1", ['i'] = "#CECACA", ['j'] = "#443A3B",
        ['m'] = "#971607", ['n'] = "#B4684D", ['p'] = "#DEB12D", ['q'] = "#47A036",
        ['s'] = "#2CBAA8", ['t'] = "#21497B", ['u'] = "#9A5CC6", ['v'] = "#EB7114"
    };

    public static string Format(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var sb = new StringBuilder(input.Length * 2);
        var openTags = new Stack<string>(8);

        for (int i = 0; i < input.Length; i++)
        {
            if (input[i] == '§' && i + 1 < input.Length)
            {
                char code = input[++i];

                if (code == 'r')
                {
                    while (openTags.Count > 0)
                        sb.Append(openTags.Pop());
                }
                else if (ColorMap.TryGetValue(code, out string hex))
                {
                    sb.Append("<color=").Append(hex).Append('>');
                    openTags.Push("</color>");
                }
                else
                {
                    var tag = code switch
                    {
                        'l' => ("</b>", "<b>"),
                        'm' => ("</s>", "<s>"),
                        'n' => ("</u>", "<u>"),
                        'o' => ("</i>", "<i>"),
                        _ => (null, null)
                    };

                    if (tag.Item1 != null)
                    {
                        sb.Append(tag.Item2);
                        openTags.Push(tag.Item1);
                    }
                }
            }
            else
            {
                sb.Append(input[i]);
            }
        }

        while (openTags.Count > 0)
            sb.Append(openTags.Pop());

        return sb.ToString();
    }
}