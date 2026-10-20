using Content.Shared.Screen.Components;
using Content.Server.AlertLevel;
using Content.Shared.DeviceNetwork.Events;
using Robust.Shared.Timing;
using Content.Shared.TextScreen;
using Content.Shared._Starlight.Screen;
using Content.Shared.Station.Components;
using Content.Server.Nuke;
using Content.Server.Power.Components;
using Content.Shared.AlertLevel;
using Content.Shared.Nuke;
using Content.Shared.Power;

namespace Content.Server._Starlight.Screen.Systems;

/// <summary>
/// Controls the wallmounted screens on stations and shuttles displaying e.g. FTL duration, ETA
/// </summary>
public sealed partial class ScreenSystem : EntitySystem
{
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private SharedAppearanceSystem _appearanceSystem = default!;

    private TimeSpan _nextNukeUpdate;
    private bool _hasActiveNukeTimer;

    private static readonly HashSet<string> NukeAlertLevels =
        new(StringComparer.OrdinalIgnoreCase) { "delta", "epsilon" };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ScreenComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
        SubscribeLocalEvent<AlertLevelChangedEvent>(OnAlertLevelChanged);
        SubscribeLocalEvent<ScreenComponent, ComponentStartup>(OnScreenStartup);
        SubscribeLocalEvent<ScreenComponent, PowerChangedEvent>(OnPowerChanged);
    }

    /// <summary>
    ///     Calls either a normal screen text update or shuttle timer update based on the presence of
    ///     <see cref="ShuttleTimerMasks.ShuttleMap"/> in <see cref="args.Data"/>
    /// </summary>
    private void OnPacketReceived(EntityUid uid, ScreenComponent component, DeviceNetworkPacketEvent args)
    {
        if (args.Data.TryGetValue(ShuttleTimerMasks.ShuttleMap, out _))
            ShuttleTimer(uid, component, args);
        else
            ScreenText(uid, component, args);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_gameTiming.CurTime >= _nextNukeUpdate)
        {
            _nextNukeUpdate = _gameTiming.CurTime + TimeSpan.FromSeconds(1);
            UpdateNukeTimers();
        }
    }

    private void UpdateNukeTimers()
    {
        var nukeQuery = EntityQueryEnumerator<NukeComponent, TransformComponent>();
        var stationNukeTimes = new Dictionary<EntityUid, float>();
        while (nukeQuery.MoveNext(out _, out var nuke, out var nukeXform))
        {
            if (nuke.Status != NukeStatus.ARMED)
                continue;
            var nukeGrid = nukeXform.GridUid;
            if (nukeGrid == null)
                continue;

            if (!TryComp<StationMemberComponent>(nukeGrid.Value, out var nukeStationMember))
                continue;

            var station = nukeStationMember.Station;
            if (!stationNukeTimes.TryGetValue(station, out var existing) || nuke.RemainingTime < existing)
            {
                stationNukeTimes[station] = nuke.RemainingTime;
            }
        }

        if (stationNukeTimes.Count == 0 && !_hasActiveNukeTimer)
            return;

        _hasActiveNukeTimer = stationNukeTimes.Count > 0;

        var query = EntityQueryEnumerator<ScreenComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var xform))
        {
            if (TryComp<ApcPowerReceiverComponent>(uid, out var power) && !power.Powered)
                continue;

            if (!TryGetScreenStation(uid, out var station, out var alertLevel, xform))
            {
                ClearNukeTimer(uid);
                continue;
            }

            if (!NukeAlertLevels.Contains(alertLevel))
            {
                ClearNukeTimer(uid);
                continue;
            }

            if (stationNukeTimes.TryGetValue(station, out var remaining) && remaining > 0f)
            {
                var target = _gameTiming.CurTime + TimeSpan.FromSeconds(remaining);
                if (!_appearanceSystem.TryGetData(uid, ScreenVisuals.NukeTime, out TimeSpan existing) || System.Math.Abs((existing - target).TotalSeconds) > 1.5)
                {
                    _appearanceSystem.SetData(uid, ScreenVisuals.NukeTime, target);
                }
            }
            else
            {
                ClearNukeTimer(uid);
            }
        }
    }

    private void OnScreenStartup(EntityUid uid, ScreenComponent component, ComponentStartup args)
    {
        if (!TryGetScreenStation(uid, out var station, out var alertLevel))
            return;

        _appearanceSystem.SetData(uid, TextScreenVisuals.AlertLevel, alertLevel);

        if (TryComp<ApcPowerReceiverComponent>(uid, out var power) && !power.Powered)
            return;

        UpdateScreenNukeTimer(uid, station, alertLevel);
    }

    private void OnPowerChanged(EntityUid uid, ScreenComponent component, ref PowerChangedEvent args)
    {
        if (!args.Powered)
        {
            ClearNukeTimer(uid);
            return;
        }

        if (!TryGetScreenStation(uid, out var station, out var alertLevel))
            return;

        UpdateScreenNukeTimer(uid, station, alertLevel);
    }

    private bool TryGetScreenStation(EntityUid uid, out EntityUid station, out string alertLevel, TransformComponent? xform = null)
    {
        station = default;
        alertLevel = string.Empty;

        if (!Resolve(uid, ref xform, false))
            return false;

        var grid = xform.GridUid;
        if (grid == null)
            return false;

        if (!TryComp<StationMemberComponent>(grid, out var stationMember))
            return false;

        station = stationMember.Station;
        if (!TryComp<AlertLevelComponent>(station, out var alertLevelComp))
            return false;

        alertLevel = alertLevelComp.CurrentLevel;
        return true;
    }

    private void OnAlertLevelChanged(AlertLevelChangedEvent args)
    {
        var query = EntityQueryEnumerator<ScreenComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var screen, out var xform))
        {
            var grid = xform.GridUid;
            if (grid == null)
                continue;

            if (!TryComp<StationMemberComponent>(grid, out var stationMember))
                continue;

            if (stationMember.Station != args.Station)
                continue;

            _appearanceSystem.SetData(uid, TextScreenVisuals.AlertLevel, args.AlertLevel);

            if (TryComp<ApcPowerReceiverComponent>(uid, out var power) && !power.Powered)
                continue;

            UpdateScreenNukeTimer(uid, stationMember.Station, args.AlertLevel);
        }
    }

    private void UpdateScreenNukeTimer(EntityUid uid, EntityUid station, string alertLevel)
    {
        if (!NukeAlertLevels.Contains(alertLevel))
        {
            ClearNukeTimer(uid);
            return;
        }

        var lowestRemaining = float.MaxValue;
        var nukeQuery = EntityQueryEnumerator<NukeComponent, TransformComponent>();
        while (nukeQuery.MoveNext(out _, out var nuke, out var nukeXform))
        {
            if (nuke.Status != NukeStatus.ARMED)
                continue;
            if (nukeXform.GridUid == null)
                continue;

            if (!TryComp<StationMemberComponent>(nukeXform.GridUid.Value, out var nukeStationMember)
                || nukeStationMember.Station != station)
                continue;

            if (nuke.RemainingTime < lowestRemaining)
                lowestRemaining = nuke.RemainingTime;
        }

        if (lowestRemaining < float.MaxValue && lowestRemaining > 0f)
        {
            var target = _gameTiming.CurTime + TimeSpan.FromSeconds(lowestRemaining);
            _appearanceSystem.SetData(uid, ScreenVisuals.NukeTime, target);
        }
        else
        {
            ClearNukeTimer(uid);
        }
    }

    private void ClearNukeTimer(EntityUid uid)
    {
        if (!_appearanceSystem.TryGetData(uid, ScreenVisuals.NukeTime, out TimeSpan existing) || existing == TimeSpan.Zero)
            return;

        _appearanceSystem.SetData(uid, ScreenVisuals.NukeTime, TimeSpan.Zero);
    }

    /// <summary>
    ///     Send a text update to every screen on the same MapUid as the originating comms console.
    /// </summary>
    private void ScreenText(EntityUid uid, ScreenComponent component, DeviceNetworkPacketEvent args)
    {
        // don't allow text updates if there's an active timer
        if (HasComp<ScreenTimerComponent>(uid)
            && _appearanceSystem.TryGetData(uid, TextScreenVisuals.TargetTime, out TimeSpan target)
            && target > _gameTiming.CurTime)
            return;

        var screenMap = Transform(uid).MapUid;
        var argsMap = Transform(args.Sender).MapUid;

        if (screenMap == null || argsMap == null || screenMap != argsMap)
        {
            Log.Error($"Can't update screen text, while sender or screen on another map!");
            return;
        }

        if (!args.Data.TryGetValue(ScreenMasks.Text, out string? text) || text == null)
        {
            Log.Error($"Can't update screen text, while input text was null!");
            return;
        }

        _appearanceSystem.SetData(uid, TextScreenVisuals.DefaultText, text);
        _appearanceSystem.SetData(uid, TextScreenVisuals.ScreenText, text);
    }

    /// <summary>
    /// Determines if/how a timer packet affects this screen.
    /// Currently there are 2 broadcast domains: Arrivals, and every other screen.
    /// Domain is determined by the <see cref="Shared.DeviceNetwork.Components.DeviceNetworkComponent.TransmitFrequencyId"/> on each timer.
    /// Each broadcast domain is divided into subnets. Screen MapUid determines subnet.
    /// Subnets are the shuttle, source, and dest. Source/dest change each jump.
    /// This is required to send different timers to the shuttle/terminal/station.
    /// </summary>
    private void ShuttleTimer(EntityUid uid, ScreenComponent component, DeviceNetworkPacketEvent args)
    {
        var timerXform = Transform(uid);

        // no false positives.
        if (timerXform.MapUid == null)
            return;

        string key;
        args.Data.TryGetValue(ShuttleTimerMasks.ShuttleMap, out EntityUid? shuttleMap);
        args.Data.TryGetValue(ShuttleTimerMasks.SourceMap, out EntityUid? source);
        args.Data.TryGetValue(ShuttleTimerMasks.DestMap, out EntityUid? dest);
        args.Data.TryGetValue(ShuttleTimerMasks.Docked, out bool docked);
        string text = docked ? ShuttleTimerMasks.ETD : ShuttleTimerMasks.ETA;

        switch (timerXform.MapUid)
        {
            // sometimes the timer transforms on FTL shuttles have a hyperspace mapuid, so matching by grid works as a fallback.
            case var local when local == shuttleMap || timerXform.GridUid == shuttleMap:
                key = ShuttleTimerMasks.ShuttleTime;
                break;
            case var origin when origin == source:
                key = ShuttleTimerMasks.SourceTime;
                break;
            case var remote when remote == dest:
                key = ShuttleTimerMasks.DestTime;
                text = ShuttleTimerMasks.ETA;
                break;
            default:
                return;
        }

        if (!args.Data.TryGetValue(key, out TimeSpan duration))
            return;

        if (args.Data.TryGetValue(ScreenMasks.Text, out string? label) && label != null)
            text = label;

        _appearanceSystem.SetData(uid, TextScreenVisuals.ScreenText, text);
        _appearanceSystem.SetData(uid, TextScreenVisuals.TargetTime, _gameTiming.CurTime + duration);

        if (args.Data.TryGetValue(ScreenMasks.Color, out Color color))
            _appearanceSystem.SetData(uid, TextScreenVisuals.Color, color);
    }

    public void ForceGlobalTimer()
    {

    }
}
