using System;

namespace BattleSquaresSDK
{
    public interface ILogger
    {
        public bool enable { get; set; }
        void Log(string message);
        void Log(string message, float duration);
    }
}
