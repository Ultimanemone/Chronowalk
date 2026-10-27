using ChronowalkGame.Utils.Logger;
using UnityEngine;

namespace ChronowalkGame.Utils
{
    /// <summary>
    /// Generic MonoBehaviour subclass with default creation of a singleton on Awake()
    /// </summary>
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        public static T Instance { get; private set; }

        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                AdvLogger.Log($"Duplicate singleton: {typeof(T).Name}", Logger.LogType.Error);
                Destroy(gameObject);
                return;
            }

            Instance = (T)this;
            InitSingleton();
        }

        /// <summary>
        /// Method that runs after initialization of the singleton Instance
        /// </summary>
        protected virtual void InitSingleton() { }

        protected virtual void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}