using UnityEngine;

public class AssetResources : MonoBehaviour
{

    [SerializeField] Material hitmarkMaterial;
    public static Material GetHitmarkMaterial => Instance.hitmarkMaterial;

    [SerializeField] Sprite smallCornerOctagon;
    public static Sprite GetSmallCornerOctagon => Instance.smallCornerOctagon;

    [SerializeField] PowerDotBehaviour powerDot;
    public static PowerDotBehaviour PowerDot => Instance.powerDot;

    [SerializeField] SpawnEventHandle spawnEventHandle;
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
