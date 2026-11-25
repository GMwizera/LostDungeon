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

        public static T Instance
        {
            get
            {
                if (AppQuitting.Value)
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

            if (transform.parent == null)
                DontDestroyOnLoad(gameObject);
        }

        protected virtual void OnApplicationQuit() => AppQuitting.Value = true;

        protected virtual void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }
    }

    /// <summary>
    /// True while the game is closing, so managers don't get made again.
    /// One shared flag, so PlayModeReset can clear it before the next Play.
    /// </summary>
    internal static class AppQuitting
    {
        public static bool Value;
    }
}
