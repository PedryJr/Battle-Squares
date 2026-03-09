using System.Linq;
using TMPro;
using UnityEngine;

public class MMRTester : MonoBehaviour
{

    // Updated constants for desired climbing behavior
/*    [SerializeField]
    public double baseK = 32.0;           // Higher base K-factor for ~20 MMR changes between equals
    [SerializeField]
    public double ratingScale = 500;    // Balance between rating difference impact and base gains
    [SerializeField]
    public double marginScale = 0.1;      // Minimal margin impact since we want consistent values
    [SerializeField]
    public double upsetClamp = 1.0;       // Moderate upset bonus
    [SerializeField]
    public double loserUpsetShare = 0.2;  // Losers take more penalty in upsets*/

    [SerializeField]
    MMRData[] mmrToTest;

    void OnDrawGizmos()
    {
        if (mmrToTest == null) return;
        if (mmrToTest.Length <= 0) return;
/*        MMRSystem.baseK = baseK;
        MMRSystem.ratingScale = ratingScale;
        MMRSystem.marginScale = marginScale;
        MMRSystem.upsetClamp = upsetClamp;
        MMRSystem.loserUpsetShare = loserUpsetShare;*/
        var newArr = MMRSystem.ComputeMMR(mmrToTest);

        DisplayMMR(mmrToTest, newArr);
    }

    public TMP_Text oldMMRColumn;
    public TMP_Text newMMRColumn;

    /// <summary>
    /// Call this to display MMR results in two TMP text columns.
    /// </summary>
    public void DisplayMMR(MMRData[] oldArr, MMRData[] newArr)
    {
        if (oldArr.Length != newArr.Length)
        {
            Debug.LogError("Old and new MMR arrays must have the same length!");
            return;
        }

        // Optional: sort by user ID so rows match cleanly
        oldArr = oldArr.OrderBy(p => p.UserUniqueId).ToArray();
        newArr = newArr.OrderBy(p => p.UserUniqueId).ToArray();

        System.Text.StringBuilder oldCol = new System.Text.StringBuilder();
        System.Text.StringBuilder newCol = new System.Text.StringBuilder();

        for (int i = 0; i < oldArr.Length; i++)
        {
            var oldP = oldArr[i];
            var newP = newArr[i];

            oldCol.AppendLine(
                $"Player {oldP.UserUniqueId}:  {oldP.MMR:F1}"
            );

            newCol.AppendLine(
                $"{newP.MMR:F1}"
            );
        }

        oldMMRColumn.text = oldCol.ToString();
        newMMRColumn.text = newCol.ToString();
    }
}