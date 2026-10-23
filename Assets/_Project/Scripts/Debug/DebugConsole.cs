using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Отладочная консоль для тестирования игры (спавн врагов и т.п.).
///
/// Управление:
///   "/"    — открыть консоль
///   Esc    — закрыть
///   Enter  — выполнить команду
///   ↑ / ↓  — история команд
///
/// Команды:
///   spawn &lt;id&gt;  — создать врага рядом с игроком
///   spawn        — показать доступные id
///   clear        — очистить лог
///   help         — справка
///
/// Ввод символов идет через Keyboard.onTextInput, поэтому "/" срабатывает
/// в любой раскладке (на русской это Shift + клавиша "\"), а не только на US.
/// UI строится кодом на Canvas: не нужны ни EventSystem, ни префабы, ни ассеты шрифтов.
/// </summary>
public class DebugConsole : MonoBehaviour
{
    // Пока консоль открыта, PlayerInput игнорирует клики/пробел/F5/F6
    public static bool IsOpen { get; private set; }

    [Header("Спавн врагов")]
    [Tooltip("Радиус поиска свободной клетки вокруг игрока")]
    public int spawnRadius = 4;

    [Header("Визуал")]
    public int fontSize = 18;

    private const int MaxBufferedLines = 200;
    private const int MaxInputLength = 160;
    private const int MaxHistoryLines = 50;
    private const float CaretBlinkInterval = 0.5f;

    // --- визуал ---
    private GameObject _root;
    private Text _logText;
    private Text _inputText;
    private RectTransform _logRect;

    // --- состояние ---
    private readonly List<string> _lines = new List<string>();
    private readonly List<string> _history = new List<string>();
    private readonly System.Text.StringBuilder _input = new System.Text.StringBuilder();
    private int _historyIndex;
    private bool _logDirty;
    private bool _caretVisible = true;
    private float _caretTimer;
    private bool _welcomeShown;

    // --- ввод ---
    private Keyboard _keyboard;
    private bool _openRequested;
    private bool _backspaceFromText;
    private bool _submitFromText;

    // =====================================================================
    // Автосоздание консоли при старте сцены (сцену и префабы трогать не нужно)
    // =====================================================================
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindAnyObjectByType<DebugConsole>() != null) return;

        GameObject consoleObject = new GameObject("~DebugConsole");
        consoleObject.AddComponent<DebugConsole>();
    }

    private void Awake()
    {
        IsOpen = false;
        BuildUi();
        SetVisible(false);
    }

    private void OnEnable()
    {
        Application.logMessageReceived += OnUnityLog;
        EnsureKeyboard();
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= OnUnityLog;

        if (_keyboard != null)
        {
            _keyboard.onTextInput -= OnTextInput;
            _keyboard = null;
        }

        IsOpen = false;
    }

    // =====================================================================
    // Ввод
    // =====================================================================
    private void EnsureKeyboard()
    {
        Keyboard keyboard = Keyboard.current;
        if (_keyboard == keyboard) return;

        if (_keyboard != null) _keyboard.onTextInput -= OnTextInput;
        _keyboard = keyboard;
        if (_keyboard != null) _keyboard.onTextInput += OnTextInput;
    }

    // Сюда приходит именно введенный символ с учетом раскладки ОС
    private void OnTextInput(char character)
    {
        if (!IsOpen)
        {
            // "/" на русской раскладке даст этот же символ, т.к. раскладка уже применена
            if (character == '/') _openRequested = true;
            return;
        }

        if (character == '\b')
        {
            _backspaceFromText = true;
            Backspace();
            return;
        }

        if (character == '\n' || character == '\r')
        {
            _submitFromText = true;
            Submit();
            return;
        }

        if (character == '\t') return;

        // Управляющие символы не печатаем
        if (character < ' ' || character == '\u007f') return;

        Append(character);
    }

    private void Update()
    {
        EnsureKeyboard();

        Keyboard keyboard = _keyboard;
        if (keyboard == null)
        {
            _openRequested = false;
            return;
        }

        // Физическая клавиша "/" — страховка, если по какой-то причине текстовый ввод не пришел
        if (keyboard.slashKey.wasPressedThisFrame) _openRequested = true;

        if (_openRequested)
        {
            _openRequested = false;
            if (!IsOpen) Open();
        }

        if (!IsOpen) return;

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            Close();
            return;
        }

        if (keyboard.upArrowKey.wasPressedThisFrame) BrowseHistory(-1);
        if (keyboard.downArrowKey.wasPressedThisFrame) BrowseHistory(1);

        // Страховка для Enter/Backspace, если они не пришли как текстовый ввод.
        // Флаги не дают обработать одно нажатие дважды.
        if (keyboard.backspaceKey.wasPressedThisFrame && !_backspaceFromText) Backspace();
        if (keyboard.enterKey.wasPressedThisFrame && !_submitFromText) Submit();

        _backspaceFromText = false;
        _submitFromText = false;

        _caretTimer += Time.unscaledDeltaTime;
        if (_caretTimer >= CaretBlinkInterval)
        {
            _caretTimer = 0f;
            _caretVisible = !_caretVisible;
            RefreshInput();
        }

        RefreshLog();
    }

    private void Append(char character)
    {
        if (_input.Length >= MaxInputLength) return;

        _input.Append(character);
        _caretVisible = true;
        _caretTimer = 0f;
        RefreshInput();
    }

    private void Backspace()
    {
        if (_input.Length == 0) return;

        _input.Length = _input.Length - 1;
        _caretVisible = true;
        _caretTimer = 0f;
        RefreshInput();
    }

    private void BrowseHistory(int direction)
    {
        if (_history.Count == 0) return;

        int target = Mathf.Clamp(_historyIndex + direction, 0, _history.Count);
        if (target == _historyIndex) return;

        _historyIndex = target;
        _input.Clear();
        if (_historyIndex < _history.Count) _input.Append(_history[_historyIndex]);

        RefreshInput();
    }

    // =====================================================================
    // Открытие / закрытие
    // =====================================================================
    private void Open()
    {
        IsOpen = true;
        SetVisible(true);

        _input.Clear();
        _historyIndex = _history.Count;
        _caretVisible = true;
        _caretTimer = 0f;

        if (!_welcomeShown)
        {
            _welcomeShown = true;
            AddLine("Консоль открыта. help — список команд, Esc — закрыть.");
        }

        RefreshInput();
        RefreshLog();
    }

    private void Close()
    {
        IsOpen = false;
        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        if (_root != null) _root.SetActive(visible);
    }

    // =====================================================================
    // Команды
    // =====================================================================
    private void Submit()
    {
        string commandLine = _input.ToString().Trim();
        _input.Clear();
        _caretVisible = true;
        _caretTimer = 0f;

        if (commandLine.Length > 0)
        {
            _history.Add(commandLine);
            if (_history.Count > MaxHistoryLines) _history.RemoveAt(0);
            _historyIndex = _history.Count;

            AddLine("> " + commandLine);
            Execute(commandLine);
        }

        RefreshInput();
    }

    private void Execute(string commandLine)
    {
        string[] parts = commandLine.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;

        string command = parts[0].ToLowerInvariant();

        switch (command)
        {
            case "help":
                AddLine("Команды:");
                AddLine("  spawn <id>  — создать врага рядом с игроком");
                AddLine("  spawn       — список доступных id");
                AddLine("  clear       — очистить консоль");
                break;

            case "clear":
                _lines.Clear();
                _logDirty = true;
                break;

            case "spawn":
                ExecuteSpawn(parts);
                break;

            default:
                AddLine("Неизвестная команда: " + command + " (help — список команд)");
                break;
        }
    }

    private void ExecuteSpawn(string[] parts)
    {
        string[] availableIds = EnemyDatabase.GetIds();

        if (availableIds.Length == 0)
        {
            AddLine("Ошибка: база врагов пуста (Resources/EnemyDatabase.json).");
            return;
        }

        if (parts.Length < 2)
        {
            AddLine("Укажите id: spawn <id>");
            AddLine("Доступные id: " + string.Join(", ", availableIds));
            return;
        }

        string enemyId = parts[1];

        if (!EnemyDatabase.Contains(enemyId))
        {
            AddLine("Ошибка: неизвестный id '" + enemyId + "'.");
            AddLine("Доступные id: " + string.Join(", ", availableIds));
            return;
        }

        if (EnemySpawner.Instance == null)
        {
            AddLine("Ошибка: на сцене нет EnemySpawner.");
            return;
        }

        if (EnemySpawner.Instance.baseEnemyPrefab == null)
        {
            AddLine("Ошибка: в EnemySpawner не назначен baseEnemyPrefab.");
            return;
        }

        Vector2Int cell;
        GameObject spawned = EnemySpawner.Instance.SpawnEnemyNearPlayer(enemyId, Mathf.Max(1, spawnRadius), out cell);

        if (spawned == null)
        {
            AddLine("Ошибка: рядом с игроком нет свободной клетки.");
            return;
        }

        AddLine("Создан враг '" + enemyId + "' в клетке (" + cell.x + ", " + cell.y + ").");
    }

    // =====================================================================
    // Лог
    // =====================================================================
    private void AddLine(string line)
    {
        if (string.IsNullOrEmpty(line)) return;

        _lines.Add(line);
        if (_lines.Count > MaxBufferedLines) _lines.RemoveAt(0);

        _logDirty = true;
        if (IsOpen) RefreshLog();
    }

    private void RefreshLog()
    {
        if (!_logDirty) return;
        if (_logText == null || _logRect == null) return;

        // Ждем первый проход верстки, иначе высота еще нулевая
        float height = _logRect.rect.height;
        if (height <= 1f) return;

        _logDirty = false;

        int maxVisibleLines = Mathf.Max(3, Mathf.FloorToInt(height / (fontSize * 1.25f)));
        if (_lines.Count > maxVisibleLines)
        {
            _lines.RemoveRange(0, _lines.Count - maxVisibleLines);
        }

        _logText.text = string.Join("\n", _lines);
    }

    private void RefreshInput()
    {
        if (_inputText == null) return;
        _inputText.text = "> " + _input + (_caretVisible ? "|" : " ");
    }

    // Предупреждения и ошибки Unity дублируем в консоль (обычные Debug.Log не берем,
    // иначе лог шагов персонажа засыпает всё остальное)
    private void OnUnityLog(string message, string stackTrace, LogType type)
    {
        if (type == LogType.Log) return;

        string prefix = type == LogType.Warning ? "[warn] " : "[err] ";
        string clean = message.Replace('\n', ' ').Replace('\r', ' ');
        AddLine(prefix + clean);
    }

    // =====================================================================
    // Построение UI кодом
    // =====================================================================
    private void BuildUi()
    {
        GameObject canvasObject = new GameObject("ConsoleCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.matchWidthOrHeight = 1f;

        _root = canvasObject;

        // Затемнение игры под консолью
        Image backdrop = CreateImage(canvasObject.transform, "Backdrop", new Color(0f, 0f, 0f, 0.55f));
        StretchFull(backdrop.rectTransform);

        // Панель консоли
        Image panel = CreateImage(canvasObject.transform, "Panel", new Color(0.04f, 0.05f, 0.06f, 0.94f));
        RectTransform panelRect = panel.rectTransform;
        panelRect.anchorMin = new Vector2(0.03f, 0.03f);
        panelRect.anchorMax = new Vector2(0.97f, 0.62f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Строка ввода (внизу панели)
        _inputText = CreateText(panel.transform, "Input", TextAnchor.MiddleLeft);
        RectTransform inputRect = _inputText.rectTransform;
        inputRect.anchorMin = new Vector2(0f, 0f);
        inputRect.anchorMax = new Vector2(1f, 0f);
        inputRect.pivot = new Vector2(0.5f, 0f);
        inputRect.offsetMin = new Vector2(16f, 10f);
        inputRect.offsetMax = new Vector2(-16f, 42f);
        _inputText.color = new Color(0.72f, 0.95f, 1f);
        _inputText.horizontalOverflow = HorizontalWrapMode.Overflow;
        _inputText.verticalOverflow = VerticalWrapMode.Overflow;

        // Лог (занимает всё над строкой ввода)
        _logText = CreateText(panel.transform, "Log", TextAnchor.UpperLeft);
        _logRect = _logText.rectTransform;
        _logRect.anchorMin = Vector2.zero;
        _logRect.anchorMax = Vector2.one;
        _logRect.offsetMin = new Vector2(16f, 46f);
        _logRect.offsetMax = new Vector2(-16f, -12f);

        _logText.text = string.Empty;
        _inputText.text = string.Empty;
    }

    private Image CreateImage(Transform parent, string objectName, Color color)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);

        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private Text CreateText(Transform parent, string objectName, TextAnchor alignment)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);

        Text text = textObject.GetComponent<Text>();
        text.font = LoadFont();
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = new Color(0.86f, 0.92f, 0.86f);
        text.raycastTarget = false;
        text.supportRichText = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.lineSpacing = 1.05f;
        return text;
    }

    private static Font LoadFont()
    {
        // Встроенный шрифт Unity 6 (Arial.ttf из проекта удален начиная с 2022.2)
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (font == null)
        {
            font = Font.CreateDynamicFontFromOSFont(new string[] { "Segoe UI", "Arial", "Liberation Sans" }, 18);
        }

        return font;
    }

    private static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
