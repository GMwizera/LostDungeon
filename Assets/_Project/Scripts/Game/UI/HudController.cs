using System.Collections.Generic;
using LostDungeon.Core.Events;
using LostDungeon.Game.Inventory;
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
    /// The in-game HUD plus the pause, win, lose and loading screens.
    /// It only listens to events, so gameplay code never knows about the UI.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class HudController : MonoBehaviour
    {
        private class AbilityView
        {
            public VisualElement Root;
            public CooldownRing Ring;
            public Label Time;
            public VisualElement Lock;
            public float Duration;
            public float Remaining;
        }

        [SerializeField] private UIIcons icons;
        [SerializeField] private PlayerInputReader playerInput;

        private readonly Dictionary<string, AbilityView> abilities = new Dictionary<string, AbilityView>();
        private readonly Dictionary<string, Label> itemCounts = new Dictionary<string, Label>();
        private VisualElement root;
        private VisualElement inventory;
        private VisualElement vitalityFill;
        private Label vitalityText;
        private Label goldCount;
        private VisualElement foeDots;
        private Label roomCount;
        private VisualElement objectiveBanner;
        private Label objective;
        private Label levelName;
        private Label prompt;
        private Label conditions;
        private Label weatherTitle;
        private VisualElement pauseOverlay;
        private VisualElement completeOverlay;
        private VisualElement gameOverOverlay;
        private VisualElement loadingOverlay;
        private VisualElement loadingFill;
        private Label loadingMessage;
        private int enemiesThisLevel;
        private int goldThisLevel;
        private int potionsThisLevel;
        private bool levelNeedsKey;

        private void OnEnable()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            inventory = root.Q("inventory");
            vitalityFill = root.Q("vitality-fill");
            vitalityText = root.Q<Label>("vitality-text");
            goldCount = root.Q<Label>("gold-count");
            foeDots = root.Q("foe-dots");
            roomCount = root.Q<Label>("room-count");
            objectiveBanner = root.Q("objective-banner");
            objective = root.Q<Label>("objective");
            levelName = root.Q<Label>("level-name");
            prompt = root.Q<Label>("prompt");
            conditions = root.Q<Label>("conditions");
            weatherTitle = root.Q<Label>("weather-title");
            pauseOverlay = root.Q("pause-overlay");
            completeOverlay = root.Q("complete-overlay");
            gameOverOverlay = root.Q("gameover-overlay");
            loadingOverlay = root.Q("loading-overlay");
            loadingFill = root.Q("loading-fill");
            loadingMessage = root.Q<Label>("loading-message");

            SetIcon("key-icon", icons.key);
            SetIcon("objective-icon", icons.flag);
            SetIcon("weather-icon", icons.cloud);
            SetIcon("gold-icon", icons.gold);
            SetIcon("orb-strike-icon", icons.sword);
            SetIcon("orb-wind-icon", icons.dash);
            SetIcon("orb-fire-icon", icons.fireball);

            CreateAbilitySlots();
            BindButtons();
            SetUpTouchControls();
            UISounds.AddClickSounds(root);

            GameEvents.PlayerHealthChanged += OnHealthChanged;
            GameEvents.InventoryChanged += OnInventoryChanged;
            GameEvents.ObjectiveChanged += OnObjectiveChanged;
            GameEvents.ObjectiveStatusChanged += OnObjectiveStatusChanged;
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
            OnConditionsChanged(null, null);
            UISounds.Show(root.Q("key-box"), false);
            UISounds.Show(root.Q("hearts-bonus"), false);
        }

        private void OnDisable()
        {
            GameEvents.PlayerHealthChanged -= OnHealthChanged;
            GameEvents.InventoryChanged -= OnInventoryChanged;
            GameEvents.ObjectiveChanged -= OnObjectiveChanged;
            GameEvents.ObjectiveStatusChanged -= OnObjectiveStatusChanged;
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
            // Count down the rings here, so the HUD doesn't need the player.
            foreach (AbilityView view in abilities.Values)
            {
                if (view.Remaining <= 0f)
                    continue;
                view.Remaining = Mathf.Max(0f, view.Remaining - Time.deltaTime);
                view.Ring.Remaining = view.Duration > 0f ? view.Remaining / view.Duration : 0f;
                view.Time.text = view.Remaining > 0.05f ? $"{view.Remaining:0.0}s" : "";
            }
        }

        // ---------- Setup ----------

        private void SetIcon(string name, Sprite sprite)
        {
            VisualElement element = root.Q(name);
            if (element != null && sprite != null)
                element.style.backgroundImage = new StyleBackground(sprite);
        }

        /// <summary>Ability buttons with rings. Tap or click one to use it.</summary>
        private void CreateAbilitySlots()
        {
            VisualElement bar = root.Q("abilities");
            bar.Clear();
            abilities.Clear();
            AddAbility(bar, "Sword", "STRIKE", "strike", icons.sword, "LMB", new Color(1f, 0.82f, 0.35f), 6f, playerInput != null ? playerInput.PressAttack : null);
            AddAbility(bar, "Dash", "DASH", "dash", icons.dash, "SPACE", new Color(0.4f, 0.9f, 1f), 4f, playerInput != null ? playerInput.PressDash : null);
            AddAbility(bar, "Fireball", "FIRE", "fire", icons.fireball, "Q", new Color(1f, 0.5f, 0.2f), 4f, playerInput != null ? playerInput.PressFireball : null);
            AddAbility(bar, "Use", "USE", "use", icons.key, "E", new Color(0.3f, 0.9f, 0.6f), 0f, playerInput != null ? playerInput.PressInteract : null);
        }

        private void AddAbility(VisualElement bar, string abilityName, string label, string style, Sprite icon, string keyHint,
            Color ringColor, float ringWidth, System.Action press)
        {
            var button = new VisualElement();
            button.AddToClassList("fe-ability");
            button.AddToClassList($"fe-ability--{style}");

            var ring = new CooldownRing();
            ring.Configure(ringColor, ringWidth);
            if (ringWidth > 0f)
                button.Add(ring);

            var image = new VisualElement { pickingMode = PickingMode.Ignore };
            image.AddToClassList("fe-ability-icon");
            image.style.backgroundImage = new StyleBackground(icon);
            button.Add(image);

            var name = new Label(label) { pickingMode = PickingMode.Ignore };
            name.AddToClassList("fe-ability-name");
            button.Add(name);

            var time = new Label("") { pickingMode = PickingMode.Ignore };
            time.AddToClassList("fe-ability-time");
            button.Add(time);

            if (!PlatformFeatures.IsMobile)
            {
                var key = new Label(keyHint) { pickingMode = PickingMode.Ignore };
                key.AddToClassList("fe-ability-key");
                button.Add(key);
            }

            var lockIcon = new VisualElement { pickingMode = PickingMode.Ignore };
            lockIcon.AddToClassList("fe-ability-lock");
            lockIcon.style.backgroundImage = new StyleBackground(icons.locked);
            UISounds.Show(lockIcon, false);
            button.Add(lockIcon);

            if (press != null)
                button.RegisterCallback<PointerDownEvent>(_ => press());

            bar.Add(button);
            abilities[abilityName] = new AbilityView { Root = button, Ring = ring, Time = time, Lock = lockIcon };
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
        /// Touch joystick for phones and touch screens, unless turned off in Settings.
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
                playerInput.SetVirtualMove(new Vector2(offset.x, -offset.y) / radius); // UI y goes down, so flip it
            }

        }

        // ---------- Event handlers ----------

        private void OnHealthChanged(int current, int max)
        {
            float fraction = max > 0 ? (float)current / max : 0f;
            vitalityFill.style.width = Length.Percent(fraction * 100f);
            vitalityFill.style.backgroundColor = fraction > 0.34f ? new Color(0.91f, 0.71f, 0.24f) : new Color(0.94f, 0.27f, 0.27f);
            vitalityText.text = $"{current}/{max}  {Mathf.RoundToInt(fraction * 100f)}%";
        }

        /// <summary>Updates the gold count and one slot per item.</summary>
        private void OnInventoryChanged(string itemId, int count)
        {
            if (itemId == ItemIds.Gold)
                goldCount.text = count.ToString();
            SetPouchSlot(itemId, count);
        }

        private void SetPouchSlot(string itemId, int count)
        {
            if (!itemCounts.TryGetValue(itemId, out Label label))
            {
                var slot = new VisualElement { pickingMode = PickingMode.Ignore };
                slot.AddToClassList("fe-slot");
                if (itemId == ItemIds.Key) slot.AddToClassList("fe-slot--key");
                if (itemId == ItemIds.Potion) slot.AddToClassList("fe-slot--potion");
                var icon = new VisualElement { pickingMode = PickingMode.Ignore };
                icon.AddToClassList("fe-slot-icon");
                icon.style.backgroundImage = new StyleBackground(icons.ForItem(itemId));
                label = new Label { pickingMode = PickingMode.Ignore };
                label.AddToClassList("fe-slot-count");
                slot.Add(icon);
                slot.Add(label);
                inventory.Add(slot);
                itemCounts[itemId] = label;
            }

            label.text = count.ToString();
            UISounds.Show(label.parent, count > 0);
        }

        private void OnObjectiveChanged(string text) => objective.text = text?.ToUpperInvariant();

        /// <summary>Shows the key, enemies left, and a green banner when the exit opens.</summary>
        private void OnObjectiveStatusChanged(int defeated, int total, bool requiresKey, bool hasKey, bool exitOpen)
        {
            levelNeedsKey = requiresKey;
            VisualElement keyOrb = root.Q("key-box");
            UISounds.Show(keyOrb, requiresKey);
            keyOrb.EnableInClassList("fe-orb--off", !hasKey);
            objectiveBanner.EnableInClassList("fe-banner--open", exitOpen);
            SetIcon("objective-icon", exitOpen ? icons.completed : icons.flag);

            int remaining = Mathf.Max(0, total - defeated);
            root.Q<Label>("foes").text = remaining == 0 ? "ALL DEFEATED" : $"{remaining} REMAIN:";
            foeDots.Clear();
            for (int i = 0; i < remaining; i++)
            {
                var dot = new VisualElement { pickingMode = PickingMode.Ignore };
                dot.AddToClassList("fe-dot");
                foeDots.Add(dot);
            }
        }

        private void OnPromptChanged(string text)
        {
            UISounds.Show(prompt, !string.IsNullOrEmpty(text));
            prompt.text = PlatformFeatures.IsMobile ? text?.ToUpperInvariant() : $"[E]  {text?.ToUpperInvariant()}";
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
            levelName.text = displayName.ToUpperInvariant();
            GameManager game = GameManager.Instance;
            roomCount.text = $"ROOM {number} / {(game.Catalog != null ? game.Catalog.Count : 5)}";
            SetPouchSlot(ItemIds.Potion, 0); // potions restart each level
            enemiesThisLevel = 0;
            goldThisLevel = 0;
            potionsThisLevel = 0;

            SaveManager save = SaveManager.Instance;
            bool fireball = save.HasUnlock(LevelManager.UnlockFireball);
            UISounds.Show(abilities["Fireball"].Lock, !fireball);
            abilities["Fireball"].Root.EnableInClassList("fe-ability--locked", !fireball);
            root.Q("orb-fire").EnableInClassList("fe-orb--off", !fireball);
            UISounds.Show(root.Q("hearts-bonus"), save.HasUnlock(LevelManager.UnlockHealthBonus));
        }

        private void OnLoadProgress(float progress)
        {
            loadingFill.style.width = Length.Percent(progress * 100f);
            root.Q<Label>("loading-title").text = $"ENTERING ROOM {GameManager.Instance.CurrentLevel}";
            loadingMessage.text = "Loading chamber from Addressables...";
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

            // Hide the game HUD when a menu is open.
            bool playing = state == GameState.Playing;
            foreach (string name in new[] { "fe-top", "fe-banner", "prompt", "fe-abilities", "fe-joystick-area", "world-layer" })
                foreach (VisualElement element in root.Query(className: name).ToList())
                    element.style.visibility = playing ? Visibility.Visible : Visibility.Hidden;
            VisualElement world = root.Q("world-layer");
            if (world != null)
                world.style.visibility = playing ? Visibility.Visible : Visibility.Hidden;

            if (state == GameState.LevelComplete)
                FillDebrief();
            if (state == GameState.GameOver)
                root.Q<Label>("gameover-text").text = $"You fell in Room {GameManager.Instance.CurrentLevel}. The dungeon keeps your progress; try again.";
        }

        /// <summary>Fills the level complete screen with what you got.</summary>
        private void FillDebrief()
        {
            GameManager game = GameManager.Instance;
            int number = game.CurrentLevel;
            LevelDefinition level = game.Catalog != null ? game.Catalog.Get(number) : null;
            int levelCount = game.Catalog != null ? game.Catalog.Count : 5;

            root.Q<Label>("complete-title").text = game.HasNextLevel ? $"ROOM {number} CLEARED" : "YOU ESCAPED THE DUNGEON";

            var reward = root.Q<Label>("complete-reward");
            bool hasReward = level != null && !string.IsNullOrEmpty(level.RewardTag);
            reward.text = hasReward ? level.RewardTag.ToUpperInvariant() : "";
            UISounds.Show(reward, hasReward);

            VisualElement spoils = root.Q("spoils");
            spoils.Clear();
            spoils.Add(Spoil(icons.gold, $"+{goldThisLevel}"));
            if (potionsThisLevel > 0)
                spoils.Add(Spoil(icons.potion, $"{potionsThisLevel}"));
            if (levelNeedsKey)
                spoils.Add(Spoil(icons.key, "1"));
            spoils.Add(Spoil(icons.sword, $"{enemiesThisLevel}"));

            BuildTrack(number, levelCount);
            int cleared = SaveManager.Instance.Progress.CompletedLevels.Count;
            root.Q<Label>("progress-pct").text = $"{cleared} OF {levelCount} ROOMS CLEARED";

            LevelDefinition next = game.Catalog != null ? game.Catalog.Get(number + 1) : null;
            var nextButton = root.Q<Button>("next-level");
            UISounds.Show(nextButton, game.HasNextLevel);
            if (next != null)
                nextButton.text = $"NEXT: {next.DisplayName.ToUpperInvariant()}  >";
        }

        private void BuildTrack(int current, int count)
        {
            VisualElement track = root.Q("track");
            track.Clear();
            for (int i = 1; i <= count; i++)
            {
                if (i > 1)
                {
                    var line = new VisualElement();
                    line.AddToClassList("track-line");
                    if (i <= current)
                        line.AddToClassList("track-line--done");
                    track.Add(line);
                }

                var node = new VisualElement();
                node.AddToClassList("track-node");
                if (i == current)
                    node.AddToClassList("track-node--current");
                Sprite icon = i < current ? icons.completed : i == current ? icons.available : icons.locked;
                node.style.backgroundImage = new StyleBackground(icon);
                track.Add(node);
            }
        }

        /// <summary>A small box with an icon and a number.</summary>
        private static VisualElement Spoil(Sprite icon, string amount)
        {
            var chip = new VisualElement();
            chip.AddToClassList("spoil-chip");
            var image = new VisualElement();
            image.AddToClassList("spoil-chip-icon");
            image.style.backgroundImage = new StyleBackground(icon);
            var label = new Label(amount);
            label.AddToClassList("spoil-chip-text");
            chip.Add(image);
            chip.Add(label);
            return chip;
        }

        private void OnConditionsChanged(string title, string description)
        {
            WeatherService weather = WeatherService.Instance;
            weatherTitle.text = weather.HasLiveData
                ? $"KIGALI {weather.WeatherName.ToUpperInvariant()}{(weather.IsNight ? " · NIGHT" : "")} · LIVE"
                : "KIGALI WEATHER · OFFLINE";
            conditions.text = weather.Conditions.Description;
        }

        private void OnEnemyDefeated(string enemyId) => enemiesThisLevel++;

        private void OnItemCollected(string itemId, int amount)
        {
            if (itemId == ItemIds.Gold)
                goldThisLevel += amount;
            else if (itemId == ItemIds.Potion)
            {
                potionsThisLevel += amount;
                SetPouchSlot(ItemIds.Potion, potionsThisLevel);
            }
        }
    }
}
