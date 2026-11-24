using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Загружает скилы из Resources/Skills.json и раздает их по панелям.
///
/// Каждый скил лежит на панели page (1..10) и занимает слот по порядку внутри
/// своей панели: первый скил панели 1 — клавиша 1, второй — 2 и так далее.
/// Работает как EnemyDatabase: файл читается один раз, при первом обращении.
/// </summary>
public static class SkillDatabase
{
    public const int SlotsPerPage = 10;

    private const string ResourcePath = "Skills";

    private static readonly Dictionary<int, List<SkillData>> _pages = new Dictionary<int, List<SkillData>>();
    private static bool _initialized;

    /// <summary>Сколько всего скилов в файле.</summary>
    public static int Count { get; private set; }

    /// <summary>Скил в конкретном слоте панели. null, если слот пустой.</summary>
    public static SkillData GetSlot(int page, int slot)
    {
        Initialize();

        if (slot < 0 || slot >= SlotsPerPage) return null;

        List<SkillData> skills;
        if (!_pages.TryGetValue(page, out skills) || skills == null) return null;

        return slot < skills.Count ? skills[slot] : null;
    }

    public static SkillData GetById(string id)
    {
        Initialize();
        if (string.IsNullOrEmpty(id)) return null;

        foreach (KeyValuePair<int, List<SkillData>> pair in _pages)
        {
            List<SkillData> skills = pair.Value;
            for (int i = 0; i < skills.Count; i++)
            {
                if (skills[i] != null && skills[i].id == id) return skills[i];
            }
        }
        return null;
    }

    private static void Initialize()
    {
        // Флаг ставим до загрузки: если файла нет, не спамим ошибкой на каждый запрос
        if (_initialized) return;
        _initialized = true;

        TextAsset jsonFile = Resources.Load<TextAsset>(ResourcePath);
        if (jsonFile == null)
        {
            Debug.LogError("SkillDatabase: не найден Assets/_Project/Resources/" + ResourcePath + ".json");
            return;
        }

        SkillListWrapper wrapper = JsonUtility.FromJson<SkillListWrapper>(jsonFile.text);
        if (wrapper == null || wrapper.skills == null)
        {
            Debug.LogError("SkillDatabase: не удалось разобрать " + ResourcePath + ".json");
            return;
        }

        for (int i = 0; i < wrapper.skills.Count; i++)
        {
            SkillData skill = wrapper.skills[i];
            if (skill == null || string.IsNullOrEmpty(skill.id)) continue;

            AddSkill(skill);
            Count++;
        }

        Debug.Log("SkillDatabase: загружено скилов — " + Count + " на " + _pages.Count + " панелях");
    }

    private static void AddSkill(SkillData skill)
    {
        // page == 0 значит "в файле не указан" — кладем на первую панель
        int page = skill.page <= 0 ? 1 : skill.page;

        List<SkillData> skills;
        if (!_pages.TryGetValue(page, out skills))
        {
            skills = new List<SkillData>(SlotsPerPage);
            _pages[page] = skills;
        }

        if (skills.Count >= SlotsPerPage)
        {
            Debug.LogWarning("SkillDatabase: на панели " + page + " больше " + SlotsPerPage +
                             " скилов, '" + skill.id + "' пропущен");
            return;
        }

        skills.Add(skill);
    }
}
