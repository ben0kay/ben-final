// Constant-space frame-delta histogram for opt-in world-streaming benchmarks.
using System;

public sealed class FrameTimeRecorder
{
    // Half-millisecond bins up to one second; longer frames share the overflow bin.
    private const double BinMilliseconds = 0.5;
    private readonly long[] _bins = new long[2001];
    private long _frames, _spikes;
    private double _sum, _worst;

    public bool IsRecording { get; private set; }
    public long FrameCount => _frames;
    public long SpikeCount => _spikes;

    // =========================================================
    public void Start()
    {
        Array.Clear(_bins, 0, _bins.Length);
        _frames = _spikes = 0;
        _sum = _worst = 0;
        IsRecording = true;
    }

    // =========================================================
    public void Stop() => IsRecording = false;

    // =========================================================
    // O(1) work and no per-frame allocations; samples include idle terrain frames.
    public void Sample(double milliseconds, double spikeThreshold = 33.3)
    {
        if (!IsRecording || !double.IsFinite(milliseconds) || milliseconds < 0) return;
        int bin = Math.Clamp((int)(milliseconds / BinMilliseconds), 0, _bins.Length - 1);
        _bins[bin]++;
        _frames++;
        _sum += milliseconds;
        _worst = Math.Max(_worst, milliseconds);
        if (milliseconds >= spikeThreshold) _spikes++;
    }

    // =========================================================
    // Histograms provide approximate percentiles without sorting frame arrays.
    public FrameTimeSnapshot Snapshot()
    {
        if (_frames == 0) return default;
        return new FrameTimeSnapshot(_frames, _sum / _frames,
            Percentile(0.5), Percentile(0.95), Percentile(0.99),
            _worst, _spikes);
    }

    // =========================================================
    private double Percentile(double quantile)
    {
        long target = (long)Math.Ceiling(_frames * quantile);
        long cumulative = 0;
        for (int i = 0; i < _bins.Length; i++)
        {
            cumulative += _bins[i];
            if (cumulative >= target) return i * BinMilliseconds;
        }
        return (_bins.Length - 1) * BinMilliseconds;
    }
}

public readonly record struct FrameTimeSnapshot(long Frames, double AverageMs,
    double MedianMs, double P95Ms, double P99Ms, double WorstMs, long Spikes);
