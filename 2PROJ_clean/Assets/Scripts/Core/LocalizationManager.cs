using System;
using System.Collections.Generic;
using UnityEngine;

namespace SupKonQuest
{
    [Serializable]
    public class LocalizationEntry
    {
        public string key;
        public string value;
    }

    [Serializable]
    public class LocalizationData
    {
        public List<LocalizationEntry> entries = new List<LocalizationEntry>();
    }

    public class LocalizationManager : MonoBehaviour
    {
        public static LocalizationManager Instance { get; private set; }

        private readonly Dictionary<string, string> entries = new Dictionary<string, string>();
        private string currentLanguage = "fr";

        public string CurrentLanguage => currentLanguage;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            string lang = PlayerPrefs.GetString("Language", "fr");
            LoadLanguage(lang);
        }

        public void LoadLanguage(string langCode)
        {
            currentLanguage = langCode;
            PlayerPrefs.SetString("Language", langCode);

            TextAsset asset = Resources.Load<TextAsset>($"Localization/{langCode}");
            if (asset == null)
            {
                Debug.LogWarning($"[Localization] Fichier manquant : Resources/Localization/{langCode}.json");
                return;
            }

            try
            {
                LocalizationData data = JsonUtility.FromJson<LocalizationData>(asset.text);
                entries.Clear();
                foreach (LocalizationEntry e in data.entries)
                    if (!string.IsNullOrEmpty(e.key))
                        entries[e.key] = e.value;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Localization] Erreur parsing {langCode}.json : {ex.Message}");
            }
        }

        public static string Get(string key)
        {
            if (Instance != null && Instance.entries.TryGetValue(key, out string val))
                return val;
            return key; // fallback : retourne la clé si pas trouvée
        }
    }
}
