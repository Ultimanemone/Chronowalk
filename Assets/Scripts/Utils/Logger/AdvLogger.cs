using UnityEngine;

namespace ChronowalkGame.Utils.Logger
{
    public static class AdvLogger
    {
        /// <summary>
        /// Log a message to local file
        /// </summary>
        /// <param name="message">The message</param>
        /// <param name="logType">Type of log, default is None</param>
        public static void Log(string message, LogType logType = LogType.None)
        {
            switch (logType)
            {
                case LogType.Warning:
                    Debug.LogWarning(message);
                    break;
                case LogType.Error:
                    Debug.LogError(message);
                    break;
                default:
                    Debug.Log(message);
                    break;
            }
        }
    }
}