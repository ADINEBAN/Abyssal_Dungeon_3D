using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;

namespace AbyssalDungeon.UI
{
    [System.Serializable]
    public class PlayerSlotUI
    {
        public GameObject slotRoot;
        public Image iconImage;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI classText;
        public TextMeshProUGUI levelText;
        public GameObject readyIndicator;
        public bool isOccupied;
        public bool isReady;
    }

    public class LobbyUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private List<PlayerSlotUI> playerSlots;

        [Header("Buttons")]
        [SerializeField] private Button createRoomButton;
        [SerializeField] private Button joinRoomButton;
        [SerializeField] private Button inviteButton;
        [SerializeField] private Button readyButton;
        [SerializeField] private Button backButton;
        
        private bool localPlayerReady = false;
        private int currentPlayers = 1;

        private void Start()
        {
            createRoomButton.onClick.AddListener(OnCreateRoom);
            joinRoomButton.onClick.AddListener(OnJoinRoom);
            inviteButton.onClick.AddListener(OnInvite);
            readyButton.onClick.AddListener(OnReadyToggle);
            backButton.onClick.AddListener(OnBack);

            SetupInitialSlots();
        }

        private void SetupInitialSlots()
        {
            // Setup local player
            UpdateSlot(0, true, "PlayerOne", "Warrior", 10, false);
            
            // Clear others
            for(int i = 1; i < playerSlots.Count; i++)
            {
                UpdateSlot(i, false, "", "", 0, false);
            }
        }

        private void UpdateSlot(int index, bool occupied, string name, string className, int level, bool ready)
        {
            if (index < 0 || index >= playerSlots.Count) return;

            var slot = playerSlots[index];
            slot.isOccupied = occupied;
            slot.slotRoot.SetActive(occupied);
            
            if (occupied)
            {
                slot.nameText.text = name;
                slot.classText.text = className;
                slot.levelText.text = "Lv." + level;
                slot.isReady = ready;
                slot.readyIndicator.SetActive(ready);
            }
        }

        private void OnCreateRoom()
        {
            Debug.Log("Room Created. Waiting for players...");
            StartCoroutine(MockPlayersJoining());
        }

        private void OnJoinRoom()
        {
            Debug.Log("Searching for room...");
        }

        private void OnInvite()
        {
            Debug.Log("Invite friends dialog opened.");
        }

        private void OnReadyToggle()
        {
            localPlayerReady = !localPlayerReady;
            UpdateSlot(0, true, "PlayerOne", "Warrior", 10, localPlayerReady);
            CheckAllReady();
        }

        private void OnBack()
        {
            SceneManager.LoadScene("MainMenu");
        }

        private IEnumerator MockPlayersJoining()
        {
            yield return new WaitForSeconds(1.5f);
            UpdateSlot(1, true, "MageMaster", "Mage", 12, true);
            currentPlayers++;
            
            yield return new WaitForSeconds(2.0f);
            UpdateSlot(2, true, "SneakyRogue", "Rogue", 9, true);
            currentPlayers++;
            
            CheckAllReady();
        }

        private void CheckAllReady()
        {
            bool allReady = true;
            for(int i = 0; i < currentPlayers; i++)
            {
                if (!playerSlots[i].isReady) allReady = false;
            }

            if (allReady && currentPlayers > 1)
            {
                Debug.Log("All ready! Loading Class Selection...");
                SceneManager.LoadScene("ClassSelection");
            }
        }
    }
}
