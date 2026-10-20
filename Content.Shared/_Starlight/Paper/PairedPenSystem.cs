using Content.Shared.Paper;

namespace Content.Shared._Starlight.Paper;

/// <summary>
/// Handles stamp/pen pairing: records a stamp's pair ID when it is applied,
/// and decides whether a paired pen may edit an already-stamped or signed paper.
/// </summary>
public sealed class PairedPenSystem : EntitySystem
{
    /// <summary>
    /// Marker pair ID assigned to signatures made with any of the special department pens.
    /// </summary>
    public const string PairedSignatureId = "PairedPenSignature";

    /// <summary>
    /// Gets the pair ID a stamp would leave on paper. For multistamps, uses the currently selected stamp.
    /// </summary>
    public string? GetPairId(EntityUid stamp)
    {
        if (TryComp<MultistampComponent>(stamp, out var multi))
        {
            if (multi.Stamps.Count <= multi.CurrentEntry)
                return null;

            stamp = multi.Stamps[multi.CurrentEntry];
        }

        return TryComp<PairedStampComponent>(stamp, out var paired) ? paired.PairId : null;
    }

    /// <summary>
    /// A paired pen can edit only if:
    /// 1. Every rubber stamp on the paper has a pair ID this pen accepts.
    /// 2. Every signature on the paper was made using one of the special department pens.
    /// </summary>
    public bool CanEdit(EntityUid pen, EntityUid user, List<StampDisplayInfo> stamps)
    {
        if (!TryComp<PairedPenComponent>(pen, out var paired))
            return false;

        foreach (var stamp in stamps)
        {
            if (stamp.Type == StampType.Signature)
            {
                // All signatures must come from a special department pen
                if (stamp.PairId != PairedSignatureId)
                    return false;

                continue;
            }

            if (stamp.PairId is not { } pairId || !paired.PairIds.Contains(pairId))
                return false;
        }

        return true;
    }
}
