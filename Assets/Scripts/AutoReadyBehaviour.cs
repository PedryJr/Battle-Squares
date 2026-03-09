using System.Runtime.CompilerServices;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class AutoReadyBehaviour : MonoBehaviour
{
    [SerializeField]
    ReadyUpButton readyUpButton;
    [SerializeField]
    TMP_Text autoCharSumbol;

    private PersistentValue<bool> autoReadyOn;

    private readonly string autoReadyOnIndicatorText = "Auto";
    private readonly string autoReadyOffIndicatorText = "Manual";

    private readonly string autoReadyOnCharText = "A";
    private readonly string autoReadyOffCharText = "M";

    private const float Interval = 0.1f;
    private float _timer;

    private void Awake()
    {
        autoReadyOn = new PersistentValue<bool>("AutoReady", false);
    }

    private void Start()
    {
        autoCharSumbol.text = autoReadyOn.Value ? autoReadyOnCharText : autoReadyOffCharText;
        if (!NetworkManager.Singleton.IsHost)
        {
            GetComponent<Image>().enabled = true;
            GetComponent<ButtonHoverAnimation>().enabled = true;
            foreach (var item in GetComponentsInChildren<TextMeshProUGUI>()) item.enabled = true;
        }
    }

    private void Update()
    {
        _timer += Time.deltaTime;

        if (_timer < Interval) return;
        _timer = 0;

        if (autoReadyOn.Value && !NetworkManager.Singleton.IsHost) ForceReadyOn();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ForceReadyOn() => readyUpButton.READY(true);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void TOGGLEAUTO(IndicatorTextBehaviour indicator)
    {
        autoReadyOn.Value = !autoReadyOn.Value;
        indicator.INDICATE(autoReadyOn.Value ? autoReadyOnIndicatorText : autoReadyOffIndicatorText);
        autoCharSumbol.text = autoReadyOn.Value ? autoReadyOnCharText : autoReadyOffCharText;
    }
}
