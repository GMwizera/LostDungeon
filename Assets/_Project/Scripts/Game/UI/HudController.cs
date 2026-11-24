using System.Collections.Generic;
using LostDungeon.Core.Events;
using LostDungeon.Game.Levels;
using LostDungeon.Game.Managers;
using LostDungeon.Game.Platform;
using LostDungeon.Game.Player;
using LostDungeon.Game.Weather;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace LostDungeon.Game.UI
{
    /// <summary>
    /// In-game HUD and overlays (pause, level complete, game over, loading).
    /// It only listens to GameEvents and calls GameManager for button actions, so gameplay code
    /// never references the UI: the HUD could be replaced or removed without touching combat or levels.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class HudController : MonoBehaviour
    {
        private class AbilityView
        {
            public VisualElement CooldownFill;
            public VisualElement Lock;
            public float Duration;
            public float Remaining;
        }

        [SerializeField] private UIIcons icons;
        [SerializeField] private PlayerInputReader playerInput;

        private readonly Dictionary<string, AbilityView> abilities = new Dictionary<string, AbilityView>();
        private readonly Dictionary<string, Label> itemCounts = new Dictionary<string, Label>();
        private VisualElement root;
        private VisualElement hearts;
        private VisualElement inventory;
        private Label objective;
        private Label levelName;
        private Label prompt;
        private Label conditions;
        private VisualElement pauseOverlay;
        private VisualElement completeOverlay;
        private VisualElement gameOverOverlay;
        private VisualElement loadingOverlay;
        private VisualElement loadingFill;
        private Label loadingMessage;
        private int enemiesThisLevel;
        private int goldThisLevel;

        private void OnEnable()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            hearts = root.Q("hearts");
            inventory = root.Q("inventory");
            objective = root.Q<Label>("objective");
            levelName = root.Q<Label>("level-name");
            prompt = root.Q<Label>("prompt");
            conditions = root.Q<Label>("conditions");
            pauseOverlay = root.Q("pause-overlay");
            completeOverlay = root.Q("complete-overlay");
            gameOverOverlay = root.Q("gameover-overlay");
            loadingOverlay = root.Q("loading-overlay");
            loadingFill = root.Q("loading-fill");
            loadingMessage = root.Q<Label>("loading-message");

            CreateAbilitySlots();
            BindButtons();
            SetUpTouchControls();
            UISounds.AddClickSounds(root);

            GameEvents.PlayerHealthChanged += OnHealthChanged;
            GameEvents.InventoryChanged += OnInventoryChanged;
            GameEvents.ObjectiveChanged += OnObjectiveChanged;
            GameEvents.InteractionPromptChanged += OnPromptChanged;
            GameEvents.AbilityActivated += OnAbilityActivated;
            GameEvents.LevelStarted += OnLevelStarted;
            GameEvents.LevelLoadProgress += OnLoadProgress;
            GameEvents.LevelLoadFailed += OnLoadFailed;
            GameEvents.GameStateChanged += OnStateChanged;
            GameEvents.ConditionsChanged += OnConditionsChanged;
            GameEvents.EnemyDefeated += OnEnemyDefeated;
            GameEvents.ItemCollected += OnItemCollected;

            OnPromptChanged(null);
            OnStateChanged(GameState.Loading);
            WeatherService weather = WeatherService.Instance;
            OnConditionsChanged(weather.Conditions.Title, weather.Summary);
        }

        private void OnDisable()
        {
            GameEvents.PlayerHealthChanged -= OnHealthChanged;
            GameEvents.InventoryChanged -= OnInventoryChanged;
            GameEvents.ObjectiveChanged -= OnObjectiveChanged;
            GameEvents.InteractionPromptChanged -= OnPromptChanged;
            GameEvents.AbilityActivated -= OnAbilityActivated;
            GameEvents.LevelStarted -= OnLevelStarted;
            GameEvents.LevelLoadProgress -= OnLoadProgress;
            GameEvents.LevelLoadFailed -= OnLoadFailed;
            GameEvents.GameStateChanged -= OnStateChanged;
            GameEvents.ConditionsChanged -= OnConditionsChanged;
            GameEvents.EnemyDefeated -= OnEnemyDefeated;
            GameEvents.ItemCollected -= OnItemCollected;
        }

        private void Update()
        {
            // Cooldown fills count down locally from the AbilityActivated event (no reference to the player).
            foreach (AbilityView view in abilities.Values)
            {
                if (view.Remaining <= 0f)
                    continue;
                view.Remaining = Mathf.Max(0f, view.Remaining - Time.deltaTime);
                view.CooldownFill.style.height = Length.Percent(view.Duration > 0f ? view.Remaining / view.Duration * 100f : 0f);
            }
        }

        // ---------- Setup ----------

        private void CreateAbilitySlots()
        {
            VisualElement bar = root.Q("abilities");
            bar.Clear();
            abilities.Clear();
            AddAbility(bar, "Sword", icons.sword, "LMB");
            AddAbility(bar, "Dash", icons.dash, "Space");
            AddAbility(bar, "Fireball", icons.fireball, "Q");
        }

        private void AddAbility(VisualElement bar, string abilityName, Sprite icon, string keyHint)
        {
            var slot = new VisualElement();
            slot.AddToClassList("ability-slot");

            var image = new VisualElement();
            image.AddToClassList("ability-icon");
            image.style.backgroundImage = new StyleBackground(icon);
            slot.Add(image);

            var fill = new VisualElement();
            fill.AddToClassList("ability-cooldown");
            slot.Add(fill);

            var lockIcon = new VisualElement();
            lockIcon.AddToClassList("ability-lock");
            lockIcon.style.backgroundImage = new StyleBackground(icons.locked);
            UISounds.Show(lockIcon, false);
            slot.Add(lockIcon);

            if (!PlatformFeatures.IsMobile)
            {
                var key = new Label(keyHint);
                key.AddToClassList("ability-key");
                slot.Add(key);
            }

            bar.Add(slot);
            abilities[abilityName] = new AbilityView { CooldownFill = fill, Lock = lockIcon };
        }

        private void BindButtons()
        {
            GameManager game = GameManager.Instance;
            root.Q<Button>("pause-button").clicked += game.TogglePause;
            root.Q<Button>("resume").clicked += game.TogglePause;
            root.Q<Button>("pause-restart").clicked += game.RestartLevel;
            root.Q<Button>("pause-menu").clicked += game.ReturnToMenu;
            root.Q<Button>("next-level").clicked += game.NextLevel;
            root.Q<Button>("complete-menu").clicked += game.ReturnToMenu;
            root.Q<Button>("retry").clicked += game.RestartLevel;
            root.Q<Button>("gameover-menu").clicked += game.ReturnToMenu;
            root.Q<Button>("loading-back").clicked += game.ReturnToMenu;
        }

        /// <summary>
        /// On-screen joystick and buttons. Shown on phones (and on touch screens in the browser)
        /// unless turned off in Settings.
        /// </summary>
        private void SetUpTouchControls()
        {
            VisualElement touch = root.Q("touch");
            bool wanted = SaveManager.Instance.Data.settings.showTouchControls &&
                          (PlatformFeatures.IsMobile || Touchscreen.current != null);
            UISounds.Show(touch, wanted);
            if (!wanted || playerInput == null)
                return;

            VisualElement stick = root.Q("joystick");
            VisualElement knob = root.Q("joystick-knob");
            int activePointer = -1;

            stick.RegisterCallback<PointerDownEvent>(e =>
            {
                activePointer = e.pointerId;
                stick.CapturePointer(e.pointerId);
                MoveKnob(e.localPosition);
            });
            stick.RegisterCallback<PointerMoveEvent>(e =>
            {
                if (e.pointerId == activePointer)
                    MoveKnob(e.localPosition);
            });
            stick.RegisterCallback<PointerUpEvent>(e =>
            {
                if (e.pointerId != activePointer)
                    return;
                activePointer = -1;
                stick.ReleasePointer(e.pointerId);
                knob.style.translate = new Translate(0, 0);
                playerInput.SetVirtualMove(Vector2.zero);
            });

            void MoveKnob(Vector3 local)
            {
                float radius = stick.resolvedStyle.width * 0.5f;
                var offset = new Vector2(local.x - radius, local.y - radius);
                offset = Vector2.ClampMagnitude(offset, radius);
                knob.style.translate = new Translate(offset.x, offset.y);
                playerInput.SetVirtualMove(new Vector2(offset.x, -offset.y) / radius); // UI y points down
            }

            root.Q("touch-attack").style.backgroundImage = new StyleBackground(icons.sword);
            root.Q("touch-dash").style.backgroundImage = new StyleBackground(icons.dash);
            root.Q("touch-fireball").style.backgroundImage = new StyleBackground(icons.fireball);
            root.Q("touch-interact").style.backgroundImage = new StyleBackground(icons.available);

            BindTouchButton("touch-attack", playerInput.PressAttack);
            BindTouchButton("touch-dash", playerInput.PressDash);
            BindTouchButton("touch-fireball", playerInput.PressFireball);
            BindTouchButton("touch-interact", playerInput.PressInteract);
        }

        private void BindTouchButton(string name, System.Action action) =>
            root.Q(name).RegisterCallback<PointerDownEvent>(_ => action());

        // ---------- Event handlers ----------

        private void OnHealthChanged(int current, int max)
        {
            hearts.Clear();
            for (int i = 0; i < max; i++)
            {
                var heart = new VisualElement();
                heart.AddToClassList("heart");
                heart.style.backgroundImage = new StyleBackground(i < current ? icons.heartFull : icons.heartEmpty);
                hearts.Add(heart);
            }
        }

        private void OnInventoryChanged(string itemId, int count)
        {
            if (!itemCounts.TryGetValue(itemId, out Label label))
            {
                var item = new VisualElement();
                item.AddToClassList("inventory-item");
                var icon = new VisualElement();
                icon.AddToClassList("inventory-icon");
                icon.style.backgroundImage = new StyleBackground(icons.ForItem(itemId));
                label = new Label();
                label.AddToClassList("inventory-count");
                item.Add(icon);
                item.Add(label);
                inventory.Add(item);
                itemCounts[itemId] = label;
            }

            label.text = count.ToString();
            UISounds.Show(label.parent, count > 0);
        }

        private void OnObjectiveChanged(string text) => objective.text = text;

        private void OnPromptChanged(string text)
        {
            UISounds.Show(prompt, !string.IsNullOrEmpty(text));
            prompt.text = PlatformFeatures.IsMobile ? text : $"[E]  {text}";
        }

        private void OnAbilityActivated(string abilityName, float cooldown)
        {
            if (!abilities.TryGetValue(abilityName, out AbilityView view))
                return;
            view.Duration = cooldown;
            view.Remaining = cooldown;
        }

        private void OnLevelStarted(int number, string displayName)
        {
            levelName.text = $"Level {number}: {displayName}";
            enemiesThisLevel = 0;
            goldThisLevel = 0;

            bool fireball = SaveManager.Instance.HasUnlock(LevelManager.UnlockFireball);
            UISounds.Show(abilities["Fireball"].Lock, !fireball);
        }

        private void OnLoadProgress(float progress)
        {
            loadingFill.style.width = Length.Percent(progress * 100f);
            loadingMessage.text = "Loading...";
            UISounds.Show(root.Q("loading-back"), false);
        }

        private void OnLoadFailed(string message)
        {
            UISounds.Show(loadingOverlay, true);
            loadingMessage.text = message;
            UISounds.Show(root.Q("loading-back"), true);
        }

        private void OnStateChanged(GameState state)
        {
            UISounds.Show(pauseOverlay, state == GameState.Paused);
            UISounds.Show(completeOverlay, state == GameState.LevelComplete);
            UISounds.Show(gameOverOverlay, state == GameState.GameOver);
            UISounds.Show(loadingOverlay, state == GameState.Loading);

            if (state == GameState.LevelComplete)
                FillCompleteScreen();
        }

        private void FillCompleteScreen()
        {
            GameManager game = GameManager.Instance;
            LevelDefinition level = game.Catalog != null ? game.Catalog.Get(game.CurrentLevel) : null;
            string reward = level != null && !string.IsNullOrEmpty(level.RewardDescription) ? level.RewardDescription : "Treasure collected.";

            root.Q<Label>("complete-reward").text = reward;
            root.Q<Label>("complete-stats").text = $"Enemies defeated {enemiesThisLevel}   ·   Gold found {goldThisLevel}";
            UISounds.Show(root.Q("next-level"), game.HasNextLevel);
        }

        private void OnConditionsChanged(string title, string description) => conditions.text = $"{title} · {description}";
        private void OnEnemyDefeated(string enemyId) => enemiesThisLevel++;

        private void OnItemCollected(string itemId, int amount)
        {
            if (itemId == Inventory.ItemIds.Gold)
                goldThisLevel += amount;
        }
    }
}
