using FS_ThirdPerson;
using UnityEngine;
using UnityEngine.Events;

namespace FS_Core
{
    public class EquipHotspot : Hotspot
    {
        [Header("Equip")]
        public EquippableItem item;

        [Space(5)]
        public UnityEvent OnEquipEvent;

        [HideInInspector] public EquippableItemObject itemObject;

        [Header("Trigger & Hint (your own UI)")]
        public string playerTag = "Player";
        public KeyCode interactKey = KeyCode.E;

        [Tooltip("Drag your world-space hint UI here (e.g., a Canvas that says 'Press E').")]
        public GameObject hintUI;

        private bool playerInside;

        private void Reset()
        {
            // Ensure trigger collider if this object has one
            var col = GetComponent<Collider>();
            if (col) col.isTrigger = true;
        }

        private void Start()
        {
            SetHint(false);
        }

        private void Update()
        {
            if (!playerInside) return;

            if (Input.GetKeyDown(interactKey))
            {
                // ✅ Keep ALL original equip logic
                // We need a detector reference to equip (Fantacode expects HotspotDetector)
                // Usually HotspotDetector is on the player.
                HotspotDetector detector = FindDetectorOnPlayer();

                if (detector == null)
                {
                    Debug.LogWarning($"[EquipHotspot] No HotspotDetector found on player. Cannot equip: {item?.Name}");
                    return;
                }

                Interact(detector); // calls your original Interact logic
            }
        }

        private HotspotDetector FindDetectorOnPlayer()
        {
            // Find the player (by tag) and get HotspotDetector component
            var player = GameObject.FindGameObjectWithTag(playerTag);
            if (!player) return null;

            return player.GetComponent<HotspotDetector>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;

            playerInside = true;
            SetHint(true);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;

            playerInside = false;
            SetHint(false);
        }

        private void SetHint(bool show)
        {
            if (hintUI) hintUI.SetActive(show);
        }

        // ✅ Keep this override but it no longer controls UI
        public override void ShowIndicator(bool state)
        {
            // We intentionally ignore Hotspot's UI indicator now.
            // Do nothing (or you can keep base.ShowIndicator(false) if needed).
            base.ShowIndicator(false);
        }

        // ✅ Original equip logic (unchanged)
        public override void Interact(HotspotDetector detector)
        {
            var itemEquipper = detector.GetComponent<ItemEquipper>();
            if (!itemEquipper.PreventItemSwitching && !itemEquipper.IsChangingItem)
            {
                itemEquipper.EquipItem(item, false, itemObject: itemObject);
                OnEquipEvent?.Invoke();

                base.Interact(detector);

                // Hide hint after equip
                SetHint(false);

                if (itemObject != null)
                    itemObject.gameObject.SetActive(false);
            }
        }
    }
}
