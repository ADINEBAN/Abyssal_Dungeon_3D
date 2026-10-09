using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

namespace AbyssalDungeon.Dungeon
{
    public class PuzzleManager : MonoBehaviour
    {
        [Header("Crystals Configuration")]
        [SerializeField] private CrystalInteractable[] crystals;
        [SerializeField] private int[] correctSequence;
        
        [Header("UI & References")]
        [SerializeField] private TextMeshProUGUI hintText;
        [SerializeField] private DoorController doorToOpen;
        
        [Header("Events")]
        public UnityEvent OnPuzzleSolved;

        private List<int> currentInputSequence = new List<int>();
        private bool puzzleSolved = false;

        private void Start()
        {
            if (hintText != null) hintText.text = "Activate the crystals in the correct order.";
            
            for (int i = 0; i < crystals.Length; i++)
            {
                int index = i; // local copy for closure
                crystals[i].OnCrystalActivated.AddListener(() => OnCrystalClicked(index));
            }
        }

        private void OnCrystalClicked(int crystalIndex)
        {
            if (puzzleSolved) return;

            currentInputSequence.Add(crystalIndex);
            crystals[crystalIndex].SetGlow(Color.cyan);

            if (currentInputSequence.Count == correctSequence.Length)
            {
                CheckSequence();
            }
        }

        private void CheckSequence()
        {
            bool isCorrect = true;
            for (int i = 0; i < correctSequence.Length; i++)
            {
                if (currentInputSequence[i] != correctSequence[i])
                {
                    isCorrect = false;
                    break;
                }
            }

            if (isCorrect)
            {
                SolvePuzzle();
            }
            else
            {
                StartCoroutine(ResetPuzzleRoutine());
            }
        }

        private void SolvePuzzle()
        {
            puzzleSolved = true;
            if (hintText != null) hintText.text = "Puzzle Solved!";
            
            foreach (var crystal in crystals)
            {
                crystal.SetGlow(Color.green);
            }

            if (doorToOpen != null) doorToOpen.Unlock();
            OnPuzzleSolved?.Invoke();
        }

        private IEnumerator ResetPuzzleRoutine()
        {
            if (hintText != null) hintText.text = "Wrong sequence!";
            
            foreach (var crystal in crystals)
            {
                crystal.SetGlow(Color.red);
            }

            yield return new WaitForSeconds(1.5f);

            currentInputSequence.Clear();
            foreach (var crystal in crystals)
            {
                crystal.SetGlow(Color.black); // Off
            }

            if (hintText != null) hintText.text = "Try again.";
        }
    }

    // Small helper class for the crystals
    public class CrystalInteractable : MonoBehaviour
    {
        public UnityEvent OnCrystalActivated;
        [SerializeField] private Renderer crystalRenderer;
        private bool isPlayerNearby = false;

        private void Update()
        {
            if (isPlayerNearby && Input.GetKeyDown(KeyCode.F))
            {
                OnCrystalActivated?.Invoke();
            }
        }

        private void OnTriggerEnter(Collider other) { if (other.CompareTag("Player")) isPlayerNearby = true; }
        private void OnTriggerExit(Collider other) { if (other.CompareTag("Player")) isPlayerNearby = false; }

        public void SetGlow(Color color)
        {
            if (crystalRenderer != null)
            {
                if (color == Color.black)
                {
                    crystalRenderer.material.DisableKeyword("_EMISSION");
                }
                else
                {
                    crystalRenderer.material.EnableKeyword("_EMISSION");
                    crystalRenderer.material.SetColor("_EmissionColor", color);
                }
            }
        }
    }
}
