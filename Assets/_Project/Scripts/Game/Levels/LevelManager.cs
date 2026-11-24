using System.Collections;
using LostDungeon.Core.Events;
using LostDungeon.Game.Cameras;
using LostDungeon.Game.Inventory;
using LostDungeon.Game.Managers;
using LostDungeon.Game.Player;
using LostDungeon.Game.Pooling;
using LostDungeon.Game.Weather;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace LostDungeon.Game.Levels
{
    /// <summary>
    /// Loads the current level's content with Addressables and releases it when the level ends.
    /// Each level is an Addressable prefab (address "Level1" ... "Level5") built into its own asset bundle,
    /// so only the level being played is in memory. Shows loading progress and handles a failed load
    /// by reporting it to the UI instead of freezing.
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        public const string UnlockFireball = "ability.fireball";
        public const string UnlockHealthBonus = "health.bonus";
        private const int HealthBonus = 2;

        [SerializeField] private LevelCatalog catalog;
        [SerializeField] private GameObject player;
        [SerializeField] private CameraFollow cameraFollow;
        [SerializeField] private Light2D globalLight;

        private AsyncOperationHandle<GameObject> levelHandle;
        private LevelDefinition current;
        private float levelStartTime;
        private InputAction pauseAction;

        public LevelDefinition Current => current;

        private void OnEnable()
        {
            ExitDoor.ExitReached += OnExitReached;
            pauseAction = InputSystem.actions?.FindAction("Player/Pause");
            if (pauseAction != null)
                pauseAction.performed += OnPausePressed;
        }

        private void OnDisable()
        {
            ExitDoor.ExitReached -= OnExitReached;
            if (pauseAction != null)
                pauseAction.performed -= OnPausePressed;
        }

        private IEnumerator Start()
        {
            yield return LoadLevel(GameManager.Instance.CurrentLevel);
        }

        private IEnumerator LoadLevel(int number)
        {
            ReleaseLevel();
            current = catalog != null ? catalog.Get(number) : null;
            if (current == null)
            {
                GameEvents.RaiseLevelLoadFailed($"Level {number} does not exist.");
                yield break;
            }

            GameManager.Instance.SetState(GameState.Loading);
            player.SetActive(false);

            levelHandle = Addressables.InstantiateAsync(current.AddressKey, Vector3.zero, Quaternion.identity);
            while (!levelHandle.IsDone)
            {
                GameEvents.RaiseLevelLoadProgress(levelHandle.PercentComplete);
                yield return null;
            }

            if (levelHandle.Status != AsyncOperationStatus.Succeeded || levelHandle.Result == null)
            {
                string reason = levelHandle.OperationException != null ? levelHandle.OperationException.Message : "unknown error";
                Debug.LogError($"Failed to load {current.AddressKey}: {reason}");
                ReleaseLevel();
                GameEvents.RaiseLevelLoadFailed($"Could not load {current.DisplayName}. Check that the Addressables content was built.");
                yield break;
            }

            GameEvents.RaiseLevelLoadProgress(1f);
            var root = levelHandle.Result.GetComponent<LevelRoot>();
            PlacePlayer(root != null && root.PlayerSpawn != null ? root.PlayerSpawn.position : Vector3.zero);

            if (globalLight != null)
                globalLight.intensity = WorldModifiers.LightIntensity;

            levelStartTime = Time.time;
            GameManager.Instance.SetState(GameState.Playing);
            GameEvents.RaiseLevelStarted(current.Number, current.DisplayName);
            if (root != null && root.Objective != null)
                root.Objective.Publish();
        }

        /// <summary>Applies saved rewards (unlocked fireball, extra health) and items, then drops the player in.</summary>
        private void PlacePlayer(Vector3 position)
        {
            SaveManager save = SaveManager.Instance;
            player.transform.position = position;

            player.GetComponent<PlayerHealth>().ConfigureMaxHealth(save.HasUnlock(UnlockHealthBonus) ? HealthBonus : 0);
            player.SetActive(true);

            player.GetComponent<PlayerAbilities>().SetSpecialUnlocked(save.HasUnlock(UnlockFireball));
            player.GetComponent<PlayerInventory>().Load(save.Data.inventory);

            if (cameraFollow != null)
                cameraFollow.SnapToTarget();
        }

        private void OnExitReached()
        {
            if (current == null || GameManager.Instance.State != GameState.Playing)
                return;

            SaveManager.Instance.RecordLevelComplete(current.Number, current.RewardUnlock,
                player.GetComponent<PlayerInventory>().ToSaveEntries(), Time.time - levelStartTime);

            GameManager.Instance.SetState(GameState.LevelComplete);
            GameEvents.RaiseLevelCompleted(current.Number);
        }

        private void OnPausePressed(InputAction.CallbackContext _) => GameManager.Instance.TogglePause();

        /// <summary>
        /// Releases the level through Addressables (destroys the instance and lets its bundle unload).
        /// Pooled enemies and arrows are cleared first: they were created from prefabs inside the
        /// level's bundle, so they must not outlive it.
        /// </summary>
        private void ReleaseLevel()
        {
            if (PoolManager.HasInstance)
                PoolManager.Instance.Clear();

            if (levelHandle.IsValid())
                Addressables.Release(levelHandle);
            levelHandle = default;
        }

        private void OnDestroy() => ReleaseLevel();
    }
}
