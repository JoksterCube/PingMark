using System;
using UnityEngine;

namespace JoksterCube.PingMark.Domain;

// What the mod's painted icons (buttons and statuses) are drawn with, in the same style as TempestRite's: soft glows,
// shadows and glowing lines over shapes given as distance functions, so the mod ships no image files.
internal static class IconPaint
{
    internal const int Size = 64;

    internal static void Glow(IconCanvas canvas, float cx, float cy, float radius, Color color, float strength) =>
        canvas.Paint((x, y) =>
        {
            float r = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
            return new Color(color.r, color.g, color.b, Mathf.Pow(Mathf.Clamp01(1f - r / radius), 2f) * strength);
        });

    // A soft shadow of the shape, a pixel down.
    internal static void Shadow(IconCanvas canvas, Func<float, float, float> shape, float strength)
    {
        float[] alpha = new float[Size * Size];
        for (int py = 0; py < Size; py++)
            for (int px = 0; px < Size; px++)
                alpha[py * Size + px] = Mathf.Clamp01(0.5f - shape(px + 0.5f, py + 0.5f));
        float[] shadow = IconCanvas.Blur(alpha, 2);
        for (int i = Size; i < shadow.Length; i++) canvas.Blend(i, new Color(0f, 0f, 0f, shadow[i - Size] * strength));
    }

    // A line (distance function) in core, with a soft glow out to width pixels around it.
    internal static void GlowLine(IconCanvas canvas, Func<float, float, float> line, Color glow, Color core, float width, float strength) =>
        canvas.Paint((x, y) =>
        {
            float d = line(x, y);
            float halo = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Max(d, 0f) / width), 2f) * strength;
            return IconCanvas.Over(new Color(glow.r, glow.g, glow.b, halo), new Color(core.r, core.g, core.b, Mathf.Clamp01(0.5f - d)));
        });

    // Fills the shape, colored by where the pixel is and how deep inside the shape it is.
    internal static void Fill(IconCanvas canvas, Func<float, float, float> shape, Func<float, float, float, Color> color) =>
        canvas.Paint((x, y) =>
        {
            float d = shape(x, y);
            if (d > 0.5f) return Color.clear;
            Color c = color(x, y, -d);
            c.a *= Mathf.Clamp01(0.5f - d);
            return c;
        });

    // ---- Distance functions (negative inside), in pixels with y going down ----

    internal static float Circle(float x, float y, float cx, float cy, float r) =>
        Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r;

    // Close enough for the soft edges these are drawn with.
    internal static float Ellipse(float x, float y, float cx, float cy, float rx, float ry)
    {
        float nx = (x - cx) / rx, ny = (y - cy) / ry;
        return (Mathf.Sqrt(nx * nx + ny * ny) - 1f) * Mathf.Min(rx, ry);
    }

    internal static float Box(float x, float y, float left, float top, float right, float bottom) =>
        Mathf.Max(Mathf.Max(left - x, x - right), Mathf.Max(top - y, y - bottom));

    internal static float Capsule(float x, float y, float ax, float ay, float bx, float by, float r)
    {
        float dx = bx - ax, dy = by - ay;
        float length = dx * dx + dy * dy;
        float t = length > 0f ? Mathf.Clamp01(((x - ax) * dx + (y - ay) * dy) / length) : 0f;
        float px = ax + t * dx - x, py = ay + t * dy - y;
        return Mathf.Sqrt(px * px + py * py) - r;
    }
}

// A 64 x 64 picture, straight alpha, row 0 at the top.
internal sealed class IconCanvas
{
    private const int Size = IconPaint.Size;
    private readonly Color[] _pixels = new Color[Size * Size];

    internal Color[] Colors => _pixels;

    internal void Blend(int index, Color color) => _pixels[index] = Over(_pixels[index], color);

    internal void Paint(Func<float, float, Color> shade)
    {
        for (int py = 0; py < Size; py++)
            for (int px = 0; px < Size; px++)
                Blend(py * Size + px, shade(px + 0.5f, py + 0.5f));
    }

    internal static Color Over(Color below, Color above)
    {
        float a = above.a + below.a * (1f - above.a);
        if (a <= 0f) return Color.clear;
        float k = below.a * (1f - above.a);
        return new Color(
            (above.r * above.a + below.r * k) / a,
            (above.g * above.a + below.g * k) / a,
            (above.b * above.a + below.b * k) / a,
            a);
    }

    // Two box blurs: close enough to a soft shadow.
    internal static float[] Blur(float[] values, int radius)
    {
        float[] result = values;
        for (int pass = 0; pass < 2; pass++)
        {
            float[] horizontal = new float[result.Length];
            float[] vertical = new float[result.Length];
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    float sum = 0f;
                    for (int k = -radius; k <= radius; k++) sum += result[y * Size + Mathf.Clamp(x + k, 0, Size - 1)];
                    horizontal[y * Size + x] = sum / (2 * radius + 1);
                }
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    float sum = 0f;
                    for (int k = -radius; k <= radius; k++) sum += horizontal[Mathf.Clamp(y + k, 0, Size - 1) * Size + x];
                    vertical[y * Size + x] = sum / (2 * radius + 1);
                }
            result = vertical;
        }
        return result;
    }

    internal Sprite ToSprite(string name)
    {
        Color[] flipped = new Color[_pixels.Length];
        // Textures start at the bottom row.
        for (int y = 0; y < Size; y++) Array.Copy(_pixels, y * Size, flipped, (Size - 1 - y) * Size, Size);
        Texture2D texture = new(Size, Size, TextureFormat.RGBA32, false)
        {
            name = name,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        texture.SetPixels(flipped);
        texture.Apply(false, true);
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f), 100f);
        sprite.name = name;
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }
}
