using Content.Shared.Paper;

namespace Content.Shared._Starlight.Paper;

/// <summary>
/// Handles stamp/pen pairing: records a stamp's pair ID when it is applied,
/// and decides whether a paired pen may edit an already-stamped or signed paper.
/// </summary>
public sealed class PairedPenSystem : EntitySystem
{
    /// <summary>
    /// Gets the signature group a pen leaves on paper when signing.
    /// </summary>
    public string? GetSignatureGroup(EntityUid? pen)
        => TryComp<PairedPenComponent>(pen, out var paired) ? paired.SignatureGroup : null;

    /// <summary>
    /// Gets the pair ID a stamp would leave on paper. For multistamps, uses the currently selected stamp.
    /// </summary>
    public string? GetPairId(EntityUid stamp)
    {
        if (TryComp<MultistampComponent>(stamp, out var multi))
        {
            if (multi.CurrentEntry < 0 || multi.Stamps.Count <= multi.CurrentEntry)
                return null;

            stamp = multi.Stamps[multi.CurrentEntry];
        }

        return TryComp<PairedStampComponent>(stamp, out var paired) ? paired.PairId : null;
    }

    /// <summary>
    /// A paired pen can edit only if:
    /// 1. Every rubber stamp on the paper has a pair ID this pen accepts.
    /// 2. Every signature on the paper was made using a pen with the same signature group.
    /// </summary>
    public bool CanEdit(EntityUid pen, List<StampDisplayInfo> stamps)
    {
        if (!TryComp<PairedPenComponent>(pen, out var paired))
            return false;

        foreach (var stamp in stamps)
        {
            if (stamp.Type == StampType.Signature)
            {
                // Signatures require a matching signature group
                if (paired.SignatureGroup == null || stamp.PairId == null || stamp.PairId != paired.SignatureGroup)
                    return false;

                continue;
            }

            if (stamp.PairId is not { } pairId || !paired.PairIds.Contains(pairId))
                return false;
        }

        return true;
    }
}
