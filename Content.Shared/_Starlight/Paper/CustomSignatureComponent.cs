using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Shared._Starlight.Paper;

/// <summary>
/// This component indicates that an entity should use a different font or ink color for signatures.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CustomSignatureComponent : Component
{
    [DataField]
    public SignatureConfig Config = new();
}

[Serializable, NetSerializable, DataDefinition]
public partial record struct SignatureConfig
{
    [DataField]
    public string? FontPath = null;

    [DataField(customTypeSerializer: typeof(PenColorSerializer))]
    public List<Color>? PenColor = null;

    public SignatureConfig()
    {
    }

    public SignatureConfig(string? fontPath, List<Color>? penColor = null)
    {
        FontPath = fontPath;
        PenColor = penColor;
    }

    public SignatureConfig Saturate(SignatureConfig other) => new(
        other.FontPath ?? FontPath,
        other.PenColor is { Count: > 0 } ? other.PenColor : PenColor
    );
}

public sealed class PenColorSerializer :
    ITypeSerializer<List<Color>, ValueDataNode>,
    ITypeSerializer<List<Color>, SequenceDataNode>,
    ITypeCopyCreator<List<Color>>
{
    public ValidationNode Validate(
        ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null)
        => Color.TryFromName(node.Value, out _) || Color.TryFromHex(node.Value, out _)
            ? new ValidatedValueNode(node)
            : new ErrorNode(node, $"Failed parsing Color from value '{node.Value}'.");

    public List<Color> Read(
        ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<List<Color>>? instanceProvider = null)
    {
        var col = Color.TryFromName(node.Value, out var namedColor)
            ? namedColor
            : Color.FromHex(node.Value);

        return [col];
    }

    public ValidationNode Validate(
        ISerializationManager serializationManager,
        SequenceDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null)
    {
        if (node.Sequence.Count == 0)
            return new ErrorNode(node, "PenColor sequence cannot be empty.");

        var list = new List<ValidationNode>();
        foreach (var dataNode in node.Sequence)
        {
            if (dataNode is ValueDataNode valNode && (Color.TryFromName(valNode.Value, out _) || Color.TryFromHex(valNode.Value, out _)))
            {
                list.Add(new ValidatedValueNode(valNode));
            }
            else
            {
                list.Add(new ErrorNode(dataNode, "Failed parsing Color item in sequence."));
            }
        }

        return new ValidatedSequenceNode(list);
    }

    public List<Color> Read(
        ISerializationManager serializationManager,
        SequenceDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<List<Color>>? instanceProvider = null)
    {
        var colors = new List<Color>(node.Count);
        foreach (var dataNode in node.Sequence)
        {
            if (dataNode is ValueDataNode valNode)
            {
                var col = Color.TryFromName(valNode.Value, out var namedColor)
                    ? namedColor
                    : Color.FromHex(valNode.Value);
                colors.Add(col);
            }
        }

        return colors;
    }

    public DataNode Write(
        ISerializationManager serializationManager,
        List<Color> value,
        IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        if (value.Count == 1)
            return new ValueDataNode(value[0].ToHex());

        var seq = new SequenceDataNode();
        foreach (var color in value)
        {
            seq.Add(new ValueDataNode(color.ToHex()));
        }

        return seq;
    }

    public List<Color> CreateCopy(
        ISerializationManager serializationManager,
        List<Color> source,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null)
        => [.. source];
}
