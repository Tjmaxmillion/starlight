using System.Linq;

// ReSharper disable CheckNamespace
namespace Content.Shared.Paper;

public partial struct StampDisplayInfo : IEquatable<StampDisplayInfo>
{
    /// <summary>
    /// Pairing ID of the stamp that made this mark, see PairedPenSystem.
    /// Also used to mark signatures made with special department pens.
    /// </summary>
    [DataField]
    public string? PairId;

    [DataField]
    public List<Color>? Gradient;

    public readonly bool Equals(StampDisplayInfo other)
        => StampedName == other.StampedName
           && StampedColor.Equals(other.StampedColor)
           && Type == other.Type
           && Font == other.Font
           && PairId == other.PairId
           && ((Gradient == null && other.Gradient == null)
               || (Gradient != null && other.Gradient != null && Gradient.SequenceEqual(other.Gradient)));

    public override readonly bool Equals(object? obj)
        => obj is StampDisplayInfo other && Equals(other);

    public static bool operator ==(StampDisplayInfo left, StampDisplayInfo right)
        => left.Equals(right);

    public static bool operator !=(StampDisplayInfo left, StampDisplayInfo right)
        => !left.Equals(right);

    public override readonly int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(StampedName);
        hash.Add(StampedColor);
        hash.Add(Type);
        hash.Add(Font);
        hash.Add(PairId);

        if (Gradient != null)
        {
            foreach (var color in Gradient)
            {
                hash.Add(color);
            }
        }

        return hash.ToHashCode();
    }

    /// <summary>
    /// Samples a color along a multi-stop gradient at normalized position <paramref name="t"/>.
    /// </summary>
    public static Color SampleGradient(IReadOnlyList<Color> colors, float t)
    {
        if (colors.Count == 0)
            return Color.White;
        if (colors.Count == 1)
            return colors[0];

        t = Math.Clamp(t, 0f, 1f);
        var scaled = t * (colors.Count - 1);
        var index = (int)scaled;
        if (index >= colors.Count - 1)
            return colors[^1];

        var fraction = scaled - index;
        return Color.InterpolateBetween(colors[index], colors[index + 1], fraction);
    }
}
