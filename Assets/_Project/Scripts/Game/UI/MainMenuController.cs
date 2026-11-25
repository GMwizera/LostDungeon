using LostDungeon.Core.Events;
using LostDungeon.Core.Progress;
using LostDungeon.Core.Save;
using LostDungeon.Game.Audio;
using LostDungeon.Game.Inventory;
using LostDungeon.Game.Levels;
using LostDungeon.Game.Managers;
using LostDungeon.Game.Platform;
using LostDungeon.Game.Weather;
using UnityEngine;
using UnityEngine.UIElements;

namespace LostDungeon.Game.UI
{
    /// <summary>
    /// Main menu as the "Dungeon Hub" from the Stitch design: live Kigali weather, hero status,
    /// and the five chambers. Buttons: Start Game, Continue, Level Select, Settings, Exit.
    /// Chamber cards show their state with icons and colour only (lock, play, check), never words.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class MainMenuController : MonoBehaviour
    {
        private const int BaseHearts = 6;

        [SerializeField] private UIIcons icons;

        private VisualElement root;
        private VisualElement mainPanel;
        private VisualElement levelsPanel;
        private VisualElement settingsPanel;

        private void OnEnable()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            mainPanel = root.Q("main-panel");
            levelsPanel = root.Q("levels-panel");
            settingsPanel = root.Q("settings-panel");

            root.Q<Button>("start").clicked += () => GameManager.Instance.StartGame();
            root.Q<Button>("continue").clicked += () => GameManager.Instance.ContinueGame();
            root.Q<Button>("levels").clicked += () => ShowPanel(levelsPanel);
            root.Q<Button>("levels-enter").clicked += () => GameManager.Instance.ContinueGame();
            root.Q<Button>("settings").clicked += () => ShowPanel(settingsPanel);
            root.Q<Button>("exit").clicked += () => GameManager.Instance.QuitGame();
            root.Q<Button>("levels-back").clicked += () => ShowPanel(mainPanel);
            root.Q<Button>("settings-back").clicked += () =>
            {
                SaveManager.Instance.Save();
                ShowPanel(mainPanel);
            };
            root.Q<Button>("reset").clicked += () =>
            {
                SaveManager.Instance.ResetProgress();
                Refresh();
            };

            UISounds.Show(root.Q("exit"), PlatformFeatures.CanQuit); // conditional compilation decides this
            root.Q("weather-icon").style.backgroundImage = new StyleBackground(icons.cloud);
            root.Q("pouch-icon").style.backgroundImage = new StyleBackground(icons.gold);

            BindSettings();
            UISounds.AddClickSounds(root);

            GameEvents.ConditionsChanged += OnConditionsChanged;
            GameManager.Instance.SetState(GameState.MainMenu);
            ShowPanel(mainPanel);
            Refresh();
        }

        private void OnDisable() => GameEvents.ConditionsChanged -= OnConditionsChanged;

        private void Refresh()
        {
            SaveManager save = SaveManager.Instance;
            LevelProgress progress = save.Progress;
            LevelCatalog catalog = GameManager.Instance.Catalog;
            int next = progress.NextLevelToPlay();
            LevelDefinition nextLevel = catalog != null ? catalog.Get(next) : null;
            string nextName = nextLevel != null ? $"LEVEL {next}: {nextLevel.DisplayName.ToUpperInvariant()}" : $"LEVEL {next}";

            var continueButton = root.Q<Button>("continue");
            continueButton.SetEnabled(save.HasProgress);
            continueButton.text = save.HasProgress ? $"CONTINUE  ·  {nextName}" : "CONTINUE";
            root.Q<Button>("levels-enter").text = $"ENTER {nextName}  >";

            root.Q<Label>("floor-chip").text = progress.AllCompleted ? "ALL CLEARED" : $"FLOOR {next:00} READY";
            root.Q<Label>("cleared-chip").text = $"{progress.CompletedLevels.Count}/{progress.LevelCount} CLEARED";

            PlayerStats stats = save.Data.stats;
            root.Q<Label>("stats").text =
                $"ENEMIES DEFEATED {stats.enemiesDefeated}   ·   GOLD FOUND {stats.goldCollected}   ·   DEATHS {stats.deaths}";

            FillHeroStatus(save);
            FillWeather();
            BuildLevelCards(catalog, progress);
        }

        private void FillHeroStatus(SaveManager save)
        {
            bool bonus = save.HasUnlock(LevelManager.UnlockHealthBonus);
            int hearts = BaseHearts + (bonus ? LevelManager.HealthBonus : 0);
            root.Q<Label>("vitality").text = bonus
                ? $"VITALITY {hearts}/{hearts} HEARTS ({BaseHearts} BASE + {LevelManager.HealthBonus} CRYPT BONUS)"
                : $"VITALITY {hearts}/{hearts} HEARTS";

            VisualElement heartRow = root.Q("hub-hearts");
            heartRow.Clear();
            for (int i = 0; i < hearts; i++)
            {
                var heart = new VisualElement();
                heart.AddToClassList("hub-heart");
                heart.style.backgroundImage = new StyleBackground(icons.heartFull);
                heartRow.Add(heart);
            }

            VisualElement arsenal = root.Q("arsenal");
            arsenal.Clear();
            arsenal.Add(Slot(icons.sword, true));
            arsenal.Add(Slot(icons.dash, true));
            arsenal.Add(Slot(icons.fireball, save.HasUnlock(LevelManager.UnlockFireball)));

            int gold = 0;
            foreach (InventoryEntry entry in save.Data.inventory)
                if (entry.id == ItemIds.Gold)
                    gold = entry.count;
            root.Q<Label>("pouch-gold").text = gold.ToString();
        }

        private static VisualElement Slot(Sprite icon, bool unlocked)
        {
            var slot = new VisualElement();
            slot.AddToClassList("slot");
            if (!unlocked)
                slot.AddToClassList("slot--locked");
            var image = new VisualElement();
            image.AddToClassList("icon");
            image.style.backgroundImage = new StyleBackground(icon);
            slot.Add(image);
            return slot;
        }

        private void FillWeather()
        {
            WeatherService weather = WeatherService.Instance;
            root.Q<Label>("weather-temp").text = weather.Temperature.HasValue ? $"{weather.Temperature.Value:0}°C" : "--°C";
            root.Q<Label>("weather-name").text = weather.WeatherName + (weather.IsNight ? " · Night" : "");
            root.Q<Label>("effect-title").text = weather.Conditions.Title.ToUpperInvariant();
            root.Q<Label>("effect-text").text = weather.Conditions.Description;

            var chip = root.Q<Label>("api-chip");
            chip.RemoveFromClassList("chip--cyan");
            chip.RemoveFromClassList("chip--red");
            if (weather.IsFetching)
            {
                chip.text = "CONNECTING";
            }
            else if (weather.HasLiveData)
            {
                chip.text = "ONLINE API";
                chip.AddToClassList("chip--cyan");
            }
            else
            {
                chip.text = "OFFLINE";
                chip.AddToClassList("chip--red");
            }
        }

        private void BuildLevelCards(LevelCatalog catalog, LevelProgress progress)
        {
            VisualElement list = root.Q("level-grid");
            list.Clear();
            if (catalog == null)
                return;

            foreach (LevelDefinition level in catalog.Levels)
            {
                LevelStatus status = progress.GetStatus(level.Number);
                int number = level.Number;

                var card = new Button(() => GameManager.Instance.PlayLevel(number));
                card.AddToClassList("room-card");
                card.AddToClassList($"room-card--{status.ToString().ToLowerInvariant()}");
                card.SetEnabled(status != LevelStatus.Locked);

                var badge = new VisualElement();
                badge.AddToClassList("room-status");
                badge.style.backgroundImage = new StyleBackground(IconFor(status));
                card.Add(badge);

                var text = new VisualElement();
                var titleRow = new VisualElement();
                titleRow.AddToClassList("row");
                var numberLabel = new Label($"{number:00}");
                numberLabel.AddToClassList("room-number");
                var name = new Label(level.DisplayName);
                name.AddToClassList("room-name");
                titleRow.Add(numberLabel);
                titleRow.Add(name);
                var hostiles = new Label($"Hostiles: {level.Hostiles}");
                hostiles.AddToClassList("room-hostiles");
                text.Add(titleRow);
                text.Add(hostiles);
                card.Add(text);

                var spacer = new VisualElement();
                spacer.AddToClassList("spacer");
                card.Add(spacer);

                if (!string.IsNullOrEmpty(level.RewardTag))
                {
                    var reward = new Label(level.RewardTag);
                    reward.AddToClassList("chip");
                    reward.AddToClassList(status == LevelStatus.Completed ? "chip--jade" : "chip--gold");
                    card.Add(reward);
                }

                list.Add(card);
            }
        }

        private Sprite IconFor(LevelStatus status)
        {
            switch (status)
            {
                case LevelStatus.Completed: return icons.completed;
                case LevelStatus.Available: return icons.available;
                default: return icons.locked;
            }
        }

        private void BindSettings()
        {
            GameSettings settings = SaveManager.Instance.Data.settings;
            ApplyAudio(settings);

            var music = root.Q<Slider>("music");
            var effects = root.Q<Slider>("effects");
            var touch = root.Q<Toggle>("touch");

            music.SetValueWithoutNotify(settings.musicVolume);
            effects.SetValueWithoutNotify(settings.effectsVolume);
            touch.SetValueWithoutNotify(settings.showTouchControls);

            music.RegisterValueChangedCallback(e => { settings.musicVolume = e.newValue; ApplyAudio(settings); });
            effects.RegisterValueChangedCallback(e => { settings.effectsVolume = e.newValue; ApplyAudio(settings); });
            touch.RegisterValueChangedCallback(e => settings.showTouchControls = e.newValue);
        }

        private static void ApplyAudio(GameSettings settings)
        {
            AudioManager.Instance.MusicVolume = settings.musicVolume;
            AudioManager.Instance.EffectsVolume = settings.effectsVolume;
        }

        private void ShowPanel(VisualElement panel)
        {
            UISounds.Show(mainPanel, panel == mainPanel);
            UISounds.Show(levelsPanel, panel == levelsPanel);
            UISounds.Show(settingsPanel, panel == settingsPanel);
            if (panel == levelsPanel)
                Refresh();
        }

        private void OnConditionsChanged(string title, string description) => FillWeather();
    }
}
