namespace LTOG;

internal enum MountCompletion { None, Mounted, Unmounted }

/// <summary>UI-only transition tracking; never initiates or changes a drive operation.</summary>
internal sealed class MountCompletionState(MountPhase initialPhase)
{
    private MountPhase _phase = initialPhase;
    private bool _unmountWasMounted;

    public MountCompletion Observe(MountPhase phase, bool processRunning, int? exitCode)
    {
        if (phase == _phase) return MountCompletion.None;
        var previous = _phase;
        _phase = phase;
        if (phase == MountPhase.Unmounting)
            _unmountWasMounted = previous == MountPhase.Mounted;

        var result = phase == MountPhase.Mounted && previous == MountPhase.Mounting && processRunning
            ? MountCompletion.Mounted
            : phase == MountPhase.Idle && previous == MountPhase.Unmounting && _unmountWasMounted && exitCode == 0
                ? MountCompletion.Unmounted
                : MountCompletion.None;

        if (phase == MountPhase.Idle) _unmountWasMounted = false;
        return result;
    }
}
