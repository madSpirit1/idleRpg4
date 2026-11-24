using UnityEngine;

/// <summary>
/// Процедурные спрайты для интерфейса: скруглённые плашки и треугольные стрелки.
///
/// В проекте нет ни одной UI-текстуры, а тащить ассеты ради двух кнопок не хочется —
/// поэтому спрайты рисуются кодом один раз и кешируются. Это заодно избавляет
/// от проблем со шрифтом: символы "◀" и "▶" есть не в каждом шрифте.
/// </summary>
public static class UiSprites
{
    private static Sprite _roundedRect;
    private static Sprite _arrowLeft;
    private static Sprite _arrowRight;
    private static Font _defaultFont;

    /// <summary>Встроенный шрифт Unity 6 (Arial.ttf удалён начиная с 2022.2).</summary>
    public static Font DefaultFont
    {
        get
        {
            if (_defaultFont != null) return _defaultFont;

            _defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_defaultFont == null)
            {
                _defaultFont = Font.CreateDynamicFontFromOSFont(new string[] { "Segoe UI", "Arial", "Liberation Sans" }, 18);
            }
            return _defaultFont;
        }
    }

    /// <summary>Скруглённый прямоугольник с девяткой (можно растягивать без порчи углов).</summary>
    public static Sprite RoundedRect
    {
        get
        {
            if (_roundedRect == null) _roundedRect = CreateRoundedRect(64, 18, 20);
            return _roundedRect;
        }
    }

    public static Sprite ArrowLeft
    {
        get
        {
            if (_arrowLeft == null) _arrowLeft = CreateTriangle(false);
            return _arrowLeft;
        }
    }

    public static Sprite ArrowRight
    {
        get
        {
            if (_arrowRight == null) _arrowRight = CreateTriangle(true);
            return _arrowRight;
        }
    }

    private static Sprite CreateRoundedRect(int size, int radius, int border)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color32[] pixels = new Color32[size * size];

        float half = size * 0.5f;
        float straight = half - radius;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = x + 0.5f - half;
                float py = y + 0.5f - half;

                // Расстояние до скруглённого прямоугольника
                float dx = Mathf.Max(Mathf.Abs(px) - straight, 0f);
                float dy = Mathf.Max(Mathf.Abs(py) - straight, 0f);
                float distance = Mathf.Sqrt(dx * dx + dy * dy) - radius;

                // Сглаживание в один пиксель
                float alpha = Mathf.Clamp01(0.5f - distance);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();

        Vector4 borderVector = new Vector4(border, border, border, border);
        return Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            borderVector);
    }

    private static Sprite CreateTriangle(bool pointRight)
    {
        const int size = 64;

        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        Vector2 a;
        Vector2 b;
        Vector2 c;

        if (pointRight)
        {
            a = new Vector2(12f, 8f);
            b = new Vector2(12f, size - 8f);
            c = new Vector2(size - 10f, size * 0.5f);
        }
        else
        {
            a = new Vector2(size - 12f, 8f);
            b = new Vector2(size - 12f, size - 8f);
            c = new Vector2(10f, size * 0.5f);
        }

        Color32[] pixels = new Color32[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 point = new Vector2(x + 0.5f, y + 0.5f);
                bool inside = IsPointInTriangle(point, a, b, c);
                pixels[y * size + x] = new Color32(255, 255, 255, inside ? (byte)255 : (byte)0);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static bool IsPointInTriangle(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = EdgeSign(point, a, b);
        float d2 = EdgeSign(point, b, c);
        float d3 = EdgeSign(point, c, a);

        bool hasNegative = d1 < 0f || d2 < 0f || d3 < 0f;
        bool hasPositive = d1 > 0f || d2 > 0f || d3 > 0f;

        return !(hasNegative && hasPositive);
    }

    private static float EdgeSign(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
    }
}
