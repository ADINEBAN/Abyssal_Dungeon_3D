using System.Collections;
using UnityEngine;
using TMPro;

namespace AbyssalDungeon.Dungeon
{
    public enum ChestState { Closed, Opening, Open }

    // Mock LootTable to ensure compilation
    public class LootTable : ScriptableObject
    {
        public string[] items;
        public string GetRandomLoot() { return items != null && items.Length > 0 ? items[Random.Range(0, items.Length)] : "Gold"; }
    }

    public class TreasureChest : MonoBehaviour
    {
        [Header("State")]
        [SerializeField] private ChestState currentState = ChestState.Closed;
        
        [Header("References")]
        [SerializeField] private Transform lidTransform;
        [SerializeField] private LootTable lootTable;
        [SerializeField] private Renderer chestRenderer;
        [SerializeField] private TextMeshProUGUI lootPopupText;

        [Header("Settings")]
        [SerializeField] private float openAngle = -90f;
        [SerializeField] private float openDuration = 1.0f;
        [ColorUsage(true, true)]
        [SerializeField] private Color glowColor = Color.yellow;
        
        private Material chestMaterial;
        private bool isPlayerNearby = false;

        private void Awake()
        {
            if (chestRenderer != null)
            {
                chestMaterial = chestRenderer.material;
                SetGlow(true);
            }
            if (lootPopupText != null) lootPopupText.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (isPlayerNearby && currentState == ChestState.Closed && Input.GetKeyDown(KeyCode.F))
            {
                Interact();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player")) isPlayerNearby = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player")) isPlayerNearby = false;
        }

        public void Interact()
        {
            if (currentState != ChestState.Closed) return;
            
            currentState = ChestState.Opening;
            SetGlow(false);
            StartCoroutine(OpenChestRoutine());
        }

        private IEnumerator OpenChestRoutine()
        {
            Quaternion startRot = lidTransform.localRotation;
            Quaternion endRot = startRot * Quaternion.Euler(openAngle, 0, 0);
            float time = 0;

            while (time < 1f)
            {
                time += Time.deltaTime / openDuration;
                lidTransform.localRotation = Quaternion.Slerp(startRot, endRot, time);
                yield return null;
            }

            currentState = ChestState.Open;
            SpawnLoot();
        }

        private void SpawnLoot()
        {
            string loot = lootTable != null ? lootTable.GetRandomLoot() : "100 Gold";
            Debug.Log($"Spawned Loot: {loot}");

            if (lootPopupText != null)
            {
                lootPopupText.text = $"Found: {loot}!";
                lootPopupText.gameObject.SetActive(true);
                Invoke(nameof(HidePopup), 3f);
            }
        }

        private void HidePopup()
        {
            if (lootPopupText != null) lootPopupText.gameObject.SetActive(false);
        }

        private void SetGlow(bool active)
        {
            if (chestMaterial != null)
            {
                if (active)
                {
                    chestMaterial.EnableKeyword("_EMISSION");
                    chestMaterial.SetColor("_EmissionColor", glowColor);
                }
                else
                {
                    chestMaterial.DisableKeyword("_EMISSION");
                }
            }
        }
    }
}
