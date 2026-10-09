using UnityEngine;
using System;
using System.Linq;

namespace AbyssalDungeon.Classes
{
    public class ClassManager : MonoBehaviour
    {
        public static ClassManager Instance { get; private set; }

        [Header("Class Configuration")]
        [SerializeField] private ClassData[] allClasses;
        
        [Header("Current State")]
        [SerializeField] private int selectedClassIndex = 0;

        public event Action<ClassData> OnClassSelected;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public ClassData GetClassByName(string name)
        {
            if (allClasses == null) return null;
            return allClasses.FirstOrDefault(c => c != null && c.className.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public ClassData GetClassByIndex(int index)
        {
            if (allClasses == null || index < 0 || index >= allClasses.Length)
            {
                Debug.LogWarning($"Class index {index} is out of bounds or allClasses is null.");
                return null;
            }
            return allClasses[index];
        }

        public ClassData[] GetAllClasses()
        {
            return allClasses;
        }

        public void SelectClass(int index)
        {
            ClassData selected = GetClassByIndex(index);
            if (selected != null)
            {
                selectedClassIndex = index;
                OnClassSelected?.Invoke(selected);
                Debug.Log($"Class selected: {selected.className}");
            }
        }

        public int GetSelectedClassIndex()
        {
            return selectedClassIndex;
        }
        
        public ClassData GetSelectedClass()
        {
            return GetClassByIndex(selectedClassIndex);
        }
    }
}
