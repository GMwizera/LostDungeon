using UnityEngine;

namespace LostDungeon.Game.Managers
{
    /// <summary>
    /// Base class for global managers (Singleton pattern): one instance that lives across scenes
    /// and is reachable from anywhere as T.Instance. Used for systems that must exist exactly once,
    /// such as PoolManager, and later GameManager, AudioManager and SaveManager.
    /// A second copy (for example from loading a scene twice) destroys itself.
    /// </summary>
    public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        private static T instance;
        private static bool quitting;

        public static T Instance
        {
            get
            {
                if (quitting)
                    return null;

                if (instance == null)
                {
                    instance = FindAnyObjectByType<T>();
                    if (instance == null)
                        instance = new GameObject(typeof(T).Name).AddComponent<T>();
                }

                return instance;
            }
        }

        public static bool HasInstance => instance != null;

        /// <summary>True on an extra copy that is about to destroy itself; subclasses should skip their setup.</summary>
        protected bool IsDuplicate { get; private set; }

        protected virtual void Awake()
        {
            if (instance != null && instance != this)
            {
                IsDuplicate = true;
                Destroy(gameObject);
                return;
            }

            instance = (T)this;
            quitting = false;

            if (transform.parent == null)
                DontDestroyOnLoad(gameObject);
        }

        protected virtual void OnApplicationQuit() => quitting = true;

        protected virtual void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }
    }
}
