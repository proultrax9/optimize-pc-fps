using LibreHardwareMonitor.Hardware;

namespace FpsGodPc.Services;

/// <summary>
/// Reads real CPU/GPU temperatures and GPU load via LibreHardwareMonitor.
/// The Computer object is opened lazily on first Read() call to keep startup fast.
/// Requires the app to run elevated (administrator) — the kernel driver needs it.
/// Register as a singleton in DI; dispose with the container.
/// </summary>
public sealed class HardwareMonitorService : IDisposable
{
    private Computer? _computer;
    private readonly object _lock = new();
    private bool _opened;
    private bool _disposed;

    // ------------------------------------------------------------------
    // Public API
    // ------------------------------------------------------------------

    public sealed class HardwareReadings
    {
        public float? CpuTempC { get; init; }
        public float? GpuTempC { get; init; }
        public float? GpuLoadPct { get; init; }
        public string? GpuName { get; init; }
    }

    /// <summary>
    /// Reads current hardware sensor values. Never throws — returns nulls on any error.
    /// </summary>
    public HardwareReadings Read()
    {
        try
        {
            EnsureOpen();

            float? cpuTempC = null;
            float? gpuTempC = null;
            float? gpuLoadPct = null;
            string? gpuName = null;

            lock (_lock)
            {
                // Re-read _computer inside the lock: Dispose() may have nulled it on
                // another thread between EnsureOpen() and here.
                var computer = _computer;
                if (computer is null)
                {
                    return new HardwareReadings();
                }

                foreach (var hardware in computer.Hardware)
                {
                    hardware.Update();

                    if (hardware.HardwareType == HardwareType.Cpu)
                    {
                        cpuTempC = ReadCpuTemp(hardware);
                    }
                    else if (hardware.HardwareType is HardwareType.GpuNvidia
                                                   or HardwareType.GpuAmd
                                                   or HardwareType.GpuIntel)
                    {
                        gpuName = hardware.Name;
                        (gpuTempC, gpuLoadPct) = ReadGpuSensors(hardware);
                    }
                }
            }

            return new HardwareReadings
            {
                CpuTempC = cpuTempC,
                GpuTempC = gpuTempC,
                GpuLoadPct = gpuLoadPct,
                GpuName = gpuName,
            };
        }
        catch
        {
            // Never surface exceptions — telemetry is best-effort.
            return new HardwareReadings();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        try
        {
            lock (_lock)
            {
                _computer?.Close();
                _computer = null;
            }
        }
        catch
        {
            // Suppress all disposal exceptions.
        }
    }

    // ------------------------------------------------------------------
    // Private helpers
    // ------------------------------------------------------------------

    private void EnsureOpen()
    {
        if (_opened || _disposed)
        {
            return;
        }

        lock (_lock)
        {
            if (_opened || _disposed)
            {
                return;
            }

            try
            {
                var computer = new Computer
                {
                    IsCpuEnabled = true,
                    IsGpuEnabled = true,
                };
                computer.Open();
                _computer = computer;
                _opened = true;
            }
            catch
            {
                // Library may fail on machines without driver access.
                // Mark opened so we don't keep retrying.
                _opened = true;
            }
        }
    }

    /// <summary>
    /// Returns the CPU Package temperature if available; falls back to the max
    /// temperature across all core temp sensors.
    /// </summary>
    private static float? ReadCpuTemp(IHardware cpu)
    {
        // Walk sub-hardware too (some boards expose temps there).
        foreach (var sub in cpu.SubHardware)
        {
            sub.Update();
        }

        float? packageTemp = null;
        float maxCoreTemp = float.MinValue;
        bool hasCoreTemp = false;

        foreach (var sensor in EnumerateAllSensors(cpu))
        {
            if (sensor.SensorType != SensorType.Temperature || sensor.Value is null)
            {
                continue;
            }

            var name = sensor.Name ?? string.Empty;
            var value = sensor.Value.Value;

            if (name.Contains("Package", StringComparison.OrdinalIgnoreCase)
                || name.Equals("CPU Package", StringComparison.OrdinalIgnoreCase))
            {
                packageTemp = value;
            }
            else if (name.Contains("Core", StringComparison.OrdinalIgnoreCase))
            {
                if (value > maxCoreTemp)
                {
                    maxCoreTemp = value;
                    hasCoreTemp = true;
                }
            }
        }

        return packageTemp ?? (hasCoreTemp ? maxCoreTemp : null);
    }

    /// <summary>
    /// Returns (gpuTempC, gpuLoadPct). Prefers "GPU Core" temp and "GPU Core" load.
    /// Falls back to "GPU Hot Spot" or any GPU temperature sensor.
    /// </summary>
    private static (float? temp, float? load) ReadGpuSensors(IHardware gpu)
    {
        foreach (var sub in gpu.SubHardware)
        {
            sub.Update();
        }

        float? coreTemp = null;
        float? hotSpotTemp = null;
        float? anyTemp = null;
        float? coreLoad = null;
        float? anyLoad = null;

        foreach (var sensor in EnumerateAllSensors(gpu))
        {
            if (sensor.Value is null)
            {
                continue;
            }

            var name = sensor.Name ?? string.Empty;
            var value = sensor.Value.Value;

            if (sensor.SensorType == SensorType.Temperature)
            {
                anyTemp ??= value;
                if (name.Contains("Hot Spot", StringComparison.OrdinalIgnoreCase)
                    || name.Contains("HotSpot", StringComparison.OrdinalIgnoreCase))
                {
                    hotSpotTemp = value;
                }
                else if (name.Contains("Core", StringComparison.OrdinalIgnoreCase)
                         || name.Equals("GPU Temperature", StringComparison.OrdinalIgnoreCase))
                {
                    coreTemp = value;
                }
            }
            else if (sensor.SensorType == SensorType.Load)
            {
                anyLoad ??= value;
                if (name.Contains("Core", StringComparison.OrdinalIgnoreCase)
                    || name.Equals("GPU Core", StringComparison.OrdinalIgnoreCase)
                    || name.Equals("GPU", StringComparison.OrdinalIgnoreCase))
                {
                    coreLoad = value;
                }
            }
        }

        var gpuTemp = coreTemp ?? hotSpotTemp ?? anyTemp;
        var gpuLoad = coreLoad ?? anyLoad;
        return (gpuTemp, gpuLoad);
    }

    /// <summary>
    /// Yields sensors from a hardware node and all its sub-hardware nodes.
    /// </summary>
    private static IEnumerable<ISensor> EnumerateAllSensors(IHardware hardware)
    {
        foreach (var sensor in hardware.Sensors)
        {
            yield return sensor;
        }

        foreach (var sub in hardware.SubHardware)
        {
            foreach (var sensor in sub.Sensors)
            {
                yield return sensor;
            }
        }
    }

    // ------------------------------------------------------------------
    // LibreHardwareMonitor visitor (required by the library's design)
    // ------------------------------------------------------------------

    private sealed class UpdateVisitor : IVisitor
    {
        public void VisitComputer(IComputer computer)
        {
            computer.Traverse(this);
        }

        public void VisitHardware(IHardware hardware)
        {
            hardware.Update();
            foreach (var subHardware in hardware.SubHardware)
            {
                subHardware.Accept(this);
            }
        }

        public void VisitSensor(ISensor sensor) { }

        public void VisitParameter(IParameter parameter) { }
    }
}
