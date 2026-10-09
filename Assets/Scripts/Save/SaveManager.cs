using UnityEngine;
using System.IO;
using AbyssalDungeon.Core;

namespace AbyssalDungeon.Save
{
    public class SaveManager : Singleton<SaveManager>
    {
        private string _saveFilePath;
        private SaveData _currentSaveData;

        public SaveData CurrentSaveData => _currentSaveData;

        protected override void Awake()
        {
            base.Awake();
            _saveFilePath = Path.Combine(Application.persistentDataPath, "saveData.json");
            LoadGame();
        }

        public void SaveGame()
        {
            if (_currentSaveData == null)
            {
                _currentSaveData = new SaveData();
            }

            string json = JsonUtility.ToJson(_currentSaveData, true);
            File.WriteAllText(_saveFilePath, json);
            Debug.Log($"[SaveManager] Game saved to {_saveFilePath}");
        }

        public void LoadGame()
        {
            if (File.Exists(_saveFilePath))
            {
                string json = File.ReadAllText(_saveFilePath);
                _currentSaveData = JsonUtility.FromJson<SaveData>(json);
                Debug.Log("[SaveManager] Game loaded successfully.");
            }
            else
            {
                Debug.Log("[SaveManager] No save file found, creating new save data.");
                _currentSaveData = new SaveData();
            }
        }

        public void AutoSave()
        {
            SaveGame();
        }

        public void SaveSettings(float masterVolume, float musicVolume, float sfxVolume)
        {
            PlayerPrefs.SetFloat("MasterVolume", masterVolume);
            PlayerPrefs.SetFloat("MusicVolume", musicVolume);
            PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
            PlayerPrefs.Save();
        }

        public void LoadSettings(out float masterVolume, out float musicVolume, out float sfxVolume)
        {
            masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
            sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
        }
    }
}
