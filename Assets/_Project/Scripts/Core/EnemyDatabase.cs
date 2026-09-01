using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EnemyStats
{
    public string enemyID;
    public string nameKey;
    public string raceKey;
    public string textureName; // Имя картинки в папке Resources
    public int maxHp;
    public int maxMana;
    public int damage;
    public int maxActionPoints;

    public string EnemyName => LocalizationManager.Get(nameKey);
    public string Race => LocalizationManager.Get(raceKey);
}

[Serializable]
public class EnemyListWrapper
{
    public List<EnemyStats> enemies;
}

public static class EnemyDatabase
{
    private static Dictionary<string, EnemyStats> _database = new Dictionary<string, EnemyStats>();
    private static bool _isInitialized = false;

    public static void Initialize()
    {
        if (_isInitialized) return;

        TextAsset jsonFile = Resources.Load<TextAsset>("EnemyDatabase");
        
        if (jsonFile != null)
        {
            EnemyListWrapper wrapper = JsonUtility.FromJson<EnemyListWrapper>(jsonFile.text);
            
            for (int i = 0; !(i >= wrapper.enemies.Count); i++)
            {
                EnemyStats stats = wrapper.enemies[i];
                _database[stats.enemyID] = stats;
            }
            
            _isInitialized = true;
        }
        else
        {
            Debug.LogError("EnemyDatabase.json not found in Resources!");
        }
    }

    public static EnemyStats GetStats(string enemyID)
    {
        Initialize();
        if (_database.ContainsKey(enemyID))
        {
            EnemyStats template = _database[enemyID];
            EnemyStats instanceStats = new EnemyStats();
            instanceStats.enemyID = template.enemyID;
            instanceStats.nameKey = template.nameKey;
            instanceStats.raceKey = template.raceKey;
            instanceStats.textureName = template.textureName; // Передаем текстуру
            instanceStats.maxHp = template.maxHp;
            instanceStats.maxMana = template.maxMana;
            instanceStats.damage = template.damage;
            instanceStats.maxActionPoints = template.maxActionPoints;
            return instanceStats;
        }
        return null;
    }
}
