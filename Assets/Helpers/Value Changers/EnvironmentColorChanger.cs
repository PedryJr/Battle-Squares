using UnityEngine;

public class EnvironmentColorChanger : MonoBehaviour
{

    PlayerSynchronizer playerSynchronizer;

    [SerializeField]
    Color pixelationBaseColor;
    [SerializeField]
    Material pixelMaterial;
    [SerializeField]
    Color backgroundBaseColor;
    SpriteRenderer backgroundSpriteRenderer;

    private void Awake()
    {
        playerSynchronizer = FindAnyObjectByType<PlayerSynchronizer>();
        if(playerSynchronizer.localSquare.selectedLegacyMap) gameObject.SetActive(false);
    }

/*    private void Update()
    {
        if (!backgroundSpriteRenderer) backgroundSpriteRenderer = GameObject.Find("-- Backdrop --").GetComponent<SpriteRenderer>();
    }
*/
    public void COLORCHANGE(float val)
    {

        float h, s, v;
        Color.RGBToHSV(pixelationBaseColor, out h, out s, out v);


    }

}
