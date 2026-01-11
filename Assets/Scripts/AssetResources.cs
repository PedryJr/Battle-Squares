using UnityEngine;

public class AssetResources : MonoBehaviour
{

    [SerializeField] private ButtonHoverAnimationColorSettings defaultButtonHoverColorSettings;
    public static ButtonHoverAnimationColorSettings GetDefaultButtonHoverColorSettings => Instance.defaultButtonHoverColorSettings;

    [SerializeField] private Material defaultButtonMaterial;
    public static Material GetDefaultButtonMaterial => Instance.defaultButtonMaterial;


    [SerializeField] private Material hitmarkMaterial;
    public static Material GetHitmarkMaterial => Instance.hitmarkMaterial;

    [SerializeField] private Sprite smallCornerOctagon;
    public static Sprite GetSmallCornerOctagon => Instance.smallCornerOctagon;

    [SerializeField] private PowerDotBehaviour powerDot;
    public static PowerDotBehaviour PowerDot => Instance.powerDot;

    [SerializeField] private SpawnEventHandle spawnEventHandle;
    public static SpawnEventHandle SpawnEventHandle => Instance.spawnEventHandle;

    public static AssetResources Instance 
    { 
        get; 
        private set; 
    }

    private void Awake()
    {
        Instance = this;

    }
}
