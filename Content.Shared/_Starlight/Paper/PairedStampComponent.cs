namespace Content.Shared._Starlight.Paper;

/// <summary>
/// Marks a stamp with a pairing ID. The ID is written onto the paper alongside the stamp mark,
/// letting pens with a matching <see cref="PairedPenComponent"/> keep editing the document.
/// </summary>
[RegisterComponent, Access(typeof(PairedPenSystem))]
public sealed partial class PairedStampComponent : Component
{
    /// <summary>
    /// The pairing ID this stamp leaves on paper, e.g. "Captain" or "HOS".
    /// </summary>
    [DataField(required: true)]
    public string PairId = string.Empty;
}
