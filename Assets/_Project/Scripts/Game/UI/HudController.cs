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
    /// In-game HUD and overlays (pause, Expedition Debrief, defeat, loading), styled after the Stitch designs.
    /// It only listens to GameEvents and calls GameManager for button actions, so gameplay code never
    /// references the UI: the HUD could be replaced or removed without touching combat or levels.
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
            hearts = root.Q("hearts");
            inventory = root.Q("inventory");
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

        private void SetIcon(string name, Sprite sprite)
        {
            VisualElement element = root.Q(name);
            if (element != null && sprite != null)
                element.style.backgroundImage = new StyleBackground(sprite);
        }

        private void CreateAbilitySlots()
        {
            VisualElement bar = root.Q("abilities");
            bar.Clear();
            abilities.Clear();
            AddAbility(bar, "Sword", "STRIKE", "sword", icons.sword, "LMB");
            AddAbility(bar, "Dash", "DASH", "dash", icons.dash, "SPACE");
            AddAbility(bar, "Fireball", "FIRE", "fireball", icons.fireball, "Q");
        }

        private void AddAbility(VisualElement bar, string abilityName, string label, string style, Sprite icon, string keyHint)
        {
            var slot = new VisualElement();
            slot.AddToClassList("ability-slot");
            slot.AddToClassList($"ability-slot--{style}");

            var image = new VisualElement();
            image.AddToClassList("ability-icon");
            image.style.backgroundImage = new StyleBackground(icon);
            slot.Add(image);

            var name = new Label(label);
            name.AddToClassList("ability-name");
            slot.Add(name);

            var fill = new VisualElement();
            fill.AddToClassList("ability-cooldown");
            slot.Add(fill);

            if (!PlatformFeatures.IsMobile)
            {
                var key = new Label($"[{keyHint}]");
                key.AddToClassList("ability-key");
                slot.Add(key);
            }

            var lockIcon = new VisualElement();
            lockIcon.AddToClassList("ability-lock");
            lockIcon.style.backgroundImage = new StyleBackground(icons.locked);
            UISounds.Show(lockIcon, false);
            slot.Add(lockIcon);

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

            SetIcon("touch-attack-icon", icons.sword);
            SetIcon("touch-dash-icon", icons.dash);
            SetIcon("touch-fireball-icon", icons.fireball);
            SetIcon("touch-interact-icon", icons.key);

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
            if (itemId == ItemIds.Key)
                return; // the key has its own chip

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

        /// <summary>Key counter, gate chip and foe counter, like the Stitch combat HUD.</summary>
        private void OnObjectiveStatusChanged(int defeated, int total, bool requiresKey, bool hasKey, bool exitOpen)
        {
            levelNeedsKey = requiresKey;
            UISounds.Show(root.Q("key-box"), requiresKey);
            root.Q<Label>("key-label").text = hasKey ? "1/1 KEY" : "0/1 KEY";

            var gateLabel = root.Q<Label>("gate-label");
            gateLabel.text = exitOpen ? "EXIT OPEN" : "GATE SEALED";
            gateLabel.style.color = exitOpen ? new Color(0.06f, 0.73f, 0.51f) : new Color(0.94f, 0.27f, 0.27f);
            SetIcon("gate-icon", exitOpen ? icons.completed : icons.locked);

            int remaining = Mathf.Max(0, total - defeated);
            var foes = root.Q<Label>("foes");
            foes.text = remaining == 1 ? "1 FOE" : $"{remaining} FOES";
            UISounds.Show(foes, remaining > 0);
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
            levelName.text = $"ROOM {number} · {displayName.ToUpperInvariant()}";
            enemiesThisLevel = 0;
            goldThisLevel = 0;
            potionsThisLevel = 0;

            SaveManager save = SaveManager.Instance;
            UISounds.Show(abilities["Fireball"].Lock, !save.HasUnlock(LevelManager.UnlockFireball));
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

            if (state == GameState.LevelComplete)
                FillDebrief();
            if (state == GameState.GameOver)
                root.Q<Label>("gameover-text").text = $"You fell in Room {GameManager.Instance.CurrentLevel}. The dungeon keeps your progress; try again.";
        }

        /// <summary>The Expedition Debrief screen (level complete), after the Stitch design.</summary>
        private void FillDebrief()
        {
            GameManager game = GameManager.Instance;
            int number = game.CurrentLevel;
            LevelDefinition level = game.Catalog != null ? game.Catalog.Get(number) : null;
            int levelCount = game.Catalog != null ? game.Catalog.Count : 5;

            root.Q<Label>("sector-chip").text = $"SECTOR {number:00} TELEMETRY";
            root.Q<Label>("complete-title").text = game.HasNextLevel
                ? $"ROOM {number} CLEARED · EXIT UNSEALED"
                : "THE LOST DUNGEON IS BEHIND YOU";
            root.Q<Label>("complete-summary").text = levelNeedsKey
                ? $"{enemiesThisLevel} hostiles defeated · Golden key recovered · {level?.DisplayName} escaped"
                : $"{enemiesThisLevel} hostiles defeated · {level?.DisplayName} escaped";

            int percent = Mathf.RoundToInt(SaveManager.Instance.Progress.CompletedLevels.Count * 100f / levelCount);
            root.Q<Label>("progress-pct").text = $"{percent}% COMPLETE";
            BuildTrack(number, levelCount);

            WeatherService weather = WeatherService.Instance;
            root.Q<Label>("debrief-temp").text = weather.Temperature.HasValue ? $"KIGALI {weather.Temperature.Value:0}°C" : "KIGALI OFFLINE";
            root.Q<Label>("debrief-weather").text = $"Weather condition: {weather.WeatherName}. {weather.Conditions.Description}";

            VisualElement spoils = root.Q("spoils");
            spoils.Clear();
            spoils.Add(Spoil(icons.gold, $"+{goldThisLevel} GOLD", "Coins from the floor and the chest"));
            spoils.Add(Spoil(icons.potion, $"{potionsThisLevel}x HEALING POTION", "Drunk on the spot to restore hearts"));
            if (levelNeedsKey)
                spoils.Add(Spoil(icons.key, "1x DUNGEON GATE KEY", "Used to unseal the exit door"));

            var reward = root.Q<Label>("complete-reward");
            bool hasReward = level != null && !string.IsNullOrEmpty(level.RewardDescription);
            reward.text = hasReward ? level.RewardDescription.ToUpperInvariant() : "";
            UISounds.Show(reward, hasReward);
            root.Q<Label>("complete-stats").text = $"ENEMIES DEFEATED {enemiesThisLevel}   ·   GOLD FOUND {goldThisLevel}";

            LevelDefinition next = game.Catalog != null ? game.Catalog.Get(number + 1) : null;
            var nextButton = root.Q<Button>("next-level");
            UISounds.Show(nextButton, game.HasNextLevel);
            if (next != null)
                nextButton.text = $"DESCENT TO LEVEL {next.Number}: {next.DisplayName.ToUpperInvariant()}  >";
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

        private static VisualElement Spoil(Sprite icon, string name, string description)
        {
            var row = new VisualElement();
            row.AddToClassList("spoil");
            var image = new VisualElement();
            image.AddToClassList("spoil-icon");
            image.style.backgroundImage = new StyleBackground(icon);
            row.Add(image);

            var text = new VisualElement();
            var title = new Label(name);
            title.AddToClassList("spoil-name");
            var body = new Label(description);
            body.AddToClassList("body");
            body.AddToClassList("muted");
            text.Add(title);
            text.Add(body);
            row.Add(text);
            return row;
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
                potionsThisLevel += amount;
        }
    }
}
