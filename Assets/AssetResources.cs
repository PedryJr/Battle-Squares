using UnityEngine;

public class AssetResources : MonoBehaviour
{

    [SerializeField] Sprite smallCornerOctagon;
    public static Sprite GetSmallCornerOctagon => Instance.smallCornerOctagon;

    [SerializeField] PowerDotBehaviour powerDot;
    public static PowerDotBehaviour PowerDot => Instance.powerDot;

    public static AssetResources Instance 
    { 
        get; 
        private set; 
    }

    private void Awake() => Instance = this;


}
