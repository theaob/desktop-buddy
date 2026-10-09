namespace DesktopBuddy.Core;

/// <summary>
/// Turns the system's cumulative CPU time counters into a smoothed load, and decides when the PC
/// counts as busy. The two thresholds keep it from flickering around a single value.
/// </summary>
public sealed class CpuMonitor
{
    public const double BusyAbove = 0.8;
    public const double CalmBelow = 0.6;

    // Weight of each new sample; about five samples to settle.
    private const double Smoothing = 0.35;

    private ulong _lastIdle, _lastTotal;
    private bool _hasSample;

    /// <summary>Smoothed share of CPU time in use, 0 to 1.</summary>
    public double Load { get; private set; }

    public bool Busy { get; private set; }

    /// <summary>Feeds the counters from GetSystemTimes. Kernel time includes idle time, as Windows reports it.</summary>
    public void Sample(ulong idle, ulong kernel, ulong user)
    {
        ulong total = kernel + user;
        if (_hasSample && total > _lastTotal && idle >= _lastIdle)
        {
            double busyShare = 1 - (double)(idle - _lastIdle) / (total - _lastTotal);
            Load += Smoothing * (Math.Clamp(busyShare, 0, 1) - Load);
            if (Load >= BusyAbove) Busy = true;
            else if (Load < CalmBelow) Busy = false;
        }
        _lastIdle = idle;
        _lastTotal = total;
        _hasSample = true;
    }
}
