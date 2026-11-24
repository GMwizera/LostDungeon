using LostDungeon.Core.Events;
using LostDungeon.Core.Progress;
using LostDungeon.Core.Save;
using LostDungeon.Game.Audio;
using LostDungeon.Game.Levels;
using LostDungeon.Game.Managers;
using LostDungeon.Game.Platform;
using LostDungeon.Game.Weather;
using UnityEngine;
using UnityEngine.UIElements;

namespace LostDungeon.Game.UI
{
    /// <summary>
    /// Main menu: Start, Continue, Level Select, Settings, Exit.
    /// Level tiles show their state with icons only (lock, play, check), never words.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private UIIcons icons;

        private VisualElement root;
        private VisualElement mainPanel;
        private VisualElement levelsPanel;
        private VisualElement settingsPanel;
        private VisualElement levelGrid;
        private Label conditionsLabel;
        private Label statsLabel;

        private void OnEnable()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            mainPanel = root.Q("main-panel");
            levelsPanel = root.Q("levels-panel");
            settingsPanel = root.Q("settings-panel");
            levelGrid = root.Q("level-grid");
            conditionsLabel = root.Q<Label>("conditions");
            statsLabel = root.Q<Label>("stats");

            root.Q<Button>("start").clicked += () => GameManager.Instance.StartGame();
            root.Q<Button>("continue").clicked += () => GameManager.Instance.ContinueGame();
            root.Q<Button>("levels").clicked += () => ShowPanel(levelsPanel);
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
            root.Q<Button>("continue").SetEnabled(save.HasProgress);

            PlayerStats stats = save.Data.stats;
            statsLabel.text = $"Enemies defeated {stats.enemiesDefeated}   ·   Gold {stats.goldCollected}   ·   Deaths {stats.deaths}";

            WeatherService weather = WeatherService.Instance;
            OnConditionsChanged(weather.Conditions.Title, $"{weather.Summary}. {weather.Conditions.Description}");

            BuildLevelTiles();
        }

        private void BuildLevelTiles()
        {
            levelGrid.Clear();
            LevelCatalog catalog = GameManager.Instance.Catalog;
            LevelProgress progress = SaveManager.Instance.Progress;
            if (catalog == null)
                return;

            foreach (LevelDefinition level in catalog.Levels)
            {
                LevelStatus status = progress.GetStatus(level.Number);
                int number = level.Number;

                var tile = new Button(() => GameManager.Instance.PlayLevel(number));
                tile.AddToClassList("level-tile");
                tile.AddToClassList($"level-tile--{status.ToString().ToLowerInvariant()}");
                tile.SetEnabled(status != LevelStatus.Locked);
                tile.tooltip = level.DisplayName;

                var numberLabel = new Label(number.ToString());
                numberLabel.AddToClassList("level-number");
                tile.Add(numberLabel);

                var badge = new VisualElement();
                badge.AddToClassList("level-badge");
                badge.style.backgroundImage = new StyleBackground(IconFor(status));
                tile.Add(badge);

                var name = new Label(level.DisplayName);
                name.AddToClassList("level-name");
                tile.Add(name);

                levelGrid.Add(tile);
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
                BuildLevelTiles();
        }

        private void OnConditionsChanged(string title, string description) =>
            conditionsLabel.text = $"Dungeon today: {title}  ({description})";
    }
}
