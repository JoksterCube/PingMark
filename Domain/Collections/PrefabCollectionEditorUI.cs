using UnityEngine;

namespace JoksterCube.PingMark.Domain.Collections;

internal static class PrefabCollectionEditorUI
{
    internal const int CanvasWidth = 1200;
    internal const int CanvasHeight = 740;
    internal const int DefaultColumnWidth = 186;
    internal const int ColumnTextPadding = 4;
    internal const int SectionColumnStride = 222;
    internal const int PrefabRowHeight = 28;
    internal const int VisibleRowsPerColumn = 18;
    internal const int LoadingStepsPerFrame = 512;
    internal const double LoadingBudgetMilliseconds = 3;
    internal const int ScaleReferenceWidth = 1240;
    internal const int ScaleReferenceHeight = 780;
    internal const int MinimumSectionContentWidth = 662;
    internal const int SectionContentPadding = 8;
    internal const int SectionScrollPadding = 670;
    internal const float MaximumCanvasScale = 1.2f;
    internal const float ModalBackdropOpacity = 0.68f;

    internal static readonly Color PanelBackgroundColor = new(0.12f, 0.14f, 0.12f);
    internal static readonly Color PanelBorderColor = new(0.66f, 0.52f, 0.28f);

    private const int BorderThickness = 2;

    internal static Rect ColorPickerPanel(float left, float verticalOffset) =>
        new(left + 6, 142 + verticalOffset, 202, 62);

    internal static Rect ColorSwatch(Rect panel, int option) =>
        new(panel.x + 7 + option % 6 * 31, panel.y + 6 + option / 6 * 27, 24, 20);

    internal static void DrawScreenBackdrop(Color color) =>
        Fill(new Rect(0, 0, Screen.width, Screen.height), color);

    internal static void DrawModalBackdrop() =>
        Fill(new Rect(0, 0, CanvasWidth, CanvasHeight), new Color(0f, 0f, 0f, ModalBackdropOpacity));

    internal static void DrawBorder(Rect rect, Color color)
    {
        Fill(new Rect(rect.x, rect.y, rect.width, BorderThickness), color);
        Fill(new Rect(rect.x, rect.yMax - BorderThickness, rect.width, BorderThickness), color);
        Fill(new Rect(rect.x, rect.y, BorderThickness, rect.height), color);
        Fill(new Rect(rect.xMax - BorderThickness, rect.y, BorderThickness, rect.height), color);
    }

    internal static void DrawPanel(Rect rect, Sprite? panel)
    {
        Fill(rect, PanelBackgroundColor);
        if (panel)
        {
            DrawSprite(rect, panel!);
            Fill(rect, new Color(0.03f, 0.04f, 0.03f, 0.85f));
        }
        DrawBorder(new Rect(0, 0, CanvasWidth, CanvasHeight), PanelBorderColor);
    }

    internal static void FitFont(GUIStyle style, GUIContent content, Rect rect)
    {
        while (style.fontSize > 10)
        {
            Vector2 size = style.CalcSize(content);
            if (size.x <= rect.width && size.y <= rect.height) break;
            style.fontSize--;
        }
    }

    internal static void DrawSprite(Rect rect, Sprite sprite)
    {
        Rect source = sprite.textureRect;
        Texture2D texture = sprite.texture;
        GUI.DrawTextureWithTexCoords(rect, texture, new Rect(source.x / texture.width, source.y / texture.height,
            source.width / texture.width, source.height / texture.height));
    }

    internal static void Fill(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }
}