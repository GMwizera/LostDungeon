using LostDungeon.Game.Audio;
using UnityEngine.UIElements;

namespace LostDungeon.Game.UI
{
    /// <summary>Adds a click sound to every button under a UI root.</summary>
    public static class UISounds
    {
        public static void AddClickSounds(VisualElement root)
        {
            root.Query<Button>().ForEach(button =>
                button.RegisterCallback<ClickEvent>(_ =>
                {
                    if (AudioManager.HasInstance)
                        AudioManager.Instance.Play(AudioManager.UIClick);
                }));
        }

        public static void Show(VisualElement element, bool visible) =>
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
