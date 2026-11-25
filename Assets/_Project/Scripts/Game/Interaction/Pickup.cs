using LostDungeon.Game.Inventory;
using LostDungeon.Game.Player;
using UnityEngine;

namespace LostDungeon.Game.Interaction
{
    /// <summary>
    /// A collectible lying on the floor. Walking over it collects it.
    /// Potions heal straight away; everything else goes into the inventory.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Pickup : MonoBehaviour, ICollectable
    {
        [SerializeField] private string itemId = ItemIds.Gold;
        [SerializeField] private int amount = 1;
        [SerializeField] private int healAmount = 2;
        [SerializeField] private Transform visual;

        private Vector3 visualStart;

        private void Awake()
        {
            if (visual != null)
                visualStart = visual.localPosition;
        }

        private void Update()
        {
            if (visual == null)
                return;
            // Float and turn slowly so loot catches the eye.
            visual.localPosition = visualStart + Vector3.up * (Mathf.Sin(Time.time * 3f) * 0.06f);
            visual.Rotate(0f, 90f * Time.deltaTime, 0f, Space.World);
        }

        private void OnTriggerEnter(Collider other)
        {
            GameObject who = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;
            if (who.TryGetComponent(out PlayerInventory _))
                Collect(who);
        }

        public void Collect(GameObject collector)
        {
            if (itemId == ItemIds.Potion && collector.TryGetComponent(out PlayerHealth health))
            {
                health.Heal(healAmount);
                LostDungeon.Core.Events.GameEvents.RaiseItemCollected(itemId, amount); // sound, stats
            }
            else
            {
                collector.GetComponent<PlayerInventory>().Add(itemId, amount);
            }

            gameObject.SetActive(false);
        }
    }
}
