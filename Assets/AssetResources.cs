using UnityEngine;

public class AssetResources : MonoBehaviour
{

    [SerializeField] Sprite smallCornerOctagon;

    public static AssetResources Instance 
    { 
        get; 
        private set; 
    }

    private void Awake() => Instance = this;

    public static Sprite GetSmallCornerOctagon => Instance.smallCornerOctagon;

}
