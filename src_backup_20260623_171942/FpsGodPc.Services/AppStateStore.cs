using System.Text.Json;
using System.Text.Json.Serialization;

namespace FpsGodPc.Services;

public sealed class AppliedRecord
{
    [JsonPropertyName("backup")]
    public string? Backup { get; set; }

    [JsonPropertyName("appliedAt")]
    public string AppliedAt { get; set; } = string.Empty;
}

public sealed class AppSettings
{
    [JsonPropertyName("createRestoreBeforeBoost")]
    public bool CreateRestoreBeforeBoost { get; set; } = true;

    [JsonPropertyName("confirmExtremeTweaks")]
    public bool ConfirmExtremeTweaks { get; set; } = true;

    [JsonPropertyName("language")]
    public string Language { get; set; } = "en";
}

public sealed class ExpertRiskStatus
{
    public bool Waived { get; set; }
    public string? WaivedAt { get; set; }
}

internal sealed class PersistedState
{
    [JsonPropertyName("applied")]
    public Dictionary<string, AppliedRecord> Applied { get; set; } = new();

    [JsonPropertyName("lastBoost")]
    public string? LastBoost { get; set; }

    [JsonPropertyName("lastBoostAt")]
    public string? LastBoostAt { get; set; }

    [JsonPropertyName("settings")]
    public AppSettings Settings { get; set; } = new();

    [JsonPropertyName("expertRiskWaivedAt")]
    public string? ExpertRiskWaivedAt { get; set; }
}

public sealed class AppStateStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private readonly string _path;
    private readonly object _lock = new();
    private PersistedState _state;
    private bool _batchDefer;

    public AppStateStore()
    {
        _path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "fps-god-pc",
            "state.json");
        _state = LoadState();
    }

    public string DataDirectory => Path.GetDirectoryName(_path)!;

    public bool IsApplied(string id)
    {
        lock (_lock)
        {
            return _state.Applied.ContainsKey(id);
        }
    }

    public string? GetBackup(string id)
    {
        lock (_lock)
        {
            return _state.Applied.TryGetValue(id, out var record) ? record.Backup : null;
        }
    }

    public void MarkApplied(string id, string? backup)
    {
        lock (_lock)
        {
            _state.Applied[id] = new AppliedRecord
            {
                Backup = backup,
                AppliedAt = DateTimeOffset.Now.ToString("o"),
            };
        }

        if (ShouldSave())
        {
            Save();
        }
    }

    public IReadOnlyList<string> AppliedIds()
    {
        lock (_lock)
        {
            return _state.Applied.Keys.ToList();
        }
    }

    public Dictionary<string, AppliedRecord> AppliedMap()
    {
        lock (_lock)
        {
            return new Dictionary<string, AppliedRecord>(_state.Applied);
        }
    }

    public void MarkReverted(string id)
    {
        lock (_lock)
        {
            _state.Applied.Remove(id);
        }

        if (ShouldSave())
        {
            Save();
        }
    }

    public void BeginBatch() => _batchDefer = true;

    public void EndBatch()
    {
        _batchDefer = false;
        Save();
    }

    public void SetLastBoost(string name)
    {
        lock (_lock)
        {
            _state.LastBoost = name;
            _state.LastBoostAt = DateTimeOffset.Now.ToString("o");
        }

        if (ShouldSave())
        {
            Save();
        }
    }

    public (List<(string Id, string AppliedAt)> Entries, string? LastBoost, string? LastBoostAt) RollbackInfo()
    {
        lock (_lock)
        {
            var entries = _state.Applied
                .Select(kv => (kv.Key, kv.Value.AppliedAt))
                .ToList();
            return (entries, _state.LastBoost, _state.LastBoostAt);
        }
    }

    public void ClearAllApplied()
    {
        lock (_lock)
        {
            _state.Applied.Clear();
        }

        Save();
    }

    public AppSettings GetSettings()
    {
        lock (_lock)
        {
            return _state.Settings;
        }
    }

    public void SetSettings(AppSettings settings)
    {
        lock (_lock)
        {
            _state.Settings = settings;
        }

        Save();
    }

    public ExpertRiskStatus GetExpertRiskStatus()
    {
        lock (_lock)
        {
            return new ExpertRiskStatus
            {
                Waived = _state.ExpertRiskWaivedAt is not null,
                WaivedAt = _state.ExpertRiskWaivedAt,
            };
        }
    }

    public void WaiveExpertRisk()
    {
        lock (_lock)
        {
            _state.ExpertRiskWaivedAt = DateTimeOffset.Now.ToString("o");
        }

        Save();
    }

    public void ClearExpertRiskWaiver()
    {
        lock (_lock)
        {
            _state.ExpertRiskWaivedAt = null;
        }

        Save();
    }

    private bool ShouldSave() => !_batchDefer;

    private PersistedState LoadState()
    {
        try
        {
            if (!File.Exists(_path))
            {
                return new PersistedState();
            }

            var json = File.ReadAllText(_path);
            return JsonSerializer.Deserialize<PersistedState>(json, JsonOptions) ?? new PersistedState();
        }
        catch
        {
            return new PersistedState();
        }
    }

    private void Save()
    {
        var dir = Path.GetDirectoryName(_path)!;
        Directory.CreateDirectory(dir);
        string json;
        lock (_lock)
        {
            json = JsonSerializer.Serialize(_state, JsonOptions);
        }

        File.WriteAllText(_path, json);
    }
}
