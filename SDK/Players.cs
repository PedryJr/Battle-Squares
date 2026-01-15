using System;
using System.Numerics;

namespace BattleSquaresSDK
{
    public interface IPlayerHandle
    {
        public string Name { get; }
        public ulong NetworkID { get; }
        public ulong SteamID { get; }

        public bool IsLocal { get; }

        public Vector2 GetPosition();
        public void SetPosition(Vector2 position);

        public Vector2 GetVelocity();
        public void SetVelocity(Vector2 velocity);

        public void SetAngularVelocity(float rotation);
        public float GetAngularVelocity();

        public float GetRotation();
        public void SetRotation(float rotation);

        public float GetHealth();
        public void SetHealth(float health);

        public float GetHealthCap();
        public void SetHealthCap(float cap);

        public event Action<IPlayerHandle> OnDestroyed;
    }
}