using System.Collections.Generic;
using LostDungeon.Game.Combat;
using LostDungeon.Game.Enemies;
using UnityEngine;
using UnityEngine.UIElements;

namespace LostDungeon.Game.UI
{
    /// <summary>
    /// Draws UI that follows things in the 3D world: a name plate with a health bar over every enemy,
    /// and floating damage numbers ("-2", "-4 CRIT!") where hits land. Uses the HUD's UI Toolkit panel,
    /// converting world positions to panel positions each frame.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class CombatOverlay : MonoBehaviour
    {
        private const float NumberSeconds = 0.9f;
        private const float RescanSeconds = 0.3f;

        private class Plate
        {
            public VisualElement Root;
            public Label Name;
            public VisualElement Fill;
        }

        private class FloatingNumber
        {
            public Label Label;
            public Vector3 World;
            public float Age;
        }

        private readonly Dictionary<EnemyController, Plate> plates = new Dictionary<EnemyController, Plate>();
        private readonly List<EnemyController> stale = new List<EnemyController>();
        private readonly List<FloatingNumber> numbers = new List<FloatingNumber>();
        private VisualElement layer;
        private VisualElement hurtFlash;
        private float hurtFlashLeft;
        private Camera view;
        private float nextScan;

        private void OnEnable()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            layer = new VisualElement { name = "world-layer", pickingMode = PickingMode.Ignore };
            layer.AddToClassList("world-layer");
            root.Insert(0, layer); // under every HUD panel and menu
            hurtFlash = new VisualElement { pickingMode = PickingMode.Ignore };
            hurtFlash.AddToClassList("hurt-flash");
            hurtFlash.style.opacity = 0f;
            layer.Add(hurtFlash);
            CombatFeedback.Hit += OnHit;
        }

        private void OnDisable()
        {
            CombatFeedback.Hit -= OnHit;
            layer?.RemoveFromHierarchy();
            plates.Clear();
            numbers.Clear();
        }

        private void LateUpdate()
        {
            if (view == null)
                view = Camera.main;
            if (view == null || layer?.panel == null)
                return;

            if (Time.unscaledTime >= nextScan)
            {
                nextScan = Time.unscaledTime + RescanSeconds;
                foreach (EnemyController enemy in FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude))
                    if (!plates.ContainsKey(enemy))
                        plates[enemy] = CreatePlate(enemy);
            }

            UpdatePlates();
            UpdateNumbers();
            if (hurtFlashLeft > 0f)
            {
                hurtFlashLeft -= Time.deltaTime;
                hurtFlash.style.opacity = Mathf.Clamp01(hurtFlashLeft / 0.35f);
            }
        }

        private void UpdatePlates()
        {
            stale.Clear();
            foreach (KeyValuePair<EnemyController, Plate> pair in plates)
            {
                EnemyController enemy = pair.Key;
                Plate plate = pair.Value;
                if (enemy == null || !enemy.isActiveAndEnabled)
                {
                    stale.Add(enemy);
                    plate.Root.RemoveFromHierarchy();
                    continue;
                }

                bool show = !enemy.IsDead && Place(plate.Root, enemy.transform.position + Vector3.up * (1.55f * enemy.transform.localScale.y));
                plate.Root.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
                if (show)
                    plate.Fill.style.width = Length.Percent(enemy.HealthFraction * 100f);
            }

            foreach (EnemyController enemy in stale)
                plates.Remove(enemy);
        }

        private void UpdateNumbers()
        {
            for (int i = numbers.Count - 1; i >= 0; i--)
            {
                FloatingNumber number = numbers[i];
                number.Age += Time.deltaTime;
                float t = number.Age / NumberSeconds;
                if (t >= 1f)
                {
                    number.Label.RemoveFromHierarchy();
                    numbers.RemoveAt(i);
                    continue;
                }

                Place(number.Label, number.World + Vector3.up * (t * 0.9f));
                number.Label.style.opacity = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
                float pop = t < 0.12f ? Mathf.Lerp(1.6f, 1f, t / 0.12f) : 1f;
                number.Label.style.scale = new Scale(new Vector3(pop, pop, 1f));
            }
        }

        /// <summary>Moves a UI element so it is centred on a world point. False when the point is behind the camera.</summary>
        private bool Place(VisualElement element, Vector3 world)
        {
            if (view.WorldToViewportPoint(world).z < 0f)
                return false;
            Vector2 point = RuntimePanelUtils.CameraTransformWorldToPanel(layer.panel, world, view);
            element.style.left = point.x;
            element.style.top = point.y;
            return true;
        }

        private Plate CreatePlate(EnemyController enemy)
        {
            var root = new VisualElement { pickingMode = PickingMode.Ignore };
            root.AddToClassList("nameplate");
            var name = new Label($"{enemy.Data.DisplayName.ToUpperInvariant()}  LV.{enemy.Data.Level}") { pickingMode = PickingMode.Ignore };
            name.AddToClassList("nameplate-label");
            var bar = new VisualElement { pickingMode = PickingMode.Ignore };
            bar.AddToClassList("nameplate-bar");
            var fill = new VisualElement { pickingMode = PickingMode.Ignore };
            fill.AddToClassList("nameplate-fill");
            bar.Add(fill);
            root.Add(name);
            root.Add(bar);
            layer.Add(root);
            return new Plate { Root = root, Name = name, Fill = fill };
        }

        private void OnHit(Vector3 position, int amount, bool critical, bool playerHurt)
        {
            if (layer == null)
                return;
            if (playerHurt)
                hurtFlashLeft = 0.35f; // red glow around the screen edges
            var label = new Label(critical ? $"-{amount} CRIT!" : $"-{amount}") { pickingMode = PickingMode.Ignore };
            label.AddToClassList("damage-number");
            if (critical) label.AddToClassList("damage-number--crit");
            if (playerHurt) label.AddToClassList("damage-number--hurt");
            layer.Add(label);
            Vector3 jitter = new Vector3(Random.Range(-0.15f, 0.15f), 0f, Random.Range(-0.15f, 0.15f));
            numbers.Add(new FloatingNumber { Label = label, World = position + jitter });
        }
    }
}
