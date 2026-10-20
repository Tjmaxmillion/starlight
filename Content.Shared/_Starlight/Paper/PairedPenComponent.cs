namespace Content.Shared._Starlight.Paper;

/// <summary>
/// Lets a pen edit stamped paper, as long as every rubber stamp on it carries one of
/// <see cref="PairIds"/> and every signature belongs to the person writing.
/// </summary>
[RegisterComponent, Access(typeof(PairedPenSystem))]
public sealed partial class PairedPenComponent : Component
{
    /// <summary>
    /// Stamp pairing IDs this pen can write over, e.g. ["Captain", "HOS", "CE"].
    /// </summary>
    [DataField(required: true)]
    public HashSet<string> PairIds = new();
}
