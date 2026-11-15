using System.Numerics;
using System.Text;
using Content.Shared.Paper;
using Robust.Client.Graphics;

// ReSharper disable CheckNamespace
namespace Content.Client.Paper.UI;

public sealed partial class StampLabel
{
    /// <summary>
    /// Gradient ink colors for special pens.
    /// </summary>
    public List<Color>? Gradient;

    private bool TryDrawGradient(DrawingHandleScreen handle)
    {
        if (Gradient is not { Count: > 1 } || string.IsNullOrEmpty(Text))
            return false;

        DrawGradient(handle);
        return true;
    }

    private void DrawGradient(DrawingHandleScreen handle)
    {
        if (Gradient is not { Count: > 1 } || string.IsNullOrEmpty(Text))
            return;

        var font = FontOverride
            ?? (TryGetStyleProperty<Font>(StylePropertyFont, out var styleFont)
                ? styleFont
                : UserInterfaceManager.ThemeDefaults.LabelFont);

        var lineWidths = new List<float> { 0f };
        var textHeight = font.GetHeight(UIScale);
        var lineHeight = font.GetLineHeight(UIScale);

        foreach (var rune in Text.EnumerateRunes())
        {
            if (rune.Value == '\n')
            {
                lineWidths.Add(0f);
                textHeight += lineHeight;
                continue;
            }

            var metrics = font.GetCharMetrics(rune, UIScale);
            if (metrics != null)
                lineWidths[^1] += metrics.Value.Advance;
        }

        var vOffset = VAlign switch
        {
            VAlignMode.Center or VAlignMode.Fill => (PixelSize.Y - textHeight) / 2,
            VAlignMode.Bottom => PixelSize.Y - textHeight,
            _ => 0
        };

        var ascent = font.GetAscent(UIScale);
        var lineIndex = 0;
        var currentX = 0f;
        var baseLine = new Vector2(GetHorizontalOffset(Align, PixelSize.X, lineWidths[0]), ascent + vOffset);

        foreach (var rune in Text.EnumerateRunes())
        {
            if (rune.Value == '\n')
            {
                lineIndex++;
                currentX = 0f;
                baseLine = new Vector2(
                    GetHorizontalOffset(Align, PixelSize.X, lineWidths[lineIndex]),
                    ascent + (lineIndex * lineHeight) + vOffset);
                continue;
            }

            var lineAdvance = lineWidths[lineIndex];
            var metrics = font.GetCharMetrics(rune, UIScale);
            var advance = metrics?.Advance ?? 0f;

            var charCenterX = currentX + (advance / 2f);
            var t = lineAdvance > 0f ? Math.Clamp(charCenterX / lineAdvance, 0f, 1f) : 0f;
            var charColor = StampDisplayInfo.SampleGradient(Gradient, t);

            font.DrawChar(handle, rune, baseLine, UIScale, charColor);
            baseLine.X += advance;
            currentX += advance;
        }
    }

    private static int GetHorizontalOffset(AlignMode align, int pixelWidth, float lineWidth)
        => align switch
        {
            AlignMode.Center or AlignMode.Fill => (int)((pixelWidth - lineWidth) / 2),
            AlignMode.Right => (int)(pixelWidth - lineWidth),
            _ => 0
        };
}
