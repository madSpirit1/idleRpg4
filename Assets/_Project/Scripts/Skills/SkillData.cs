using System;
using System.Collections.Generic;

/// <summary>
/// Описание одного скила. Поля совпадают с ключами в Resources/Skills.json,
/// поэтому структура данных правится в файле, без перекомпиляции.
/// </summary>
[Serializable]
public class SkillData
{
    public string id;               // уникальный идентификатор, например "fireball"
    public int page = 1;            // номер панели, на которой лежит скил (1..10)
    public string icon;             // 1-2 символа для квадрата на панели
    public string name;             // название для панели
    public string description;      // короткое описание
    public string damageType;       // physical / fire / ice / poison / holy / dark
    public string[] pattern;        // зона действия: "X" — клетка под эффектом, "." — пустая
    public string[] extraEffects;   // дополнительные эффекты

    public int PatternHeight
    {
        get { return pattern == null ? 0 : pattern.Length; }
    }

    public int PatternWidth
    {
        get
        {
            if (pattern == null || pattern.Length == 0) return 0;

            int width = 0;
            for (int i = 0; i < pattern.Length; i++)
            {
                if (pattern[i] != null && pattern[i].Length > width) width = pattern[i].Length;
            }
            return width;
        }
    }

    public bool HasPattern
    {
        get { return PatternWidth > 0 && PatternHeight > 0; }
    }

    public bool IsCellAffected(int column, int row)
    {
        if (pattern == null) return false;
        if (row < 0 || row >= pattern.Length) return false;

        string line = pattern[row];
        if (line == null || column < 0 || column >= line.Length) return false;

        char cell = line[column];
        return cell == 'X' || cell == 'x';
    }
}

/// <summary>Обертка для JsonUtility: массивы на верхнем уровне он не умеет.</summary>
[Serializable]
public class SkillListWrapper
{
    public List<SkillData> skills;
}
