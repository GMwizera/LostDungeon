using UnityEngine;

namespace LostDungeon.Game.Managers
{
    /// <summary>
    /// Base for managers (Singleton pattern): only one copy, reached with T.Instance.
    /// It stays across scenes. A second copy deletes itself.
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

        /// <summary>True on an extra copy. Child classes should skip their setup.</summary>
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
