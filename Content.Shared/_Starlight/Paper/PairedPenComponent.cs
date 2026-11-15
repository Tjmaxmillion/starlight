using Robust.Shared.GameStates;

namespace Content.Shared._Starlight.Paper;

/// <summary>
/// Lets a pen edit stamped paper, as long as every rubber stamp on it carries one of
/// <see cref="PairIds"/> and every signature matches this pen's <see cref="SignatureGroup"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(PairedPenSystem))]
public sealed partial class PairedPenComponent : Component
{
    /// <summary>
    /// Stamp pairing IDs this pen can write over, e.g. ["Captain", "HOS", "CE"].
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<string> PairIds = [];

    /// <summary>
    /// Signature group this pen belongs to, e.g. "NTCC".
    /// Pens sharing a signature group can edit papers signed by each other.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? SignatureGroup;
}
