using System.Text;
using TMPro;
using UnityEngine;
using static ContextCounter;

[ExecuteAlways]
public class WorkshopContextSectionBehaviour : MonoBehaviour
{

    const string OK_SYMBOL = "<sprite name=StatusSymbols_2>";
    const string WARNING_SYMBOL = "<sprite name=StatusSymbols_1>";
    const string ERROR_SYMBOL = "<sprite name=StatusSymbols_0>";

    public enum ContextStatus
    {
        Ok, Warning, Error
    }

    ContextStatus currentStatus;

    public string GetStatusSymbol => currentStatus switch
    {
        ContextStatus.Ok => OK_SYMBOL,
        ContextStatus.Warning => WARNING_SYMBOL,
        ContextStatus.Error => ERROR_SYMBOL,
        _ => string.Empty
    };

    [SerializeField] TMP_SpriteAsset symbols;
    [SerializeField] string descriptorPrefix;
    [SerializeField] TMP_Text contextDescriptor;

    [SerializeField] int maxCount;
    ContextCounter[] counters;



    bool FindIssue()
    {
        bool issueRaised = false;

        if (counters == null) counters = GetComponentsInChildren<ContextCounter>();

        void PrintNoDescriptorPrefixError()
        {
            Debug.Log("No descriptor prefix!");
            issueRaised = true;
        }
        void PrintNoDescriptorObjectError()
        {
            Debug.Log("No context descriptor (Text Object) assigned!");
            issueRaised = true;
        }
        void PrintNoCounters()
        {
            Debug.Log("No counters found in any child objects!");
            issueRaised = true;
        }

        if (descriptorPrefix == string.Empty) PrintNoDescriptorPrefixError();
        if (counters.Length == 0) PrintNoCounters();
        if (!contextDescriptor) PrintNoDescriptorObjectError();
        return issueRaised;
    }

    void UpdateStatus(int count)
    {
        if (count == 0) currentStatus = ContextStatus.Warning;
        else if (count > maxCount) currentStatus = ContextStatus.Error;
        else currentStatus = ContextStatus.Ok;
    }

    int FetchCount()
    {
        int count = 0;
        for (int i = 0; i < counters.Length; i++) if (counters[i]) count += counters[i].GetCount;
        UpdateStatus(count);
        return count;
    }

    [ContextMenu("Test Context")]
    void FetchContext()
    {
        if (FindIssue()) return;
        contextDescriptor.spriteAsset = symbols;
        int count = FetchCount();
        contextDescriptor.text = new StringBuilder(128).Append(descriptorPrefix).Append(":  ").Append(count).Append('/').Append(maxCount).Append("   ").Append(GetStatusSymbol).ToString();
    }

    private void Update()
    {
        FetchContext();
    }

}
