using System.Linq;
using System.Text;
using Content.Shared._Starlight.Paper;
using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Robust.Shared.Utility;

// ReSharper disable CheckNamespace
namespace Content.Shared.Paper;

public sealed partial class PaperSystem
{
    private static readonly Color DefaultSignatureColor = Color.FromHex("#333333");

    [Dependency] private PairedPenSystem _pairedPen = default!;
    [Dependency] private SharedChargesSystem _charges = default!;

    private bool TryStampWithCharges(Entity<PaperComponent> paper, EntityUid stamp, EntityUid user, StampComponent stampComp)
    {
        var stampInfo = GetStampInfo(stampComp);
        stampInfo.PairId = _pairedPen.GetPairId(stamp);
        return TryConsumeStampCharge(paper, stampInfo, stamp, user)
            && TryStamp(paper, stampInfo, stampComp.StampState);
    }

    /// <summary>
    /// If the stamp (or active multistamp entry) has limited charges, consume one charge
    /// only when the stamp isn't already present on the paper. Returns false if already
    /// stamped with a limited-charge stamp or out of charges.
    /// </summary>
    private bool TryConsumeStampCharge(Entity<PaperComponent> paper, StampDisplayInfo stampInfo, EntityUid stamp, EntityUid user)
    {
        var activeStamp = stamp;
        if (TryComp<MultistampComponent>(stamp, out var multi))
        {
            if (multi.CurrentEntry < 0 || multi.Stamps.Count <= multi.CurrentEntry)
                return true;

            activeStamp = multi.Stamps[multi.CurrentEntry];
        }

        if (TryComp<LimitedChargesComponent>(activeStamp, out var charges))
        {
            if (paper.Comp.StampedBy.Contains(stampInfo))
                return false;

            if (!_charges.TryUseCharge((activeStamp, charges)))
            {
                _popupSystem.PopupClient(Loc.GetString("stamp-charges-depleted", ("stamp", activeStamp)), stamp, user);
                return false;
            }
        }

        return true;
    }

    private static string FormatSignatureExamine(StampDisplayInfo signature)
    {
        if (signature.Gradient is { Count: > 1 } gradient && !string.IsNullOrEmpty(signature.StampedName))
            return FormatGradientMarkup(signature.StampedName, gradient);

        var escaped = FormattedMessage.EscapeText(signature.StampedName);
        return signature.StampedColor != DefaultSignatureColor
            ? $"[color={signature.StampedColor.ToHex()}]{escaped}[/color]"
            : escaped;
    }

    private static string FormatGradientMarkup(string text, IReadOnlyList<Color> gradient)
    {
        var runes = text.EnumerateRunes().ToArray();
        var sb = new StringBuilder();
        for (var i = 0; i < runes.Length; i++)
        {
            var escaped = FormattedMessage.EscapeText(runes[i].ToString());
            if (Rune.IsWhiteSpace(runes[i]))
            {
                sb.Append(escaped);
                continue;
            }

            var t = runes.Length > 1 ? (i + 0.5f) / runes.Length : 0.5f;
            var color = EnsureExamineReadable(StampDisplayInfo.SampleGradient(gradient, t));
            sb.Append($"[color={color.ToHex()}]{escaped}[/color]");
        }

        return sb.ToString();
    }

    private static Color EnsureExamineReadable(Color color)
    {
        var hsv = Color.ToHsv(color);
        if (hsv.Z >= 0.45f)
            return color;

        hsv.Z = 0.45f;
        return Color.FromHsv(hsv);
    }
}
