using Robust.Shared.Serialization;

namespace Content.Shared.Paper;

public partial struct StampDisplayInfo
{
    /// <summary>
    /// Starlight - Pairing ID of the stamp that made this mark, see PairedPenSystem.
    /// Also used to mark signatures made with special department pens.
    /// </summary>
    [DataField("pairId")]
    public string? PairId;
}
