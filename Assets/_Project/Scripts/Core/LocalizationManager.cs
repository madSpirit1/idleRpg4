using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LocalizationEntry
{
    public string key;
    public string value;
}

[Serializable]
public class LocalizationWrapper
{
    public List<LocalizationEntry> keys;
}

public static class LocalizationManager
{
    private static Dictionary<string, string> _localizedText = new Dictionary<string, string>();
    private static string _currentLanguage = "en"; // Английский по умолчанию в коде
    private static bool _isInitialized = false;

    public static void SetLanguage(string langCode)
    {
        _currentLanguage = langCode;
        _isInitialized = false;
        Initialize();
    }

    public static void Initialize()
    {
        if (_isInitialized) return;

        // Загружаем файл локализации (например, "Localization/en" или "Localization/ru")
        TextAsset jsonFile = Resources.Load<TextAsset>("Localization/" + _currentLanguage);
        
        if (jsonFile != null)
        {
            _localizedText.Clear();
            LocalizationWrapper wrapper = JsonUtility.FromJson<LocalizationWrapper>(jsonFile.text);
            
            for (int i = 0; !(i >= wrapper.keys.Count); i++)
            {
                _localizedText[wrapper.keys[i].key] = wrapper.keys[i].value;
            }
            
            _isInitialized = true;
            Debug.Log("Localization loaded: " + _currentLanguage + " | Total keys: " + _localizedText.Count);
        }
        else
        {
            Debug.LogError("Localization file not found for language: " + _currentLanguage);
            // Если выбранного языка нет, аварийно пытаемся загрузить английский
            if (_currentLanguage != "en")
            {
                _currentLanguage = "en";
                Initialize();
            }
        }
    }

    // Главный метод получения перевода. Если ключа нет, вернет сам ключ
    public static string Get(string key)
    {
        Initialize();
        if (_localizedText.ContainsKey(key))
        {
            return _localizedText[key];
        }
        return key;
    }
}