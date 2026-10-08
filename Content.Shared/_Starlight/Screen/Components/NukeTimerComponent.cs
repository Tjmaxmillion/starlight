namespace Content.Shared._Starlight.Screen.Components;

/// <summary>
///     Tracks the active nuke countdown target time and sprite layers.
///     Kept separate from ScreenTimerComponent to avoid conflicts.
/// </summary>
[RegisterComponent]
public sealed partial class NukeTimerComponent : Component
{
    [DataField]
    public TimeSpan Target = TimeSpan.Zero;
}
