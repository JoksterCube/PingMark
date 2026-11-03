using System;
using UnityEngine;

namespace JoksterCube.PingMark.Domain;

// The Perception icon, for both its status and its slot next to the Guardian Power: an eye watching from inside a
// carved stone ring, the runes cut around it faintly glowing, like a runestone come awake. Painted here when first
// needed, in the game's soft painted style, so the mod ships no image files.
internal static class PerceptionIcon
{
    private const int Size = 64;
    private const float Center = Size / 2f;

    private static readonly Color Ember = new(1f, 0.55f, 0.16f);
    // The stone ring, between these distances from the middle.
    private const float RingInner = 21.5f;
    private const float RingOuter = 29.5f;
    private const float RingMiddle = (RingInner + RingOuter) / 2f;
    private const float RingHalf = (RingOuter - RingInner) / 2f;
    // The eye: an almond 15 wide and 8.5 tall either side of its middle, made of two circles that overlap.
    private const float EyeHalfWidth = 15f;
    private const float EyeHalfHeight = 8.5f;
    private const float EyeOffset = (EyeHalfWidth * EyeHalfWidth - EyeHalfHeight * EyeHalfHeight) / (2f * EyeHalfHeight);
    private const float EyeRadius = EyeOffset + EyeHalfHeight;

    // Runes cut into the ring, each as strokes from (x, y) to (z, w): x across the ring, y outward, from -3 to 3.
    // Kaunan, Algiz, Ansuz, Isa, Sowilo, Tiwaz, Isa, Algiz.
    private static readonly Vector4[][] Runes =
    [
        [new(1f, 3f, -1.2f, 0f), new(-1.2f, 0f, 1f, -3f)],
        [new(0f, -3f, 0f, 3f), new(0f, 0.6f, -1.7f, 3f), new(0f, 0.6f, 1.7f, 3f)],
        [new(-1f, -3f, -1f, 3f), new(-1f, 3f, 1.3f, 1.5f), new(-1f, 1f, 1.3f, -0.5f)],
        [new(0f, -3f, 0f, 3f)],
        [new(-1.3f, 3f, 1f, 1f), new(1f, 1f, -1f, -1f), new(-1f, -1f, 1.3f, -3f)],
        [new(0f, -3f, 0f, 3f), new(0f, 3f, -1.7f, 1.2f), new(0f, 3f, 1.7f, 1.2f)],
        [new(0f, -3f, 0f, 3f)],
        [new(0f, -3f, 0f, 3f), new(0f, 0.6f, -1.7f, 3f), new(0f, 0.6f, 1.7f, 3f)],
    ];

    private static Sprite? _sprite;

    internal static Sprite Get() => _sprite ? _sprite! : _sprite = ToSprite(Render(), "PingMark_Perception");

    // The icon's 64 x 64 colors, straight alpha, row 0 at the top (previews).
    internal static Color[] Render()
    {
        Color[] canvas = new Color[Size * Size];
        Glow(canvas, Center, Center, 24f, Ember, 0.35f);
        StoneRing(canvas);
        Eye(canvas);
        return canvas;
    }

    private static float RingShape(float x, float y)
    {
        float r = Mathf.Sqrt((x - Center) * (x - Center) + (y - Center) * (y - Center));
        return Mathf.Abs(r - RingMiddle) - RingHalf;
    }

    // How far (x, y) is from the nearest rune's strokes, the runes spaced evenly round the ring.
    private static float RuneShape(float x, float y)
    {
        float dx = x - Center, dy = y - Center;
        float d = float.MaxValue;
        for (int k = 0; k < Runes.Length; k++)
        {
            float a = (k * 360f / Runes.Length - 90f + 22.5f) * Mathf.Deg2Rad;
            float nx = Mathf.Cos(a), ny = Mathf.Sin(a);
            // Across the ring (clockwise) and outward, from the rune's middle.
            float u = -dx * ny + dy * nx, v = dx * nx + dy * ny - RingMiddle;
            if (Mathf.Abs(u) > 6f || Mathf.Abs(v) > 6f) continue;
            foreach (Vector4 s in Runes[k]) d = Mathf.Min(d, Capsule(u, v, s.x * 1.1f, s.y * 1.1f, s.z * 1.1f, s.w * 1.1f, 0.7f));
        }
        return d;
    }

    // A ring of weathered grey stone over a soft shadow, lit from the top left, its runes cut in and glowing like embers.
    private static void StoneRing(Color[] canvas)
    {
        float[] alpha = new float[Size * Size];
        for (int py = 0; py < Size; py++)
            for (int px = 0; px < Size; px++)
                alpha[py * Size + px] = Mathf.Clamp01(0.5f - RingShape(px + 0.5f, py + 0.5f));
        float[] shadow = Blur(alpha, 2);
        for (int i = Size; i < shadow.Length; i++) Blend(canvas, i, new Color(0f, 0f, 0f, shadow[i - Size] * 0.6f));

        Paint(canvas, (x, y) =>
        {
            float ring = RingShape(x, y);
            if (ring > 1f) return Color.clear;
            float dx = x - Center, dy = y - Center;
            float r = Mathf.Max(Mathf.Sqrt(dx * dx + dy * dy), 0.001f);
            // Rounded across its width: its outer side faces out, its inner side faces in.
            float across = Mathf.Clamp((r - RingMiddle) / RingHalf, -1f, 1f);
            float facing = (-dx - dy) / (r * 1.4142f);
            float light = 0.4f + 0.26f * across * facing + 0.1f * (1f - across * across);
            light *= 0.8f + 0.2f * Noise(x, y) + 0.2f * Noise(x * 0.25f, y * 0.25f);
            // Darker toward its edges, so it reads as one rounded band.
            light *= Mathf.Lerp(0.7f, 1f, Mathf.Clamp01(-ring / 1.8f));
            Color stone = new(light * 0.92f, light * 0.95f, light, Mathf.Clamp01(0.5f - ring));

            float rune = RuneShape(x, y);
            float glow = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Max(rune, 0f) / 3.2f), 2f) * 0.8f;
            Color halo = new(Ember.r, Ember.g, Ember.b, glow * stone.a);
            Color cut = new(1f, 0.78f, 0.42f, Mathf.Clamp01(0.5f - rune) * stone.a);
            return Over(Over(stone, halo), cut);
        });
    }

    private static float EyeShape(float x, float y) =>
        Mathf.Max(Circle(x, y, Center, Center + EyeOffset, EyeRadius), Circle(x, y, Center, Center - EyeOffset, EyeRadius));

    // The eye over a soft shadow: bone white, an amber iris bright at its heart, a dark pupil, a glint and a dark lid.
    private static void Eye(Color[] canvas)
    {
        float[] alpha = new float[Size * Size];
        for (int py = 0; py < Size; py++)
            for (int px = 0; px < Size; px++)
                alpha[py * Size + px] = Mathf.Clamp01(0.5f - EyeShape(px + 0.5f, py + 0.5f) + 0.6f);
        float[] shadow = Blur(alpha, 1);
        for (int i = Size; i < shadow.Length; i++) Blend(canvas, i, new Color(0f, 0f, 0f, shadow[i - Size] * 0.5f));

        Paint(canvas, (x, y) =>
        {
            float eye = EyeShape(x, y);
            if (eye > 2f) return Color.clear;
            float inside = Mathf.Clamp01(0.5f - eye);
            // The white darkens toward the corners and under the lid.
            float shade = Mathf.Lerp(0.62f, 0.92f, Mathf.Clamp01(-eye / 6f)) * (0.95f + 0.1f * Noise(x, y));
            Color white = new(0.92f * shade, 0.88f * shade, 0.76f * shade, inside);
            float iris = Circle(x, y, Center, Center, 6.8f);
            float t = Mathf.Clamp01((iris + 6.8f) / 6.8f);
            Color irisColor = new(Mathf.Lerp(1f, 0.78f, t), Mathf.Lerp(0.85f, 0.36f, t), Mathf.Lerp(0.45f, 0.06f, t),
                Mathf.Clamp01(0.5f - iris) * inside);
            Color irisRim = new(0.3f, 0.13f, 0.03f, Mathf.Clamp01(0.5f - (Mathf.Abs(iris) - 0.5f)) * inside);
            Color pupil = new(0.06f, 0.04f, 0.03f, Mathf.Clamp01(0.5f - Circle(x, y, Center, Center, 2.8f)));
            Color glint = new(1f, 0.97f, 0.88f, Mathf.Clamp01(0.5f - Circle(x, y, Center - 2.3f, Center - 2.3f, 1.2f)));
            Color lid = new(0.16f, 0.1f, 0.06f, Mathf.Clamp01(0.5f - (Mathf.Abs(eye) - 0.95f)));
            return Over(Over(Over(Over(Over(white, irisColor), irisRim), pupil), glint), lid);
        });
    }

    // ---- Drawing ----

    private static void Glow(Color[] canvas, float cx, float cy, float radius, Color color, float strength) =>
        Paint(canvas, (x, y) =>
        {
            float r = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
            return new Color(color.r, color.g, color.b, Mathf.Pow(Mathf.Clamp01(1f - r / radius), 2f) * strength);
        });

    private static float Circle(float x, float y, float cx, float cy, float r) =>
        Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r;

    private static float Capsule(float x, float y, float ax, float ay, float bx, float by, float r)
    {
        float dx = bx - ax, dy = by - ay;
        float length = dx * dx + dy * dy;
        float t = length > 0f ? Mathf.Clamp01(((x - ax) * dx + (y - ay) * dy) / length) : 0f;
        float px = ax + t * dx - x, py = ay + t * dy - y;
        return Mathf.Sqrt(px * px + py * py) - r;
    }

    // A steady speckle from 0 to 1 for each pixel, a little smoothed: weathering on stone.
    private static float Noise(float x, float y)
    {
        int px = Mathf.FloorToInt(x), py = Mathf.FloorToInt(y);
        return (Hash(px, py) * 2f + Hash(px + 1, py) + Hash(px, py + 1) + Hash(px - 1, py) + Hash(px, py - 1)) / 6f;
    }

    private static float Hash(int x, int y)
    {
        uint h = unchecked((uint)(x * 374761393 + y * 668265263));
        h = unchecked((h ^ (h >> 13)) * 1274126177u);
        return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
    }

    private static void Paint(Color[] canvas, Func<float, float, Color> shade)
    {
        for (int py = 0; py < Size; py++)
            for (int px = 0; px < Size; px++)
                Blend(canvas, py * Size + px, shade(px + 0.5f, py + 0.5f));
    }

    private static void Blend(Color[] canvas, int index, Color color) => canvas[index] = Over(canvas[index], color);

    private static Color Over(Color below, Color above)
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
    private static float[] Blur(float[] values, int radius)
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

    private static Sprite ToSprite(Color[] pixels, string name)
    {
        Color[] flipped = new Color[pixels.Length];
        // Textures start at the bottom row.
        for (int y = 0; y < Size; y++) Array.Copy(pixels, y * Size, flipped, (Size - 1 - y) * Size, Size);
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
