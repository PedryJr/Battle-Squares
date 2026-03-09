using TMPro;
using UnityEngine;
using System.Text; // <-- needed for StringBuilder

public class PlayerDebugStats : MonoBehaviour
{
    [SerializeField] PlayerBehaviour playerBehaviour;
    [SerializeField] TMP_Text textField;

    public const string NamePrefix = "Name: ";
    public const string HealthPrefix = "Health: ";
    public const string VelocityPrefix = "Velocity: ";
    public const string PlayerFacingPrefix = "Facing: ";
    public const string LifeState = "LifeState: ";
    public const string PrimaryWeaponPrefix = "PrimaryWeapon: ";
    public const string SecondaryWeaponPrefix = "SecondaryWeapon: ";
    public const string LocalityPrefix = "Connection: ";
    public const string IdPrefix = "Id: ";
    public const char NewLine = '\n';

    StringBuilder builder = new StringBuilder(256);

    string GetFacingFromVectorDirection()
    {
        Vector2 direction = playerBehaviour.nozzleBehaviour.transform.localPosition;

        if (direction == Vector2.zero) return "None";

        direction.Normalize();

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;

        int sector = Mathf.RoundToInt(angle / 45f) % 8;

        switch (sector)
        {
            case 0: return "East";
            case 1: return "North-East";
            case 2: return "North";
            case 3: return "North-West";
            case 4: return "West";
            case 5: return "South-West";
            case 6: return "South";
            case 7: return "South-East";
            default: return "None";
        }
    }

    string GetPlayerAliveState()
    {
        if (playerBehaviour.isDead) return "Dead";
        else return "Alive";
    }

    string GetPlayerLocality()
    {
        if (playerBehaviour.isLocalPlayer) return "Local";
        else return "Remote";
    }

    string GetPlayerClientID()
    {
        return playerBehaviour.GetNetworkID().ToString();
    }

    private void Update()
    {
        builder.Clear();

        builder.Append("-= Player Stats =-");
        builder.Append(NewLine);

        builder.Append(NamePrefix);
        builder.Append(playerBehaviour.playerName);
        builder.Append(NewLine);

        builder.Append(HealthPrefix);
        builder.Append(playerBehaviour.healthPoints);
        builder.Append(NewLine);

        builder.Append(VelocityPrefix);
        builder.Append(playerBehaviour.velocity.magnitude);
        builder.Append(NewLine);

        builder.Append(LifeState);
        builder.Append(GetPlayerAliveState());
        builder.Append(NewLine);

        builder.Append(PlayerFacingPrefix);
        builder.Append(GetFacingFromVectorDirection());
        builder.Append(NewLine);

        builder.Append(PrimaryWeaponPrefix);
        builder.Append(playerBehaviour.nozzleBehaviour.primary);
        builder.Append(NewLine);

        builder.Append(SecondaryWeaponPrefix);
        builder.Append(playerBehaviour.nozzleBehaviour.secondary);
        builder.Append(NewLine);

        builder.Append(IdPrefix);
        builder.Append(playerBehaviour.GetGameID());
        builder.Append(NewLine);

        builder.Append(IdPrefix);
        builder.Append(GetPlayerClientID());
        builder.Append(NewLine);

        builder.Append(LocalityPrefix);
        builder.Append(GetPlayerLocality());

        textField.text = builder.ToString();
    }
}
