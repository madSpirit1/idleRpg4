using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Панель скилов (хотбар): один ряд из 10 квадратов, клавиши 1-0.
/// Стрелки по краям листают панели: всего их 10, на первой лежат 10 скилов,
/// остальные пока пустые под будущие умения.
///
/// Данные (id, страница, иконка, тип урона, зона, эффекты) — в Resources/Skills.json.
/// Выбор скила уходит в EventBus событием SkillSelectedEvent.
/// </summary>
public class SkillPanel : MonoBehaviour
{
    public const int PagesCount = 10;

    // Геометрия панели (в единицах Canvas, reference 1600x900)
    private const float PanelWidth = 814f;
    private const float PanelHeight = 116f;
    private const float SlotSize = 62f;
    private const float SlotStep = 68f;
    private const float SlotLeft = 70f;
    private const float SlotBottom = 14f;
    private const float ArrowY = 21f;
    private const float ArrowSize = 48f;

    private static readonly Color PanelColor = new Color(0.06f, 0.07f, 0.09f, 0.93f);
    private static readonly Color SlotColor = new Color(0.13f, 0.15f, 0.18f, 1f);
    private static readonly Color EmptySlotColor = new Color(0.09f, 0.10f, 0.12f, 1f);
    private static readonly Color ArrowColor = new Color(0.16f, 0.18f, 0.22f, 1f);
    private static readonly Color ArrowHoverColor = new Color(0.27f, 0.31f, 0.38f, 1f);
    private static readonly Color IconColor = new Color(0.85f, 0.88f, 0.85f, 1f);
    private static readonly Color TextColor = new Color(0.90f, 0.92f, 0.90f, 1f);
    private static readonly Color DimTextColor = new Color(0.55f, 0.59f, 0.55f, 1f);

    private sealed class SlotView
    {
        public RectTransform Rect;
        public Image Frame;
        public Image Background;
        public Image IconImage;
        public Text Icon;
        public Text Hotkey;
        public SkillData Skill;
    }

    private static SkillPanel _instance;

    // Картинки скилов грузятся из Resources один раз и кешируются по id.
    // null в кеше значит "файла нет" — тогда квадрат покажет буквы из поля icon.
    private static readonly System.Collections.Generic.Dictionary<string, Sprite> IconCache =
        new System.Collections.Generic.Dictionary<string, Sprite>();

    private RectTransform _panelRect;
    private RectTransform _leftRect;
    private RectTransform _rightRect;
    private Image _leftImage;
    private Image _rightImage;
    private Text _pageText;
    private Text _skillLabel;

    private readonly SlotView[] _slots = new SlotView[SkillDatabase.SlotsPerPage];
    private readonly int[] _selectedPerPage = new int[PagesCount];

    private int _page = 1;
    private int _hoveredSlot = -1;
    private int _lastHoveredSlot = -1;

    // =====================================================================
    // Автосоздание при старте сцены, как у консоли
    // =====================================================================
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindAnyObjectByType<SkillPanel>() != null) return;

        GameObject panelObject = new GameObject("~SkillPanel");
        panelObject.AddComponent<SkillPanel>();
    }

    private void Awake()
    {
        _instance = this;

        for (int i = 0; i < _selectedPerPage.Length; i++) _selectedPerPage[i] = -1;

        BuildUi();
    }

    private void Start()
    {
        RefreshSlots();
        _lastHoveredSlot = _hoveredSlot;
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    /// <summary>
    /// Наведён ли курсор на панель. PlayerInput использует это, чтобы клик по
    /// квадрату скила не отправлял персонажа в путь по клетке под панелью.
    /// </summary>
    public static bool IsPointerOverUI()
    {
        if (_instance == null || _instance._panelRect == null) return false;
        return IsMouseOver(_instance._panelRect);
    }

    // =====================================================================
    // Ввод
    // =====================================================================
    private void Update()
    {
        if (DebugConsole.IsOpen) return;

        UpdateHover();
        if (_hoveredSlot != _lastHoveredSlot)
        {
            _lastHoveredSlot = _hoveredSlot;
            RefreshSlots();
        }

        bool overLeft = IsMouseOver(_leftRect);
        bool overRight = IsMouseOver(_rightRect);

        _leftImage.color = overLeft ? ArrowHoverColor : ArrowColor;
        _rightImage.color = overRight ? ArrowHoverColor : ArrowColor;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.leftArrowKey.wasPressedThisFrame) ChangePage(-1);
            if (keyboard.rightArrowKey.wasPressedThisFrame) ChangePage(1);

            int digit = ReadDigit(keyboard);
            if (digit >= 0) SelectSlot(digit);
        }

        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            if (overLeft) ChangePage(-1);
            else if (overRight) ChangePage(1);
            else if (_hoveredSlot >= 0) SelectSlot(_hoveredSlot);
        }
    }

    private void UpdateHover()
    {
        _hoveredSlot = -1;

        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i].Skill == null) continue;

            if (IsMouseOver(_slots[i].Rect))
            {
                _hoveredSlot = i;
                break;
            }
        }
    }

    // Клавиши 1..9 и 0 — по номеру слота
    private static int ReadDigit(Keyboard keyboard)
    {
        if (keyboard.digit1Key.wasPressedThisFrame) return 0;
        if (keyboard.digit2Key.wasPressedThisFrame) return 1;
        if (keyboard.digit3Key.wasPressedThisFrame) return 2;
        if (keyboard.digit4Key.wasPressedThisFrame) return 3;
        if (keyboard.digit5Key.wasPressedThisFrame) return 4;
        if (keyboard.digit6Key.wasPressedThisFrame) return 5;
        if (keyboard.digit7Key.wasPressedThisFrame) return 6;
        if (keyboard.digit8Key.wasPressedThisFrame) return 7;
        if (keyboard.digit9Key.wasPressedThisFrame) return 8;
        if (keyboard.digit0Key.wasPressedThisFrame) return 9;

        return -1;
    }

    private static bool IsMouseOver(RectTransform rect)
    {
        if (rect == null) return false;

        Mouse mouse = Mouse.current;
        if (mouse == null) return false;

        return RectTransformUtility.RectangleContainsScreenPoint(rect, mouse.position.ReadValue(), null);
    }

    // =====================================================================
    // Страницы и выбор
    // =====================================================================
    private void ChangePage(int delta)
    {
        _page = ((_page - 1 + delta) % PagesCount + PagesCount) % PagesCount + 1;

        _hoveredSlot = -1;
        _lastHoveredSlot = -2;   // следующий Update пересчитает подсветку

        RefreshSlots();
    }

    private void SelectSlot(int index)
    {
        if (index < 0 || index >= _slots.Length) return;

        SkillData skill = _slots[index].Skill;
        if (skill == null) return;

        _selectedPerPage[_page - 1] = index;
        RefreshSlots();

        EventBus.Publish(new SkillSelectedEvent { SkillId = skill.id, Slot = index, Page = _page });

        Debug.Log("Скил выбран: " + skill.name + " [клавиша " + HotkeyLabel(index) + ", панель " + _page + "]");
    }

    private static string HotkeyLabel(int index)
    {
        return index == 9 ? "0" : (index + 1).ToString();
    }

    // =====================================================================
    // Отрисовка состояния
    // =====================================================================
    private void RefreshSlots()
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            SlotView slot = _slots[i];
            slot.Skill = SkillDatabase.GetSlot(_page, i);

            bool hasSkill = slot.Skill != null;
            bool selected = hasSkill && _selectedPerPage[_page - 1] == i;
            bool hovered = hasSkill && _hoveredSlot == i;

            Color typeColor = hasSkill ? DamageTypeColor(slot.Skill.damageType) : DimTextColor;

            slot.Frame.color = selected
                ? new Color(typeColor.r, typeColor.g, typeColor.b, 0.95f)
                : new Color(typeColor.r, typeColor.g, typeColor.b, 0f);

            Color background = hasSkill ? Color.Lerp(SlotColor, typeColor, 0.22f) : EmptySlotColor;
            if (hovered) background = Color.Lerp(background, Color.white, 0.10f);
            slot.Background.color = background;

            Sprite sprite = hasSkill ? GetSkillSprite(slot.Skill.id) : null;

            slot.IconImage.enabled = sprite != null;
            slot.IconImage.sprite = sprite;
            slot.IconImage.color = selected ? Color.white : new Color(1f, 1f, 1f, 0.88f);

            // Фолбэк: если картинки для скила нет — показываем буквы из поля icon
            slot.Icon.text = hasSkill && sprite == null ? SkillIcon(slot.Skill) : string.Empty;
            slot.Icon.color = selected ? Color.white : new Color(0.95f, 0.96f, 0.95f, 0.95f);

            slot.Hotkey.text = HotkeyLabel(i);
            slot.Hotkey.color = selected ? Color.white : DimTextColor;
        }

        RefreshHeader();
    }

    private void RefreshHeader()
    {
        _pageText.text = "ПАНЕЛЬ " + _page + " / " + PagesCount;

        SkillData skill = null;

        if (_hoveredSlot >= 0 && _hoveredSlot < _slots.Length) skill = _slots[_hoveredSlot].Skill;

        if (skill == null)
        {
            int selectedIndex = _selectedPerPage[_page - 1];
            if (selectedIndex >= 0 && selectedIndex < _slots.Length) skill = _slots[selectedIndex].Skill;
        }

        if (skill == null)
        {
            _skillLabel.text = "—";
            _skillLabel.color = DimTextColor;
            return;
        }

        _skillLabel.text = skill.name + " · " + DamageTypeName(skill.damageType);
        _skillLabel.color = DamageTypeColor(skill.damageType);
    }

    private static string SkillIcon(SkillData skill)
    {
        if (!string.IsNullOrEmpty(skill.icon)) return skill.icon;
        if (string.IsNullOrEmpty(skill.name)) return "?";

        return skill.name.Substring(0, 1).ToUpperInvariant();
    }

    /// <summary>
    /// Картинка скила из Resources/SkillIcons/&lt;id&gt;.png.
    /// Текстура грузится как обычная (Texture2D), поэтому не нужны никакие
    /// настройки импорта — спрайт собирается в рантайме.
    /// </summary>
    private static Sprite GetSkillSprite(string skillId)
    {
        if (string.IsNullOrEmpty(skillId)) return null;

        Sprite cached;
        if (IconCache.TryGetValue(skillId, out cached)) return cached;

        Texture2D texture = Resources.Load<Texture2D>("SkillIcons/" + skillId);

        Sprite sprite = null;
        if (texture != null)
        {
            sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            sprite.name = "SkillIcon_" + skillId;
        }

        IconCache[skillId] = sprite;
        return sprite;
    }

    // =====================================================================
    // Типы урона
    // =====================================================================
    private static string DamageTypeName(string damageType)
    {
        switch ((damageType ?? string.Empty).ToLowerInvariant())
        {
            case "physical": return "Физический";
            case "fire": return "Огонь";
            case "ice": return "Лёд";
            case "poison": return "Яд";
            case "holy": return "Свет";
            case "dark": return "Тьма";
            default: return string.IsNullOrEmpty(damageType) ? "—" : damageType;
        }
    }

    private static Color DamageTypeColor(string damageType)
    {
        switch ((damageType ?? string.Empty).ToLowerInvariant())
        {
            case "physical": return new Color(0.78f, 0.78f, 0.74f);
            case "fire": return new Color(0.95f, 0.46f, 0.22f);
            case "ice": return new Color(0.45f, 0.76f, 0.95f);
            case "poison": return new Color(0.56f, 0.80f, 0.36f);
            case "holy": return new Color(0.95f, 0.85f, 0.46f);
            case "dark": return new Color(0.72f, 0.52f, 0.92f);
            default: return DimTextColor;
        }
    }

    // =====================================================================
    // Построение UI
    // =====================================================================
    private void BuildUi()
    {
        GameObject canvasObject = new GameObject("SkillPanelCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.matchWidthOrHeight = 1f;

        Image panel = CreateImage(canvasObject.transform, "Panel", PanelColor, UiSprites.RoundedRect, Image.Type.Sliced);
        _panelRect = panel.rectTransform;
        _panelRect.anchorMin = new Vector2(0.5f, 0f);
        _panelRect.anchorMax = new Vector2(0.5f, 0f);
        _panelRect.pivot = new Vector2(0.5f, 0f);
        _panelRect.anchoredPosition = new Vector2(0f, 20f);
        _panelRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);

        // Стрелки
        _leftImage = CreateImage(_panelRect, "ArrowLeft", ArrowColor, UiSprites.RoundedRect, Image.Type.Sliced);
        _leftRect = _leftImage.rectTransform;
        PlaceAt(_leftRect, 14f, ArrowY, ArrowSize, ArrowSize);

        Image leftIcon = CreateImage(_leftRect, "Icon", IconColor, UiSprites.ArrowLeft, Image.Type.Simple);
        Stretch(leftIcon.rectTransform, 15f);

        _rightImage = CreateImage(_panelRect, "ArrowRight", ArrowColor, UiSprites.RoundedRect, Image.Type.Sliced);
        _rightRect = _rightImage.rectTransform;
        PlaceAt(_rightRect, 752f, ArrowY, ArrowSize, ArrowSize);

        Image rightIcon = CreateImage(_rightRect, "Icon", IconColor, UiSprites.ArrowRight, Image.Type.Simple);
        Stretch(rightIcon.rectTransform, 15f);

        // Заголовок: номер панели слева, выбранный скил справа
        _pageText = CreateText(_panelRect, "PageLabel", 13, TextAnchor.MiddleLeft);
        _pageText.color = DimTextColor;
        SetRect(_pageText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(14f, -34f), new Vector2(414f, -12f));

        _skillLabel = CreateText(_panelRect, "SkillLabel", 13, TextAnchor.MiddleRight);
        SetRect(_skillLabel.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-414f, -34f), new Vector2(-14f, -12f));

        // Квадраты скилов
        for (int i = 0; i < _slots.Length; i++)
        {
            float x = SlotLeft + SlotStep * i;

            SlotView slot = new SlotView();

            // Рамка выделения — создается раньше фона, чтобы рисоваться под ним
            slot.Frame = CreateImage(_panelRect, "SlotFrame", new Color(1f, 1f, 1f, 0f), UiSprites.RoundedRect, Image.Type.Sliced);
            PlaceAt(slot.Frame.rectTransform, x - 2f, SlotBottom - 2f, SlotSize + 4f, SlotSize + 4f);

            slot.Background = CreateImage(_panelRect, "Slot", SlotColor, UiSprites.RoundedRect, Image.Type.Sliced);
            slot.Rect = slot.Background.rectTransform;
            PlaceAt(slot.Rect, x, SlotBottom, SlotSize, SlotSize);

            slot.IconImage = CreateImage(slot.Rect, "IconImage", Color.white, null, Image.Type.Simple);
            slot.IconImage.enabled = false;
            slot.IconImage.preserveAspect = true;
            RectTransform iconRect = slot.IconImage.rectTransform;
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(48f, 48f);
            iconRect.anchoredPosition = new Vector2(0f, 2f);

            // Текстовая заглушка, если картинки для скила не нашлось
            slot.Icon = CreateText(slot.Rect, "Icon", 22, TextAnchor.MiddleCenter);
            Stretch(slot.Icon.rectTransform, 4f);

            slot.Hotkey = CreateText(slot.Rect, "Hotkey", 10, TextAnchor.LowerRight);
            SetRect(slot.Hotkey.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-26f, 3f), new Vector2(-5f, 16f));

            _slots[i] = slot;
        }
    }

    private static void PlaceAt(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
    }

    private static void Stretch(RectTransform rect, float padding)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(padding, padding);
        rect.offsetMax = new Vector2(-padding, -padding);
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static Image CreateImage(Transform parent, string objectName, Color color, Sprite sprite, Image.Type type)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);

        Image image = imageObject.GetComponent<Image>();
        image.sprite = sprite;
        image.type = type;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static Text CreateText(Transform parent, string objectName, int fontSize, TextAnchor alignment)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);

        Text text = textObject.GetComponent<Text>();
        text.font = UiSprites.DefaultFont;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = TextColor;
        text.raycastTarget = false;
        text.supportRichText = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.lineSpacing = 1.1f;
        return text;
    }
}
