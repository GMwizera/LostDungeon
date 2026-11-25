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
    /// The main menu: Kigali weather, hero info and the five levels.
    /// Level cards use icons and colour, not words, to show their state.
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

            UISounds.Show(root.Q("exit"), PlatformFeatures.CanQuit); // set by conditional compilation
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
            continueButton.text = "CONTINUE";
            var continueDetail = root.Q<Label>("continue-detail");
            continueDetail.text = save.HasProgress
                ? (progress.AllCompleted ? "All five chambers cleared" : $"Level {next} · {nextLevel?.DisplayName}")
                : "No expedition yet";
            root.Q<Button>("levels-enter").text = $"ENTER {nextName}  >";

            root.Q<Label>("cleared-chip").text = $"{progress.CompletedLevels.Count}/{progress.LevelCount} CLEARED";

            FillHeroStatus(save);
            FillWeather();
            BuildLevelCards(catalog, progress);
        }

        private void FillHeroStatus(SaveManager save)
        {
            bool bonus = save.HasUnlock(LevelManager.UnlockHealthBonus);
            int hearts = BaseHearts + (bonus ? LevelManager.HealthBonus : 0);

            VisualElement heartRow = root.Q("hub-hearts");
            heartRow.Clear();
            for (int i = 0; i < hearts; i++)
            {
                var heart = new VisualElement();
                heart.AddToClassList("hub-heart");
                heart.style.backgroundImage = new StyleBackground(icons.heartFull);
                heartRow.Add(heart);
            }


            int gold = 0;
            foreach (InventoryEntry entry in save.Data.inventory)
                if (entry.id == ItemIds.Gold)
                    gold = entry.count;
            root.Q<Label>("pouch-gold").text = gold.ToString();
        }

        private void FillWeather()
        {
            WeatherService weather = WeatherService.Instance;
            root.Q<Label>("weather-temp").text = weather.Temperature.HasValue ? $"{weather.Temperature.Value:0}°C" : "--°C";
            root.Q<Label>("weather-name").text = weather.WeatherName + (weather.IsNight ? " · Night" : "");
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
