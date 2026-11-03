using System;
using UnityEngine;
using static JoksterCube.PingMark.Domain.IconPaint;

namespace JoksterCube.PingMark.Domain;

// The Fishsight slot's icon, in the style of the flap buttons (one glowing object over a soft shadow): a wooden
// tag hanging from its cord, a fish carved into it, the fish's outline glowing. Painted when first needed, so the mod
// ships no image files. The slot draws a red cross over it while Outline Hooked Fish is off.
internal static class FishTagIcon
{
    internal enum Variant { Aqua, Amber }

    // Tilted so it hangs from the cord at its top left.
    private const float Tilt = 16f * Mathf.Deg2Rad;
    private static readonly float Cos = Mathf.Cos(Tilt), Sin = Mathf.Sin(Tilt);

    private static Sprite? _sprite;

    internal static Sprite Get() => _sprite ? _sprite! : _sprite = Picture(Variant.Aqua).ToSprite("PingMark_FishTag");

    // The icon's 64 x 64 colors, straight alpha, row 0 at the top (previews).
    internal static Color[] Render(Variant variant = Variant.Aqua) => Picture(variant).Colors;

    private static IconCanvas Picture(Variant variant)
    {
        (Color glow, Color core) = variant == Variant.Amber
            ? (new Color(1f, 0.5f, 0.12f), new Color(1f, 0.85f, 0.5f))
            : (new Color(0.3f, 0.85f, 0.95f), new Color(0.85f, 1f, 1f));

        IconCanvas canvas = new();
        Glow(canvas, 34f, 33f, 31f, glow, 0.4f);
        Shadow(canvas, (x, y) => Mathf.Min(Tag(x, y), Cord(x, y)), 0.6f);
        PaintCord(canvas);
        PaintTag(canvas);
        PaintGrommet(canvas);
        PaintFish(canvas, glow, core);
        return canvas;
    }

    // ---- Shapes, drawn untilted and turned by Tilt around the middle ----

    private static void Untilt(float x, float y, out float lx, out float ly)
    {
        float dx = x - 32f, dy = y - 32f;
        lx = 32f + dx * Cos + dy * Sin;
        ly = 32f - dx * Sin + dy * Cos;
    }

    private static float Tilted(float x, float y, Func<float, float, float> shape)
    {
        Untilt(x, y, out float lx, out float ly);
        return shape(lx, ly);
    }

    // A luggage tag: a board with rounded corners, its left end cut to a blunt point, a hole punched through it.
    private static float Board(float x, float y)
    {
        float box = Box(x, y, 16f, 23f, 54f, 45f) - 3f;
        float topCut = (44f - x - y) / 1.41421f;
        float bottomCut = (y - x - 24f) / 1.41421f;
        return Mathf.Max(box, Mathf.Max(topCut, bottomCut));
    }

    private const float HoleX = 20.5f, HoleY = 34f, HoleRadius = 2.4f;

    private static float Tag(float x, float y) =>
        Tilted(x, y, (lx, ly) => Mathf.Max(Board(lx, ly), -Circle(lx, ly, HoleX, HoleY, HoleRadius)));

    // The cord runs out of the hole up to the top left corner.
    private static float Cord(float x, float y) =>
        Tilted(x, y, (lx, ly) => Mathf.Min(Capsule(lx, ly, HoleX, HoleY, 12.5f, 26f, 1.2f),
            Capsule(lx, ly, 12.5f, 26f, 9f, 13f, 1.2f)));

    // A fish swimming right: an oval body, a forked tail and a fin on its back.
    private static float FishShape(float x, float y) => Tilted(x, y, (lx, ly) =>
    {
        float body = Ellipse(lx, ly, 41f, 34f, 9.5f, 5.4f);
        float tail = Triangle(lx, ly, 32.5f, 34f, 26f, 28f, 26f, 40f);
        float fin = Triangle(lx, ly, 37f, 29.5f, 44f, 29.5f, 38.5f, 25.5f);
        return Mathf.Min(body, Mathf.Min(tail, fin));
    });

    // Close enough for the soft edges these are drawn with: the furthest of the three sides.
    private static float Triangle(float x, float y, float ax, float ay, float bx, float by, float cx, float cy)
    {
        float turn = Mathf.Sign((bx - ax) * (cy - ay) - (by - ay) * (cx - ax));
        return Mathf.Max(Side(x, y, ax, ay, bx, by, turn), Mathf.Max(Side(x, y, bx, by, cx, cy, turn), Side(x, y, cx, cy, ax, ay, turn)));
    }

    private static float Side(float x, float y, float ax, float ay, float bx, float by, float turn)
    {
        float dx = bx - ax, dy = by - ay, length = Mathf.Sqrt(dx * dx + dy * dy);
        return -turn * (dx * (y - ay) - dy * (x - ax)) / length;
    }

    // ---- Painting ----

    // Twine: pale tan, darker along its edges, with a twist every few pixels.
    private static void PaintCord(IconCanvas canvas) => canvas.Paint((x, y) =>
    {
        float d = Cord(x, y);
        if (d > 0.5f) return Color.clear;
        float twist = 0.85f + 0.15f * Mathf.Sin((x + y) * 1.6f);
        float light = (0.6f + 0.4f * Mathf.Clamp01(-d / 1.2f)) * twist;
        return new Color(0.78f * light, 0.66f * light, 0.46f * light, Mathf.Clamp01(0.5f - d));
    });

    // The board: warm wood lit from the top, its grain running along it, a darker bevel and edge.
    private static void PaintTag(IconCanvas canvas)
    {
        canvas.Paint((x, y) =>
        {
            float d = Tag(x, y);
            if (d > 0.5f) return Color.clear;
            Untilt(x, y, out float lx, out float ly);
            float light = Mathf.Lerp(1f, 0.65f, Mathf.Clamp01((ly - 20f) / 26f));
            light *= 0.88f + 0.12f * Mathf.Sin(ly * 1.9f + Mathf.Sin(lx * 0.23f) * 1.8f);
            light *= 0.72f + 0.28f * Mathf.Clamp01(-d / 2.5f);
            return new Color(0.66f * light, 0.42f * light, 0.22f * light, Mathf.Clamp01(0.5f - d));
        });
        canvas.Paint((x, y) =>
        {
            float d = Mathf.Abs(Tag(x, y)) - 0.5f;
            return new Color(0.14f, 0.08f, 0.04f, Mathf.Clamp01(0.5f - d) * 0.75f);
        });
    }

    // An iron ring round the hole, brighter on top.
    private static void PaintGrommet(IconCanvas canvas) => canvas.Paint((x, y) =>
    {
        float d = Tilted(x, y, (lx, ly) => Mathf.Abs(Circle(lx, ly, HoleX, HoleY, HoleRadius + 0.9f)) - 0.9f);
        Untilt(x, y, out _, out float ly);
        float light = Mathf.Lerp(0.72f, 0.34f, Mathf.Clamp01((ly - (HoleY - 3f)) / 6f));
        return new Color(light, light * 0.97f, light * 0.92f, Mathf.Clamp01(0.5f - d));
    });

    // Carved in: the fish darker than the wood, its outline burning, and a glowing eye.
    private static void PaintFish(IconCanvas canvas, Color glow, Color core)
    {
        Fill(canvas, FishShape, (_, _, _) => new Color(0.2f, 0.11f, 0.05f, 0.6f));
        GlowLine(canvas, (x, y) => Mathf.Abs(FishShape(x, y)) - 0.85f, glow, core, 4f, 0.85f);
        GlowLine(canvas, (x, y) => Tilted(x, y, (lx, ly) => Circle(lx, ly, 46f, 33f, 1.1f)), glow, core, 2.5f, 0.7f);
    }
}
