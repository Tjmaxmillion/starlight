using Content.Shared._Starlight.Paper;

namespace Content.Shared.Paper;

public sealed partial class PaperSystem
{
    [Dependency] private PairedPenSystem _pairedPen = default!;
}
