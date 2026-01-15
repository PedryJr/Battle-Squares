using BattleSquaresSDK;
using System;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("Assembly-CSharp")]
namespace BattleSquaresSDK
{
    public interface IModContext
    {
        ILogger Logger { get; }
        public string GetPathToRelative(string relativePath);
        public string GetPathToRoot();

        public delegate void ProjectileSpawnEvent(ref ProjectileSpawnData projectile);
        public delegate void ProjectileCreationEvent(ref ProjectileCreator typeID);
        void PlayAudio(string path);
        void SubscribeToProjectileSpawnEvent(ProjectileSpawnEvent handler);
        void OnCreateProjectileAssets(ProjectileCreationEvent handler);
    }
}