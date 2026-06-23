using Microsoft.Data.Sqlite;

namespace FpsGodPc.Services;

public sealed record SnapshotSummary(long Id, string Label, string TakenAt, bool CommittedKnownGood);

public sealed class SafetySettings
{
    public string Language { get; set; } = "en";
    public bool WatcherEnabled { get; set; } = true;
    public bool BootAutoRevert { get; set; } = true;
    public uint ConfirmTimerSecs { get; set; } = 15;
    public bool OnboardingComplete { get; set; }
}

public sealed class SafetyStatus
{
    public bool PendingRevert { get; set; }
    public string? PendingReason { get; set; }
    public long? PendingSnapshotId { get; set; }
    public long? LastKnownGoodId { get; set; }
    public int AppliedTweakCount { get; set; }
}

public sealed class WatcherProfile
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Executable { get; set; } = string.Empty;
    public List<string> TweakIds { get; set; } = [];
    public bool WatcherEnabled { get; set; }
    public bool Installed { get; set; }
    public string? InstallPath { get; set; }
    public string LaunchOptions { get; set; } = string.Empty;
    public List<string> Notes { get; set; } = [];
    public bool Active { get; set; }
}

public sealed class BenchmarkSession
{
    public long Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string TakenAt { get; set; } = string.Empty;
    public uint DurationSecs { get; set; }
    public float? AvgFps { get; set; }
    public float? Pct1Low { get; set; }
    public float? Pct01Low { get; set; }
    public float? AvgFrametimeMs { get; set; }
    public float AvgCpuPct { get; set; }
    public float? AvgGpuPct { get; set; }
    public string Source { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public sealed class GuardianDatabase
{
    private readonly string _path;
    private static bool _showCrashWatchdog;

    public GuardianDatabase()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "fps-god-pc");
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "guardian.db");
        Migrate();
    }

    public string DataDirectory => Path.GetDirectoryName(_path)!;

    public long CreateSnapshot(string label, string stateJson)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO snapshots (label, taken_at, state_json) VALUES ($label, $taken, $json)";
        cmd.Parameters.AddWithValue("$label", label);
        cmd.Parameters.AddWithValue("$taken", DateTimeOffset.UtcNow.ToString("o"));
        cmd.Parameters.AddWithValue("$json", stateJson);
        cmd.ExecuteNonQuery();
        using var idCmd = conn.CreateCommand();
        idCmd.CommandText = "SELECT last_insert_rowid()";
        return Convert.ToInt64(idCmd.ExecuteScalar());
    }

    public List<SnapshotSummary> ListSnapshots()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, label, taken_at, committed_known_good FROM snapshots ORDER BY id DESC LIMIT 20";
        using var reader = cmd.ExecuteReader();
        var list = new List<SnapshotSummary>();
        while (reader.Read())
        {
            list.Add(new SnapshotSummary(
                reader.GetInt64(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetInt64(3) != 0));
        }

        return list;
    }

    public string GetSnapshotState(long id)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT state_json FROM snapshots WHERE id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        return cmd.ExecuteScalar() as string ?? throw new InvalidOperationException("Snapshot not found.");
    }

    public void MarkKnownGood(long id)
    {
        using var conn = Open();
        using var tx = conn.BeginTransaction();
        using (var clear = conn.CreateCommand())
        {
            clear.CommandText = "UPDATE snapshots SET committed_known_good = 0";
            clear.ExecuteNonQuery();
        }

        using (var mark = conn.CreateCommand())
        {
            mark.CommandText = "UPDATE snapshots SET committed_known_good = 1 WHERE id = $id";
            mark.Parameters.AddWithValue("$id", id);
            mark.ExecuteNonQuery();
        }

        tx.Commit();
    }

    public void RecordApplied(string tweakId, string? previous)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT OR REPLACE INTO applied_tweaks (tweak_id, previous_value, applied_at)
            VALUES ($id, $prev, $at)
            """;
        cmd.Parameters.AddWithValue("$id", tweakId);
        cmd.Parameters.AddWithValue("$prev", (object?)previous ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$at", DateTimeOffset.UtcNow.ToString("o"));
        cmd.ExecuteNonQuery();
    }

    public void RemoveApplied(string tweakId)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM applied_tweaks WHERE tweak_id = $id";
        cmd.Parameters.AddWithValue("$id", tweakId);
        cmd.ExecuteNonQuery();
    }

    public List<string> AppliedIds()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT tweak_id FROM applied_tweaks";
        using var reader = cmd.ExecuteReader();
        var ids = new List<string>();
        while (reader.Read())
        {
            ids.Add(reader.GetString(0));
        }

        return ids;
    }

    public string? GetPrevious(string tweakId)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT previous_value FROM applied_tweaks WHERE tweak_id = $id";
        cmd.Parameters.AddWithValue("$id", tweakId);
        var result = cmd.ExecuteScalar();
        return result is DBNull or null ? null : result.ToString();
    }

    public void SetPendingRevert(long snapshotId, string reason)
    {
        using var conn = Open();
        using var tx = conn.BeginTransaction();
        using (var clear = conn.CreateCommand())
        {
            clear.CommandText = "DELETE FROM pending_revert";
            clear.ExecuteNonQuery();
        }

        using (var insert = conn.CreateCommand())
        {
            insert.CommandText = """
                INSERT INTO pending_revert (snapshot_id, reason, created_at)
                VALUES ($snap, $reason, $at)
                """;
            insert.Parameters.AddWithValue("$snap", snapshotId);
            insert.Parameters.AddWithValue("$reason", reason);
            insert.Parameters.AddWithValue("$at", DateTimeOffset.UtcNow.ToString("o"));
            insert.ExecuteNonQuery();
        }

        tx.Commit();
    }

    public (long SnapshotId, string Reason)? PendingRevertInfo()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT snapshot_id, reason FROM pending_revert ORDER BY id DESC LIMIT 1";
        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        var snap = reader.IsDBNull(0) ? 0L : reader.GetInt64(0);
        return (snap, reader.GetString(1));
    }

    public void ClearPendingRevert()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM pending_revert";
        cmd.ExecuteNonQuery();
    }

    public SafetyStatus GetSafetyStatus()
    {
        var pending = PendingRevertInfo();
        using var conn = Open();

        long? lastKg = null;
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT id FROM snapshots WHERE committed_known_good = 1 ORDER BY id DESC LIMIT 1";
            var result = cmd.ExecuteScalar();
            if (result is long id)
            {
                lastKg = id;
            }
        }

        int count = 0;
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM applied_tweaks";
            count = Convert.ToInt32(cmd.ExecuteScalar());
        }

        return new SafetyStatus
        {
            PendingRevert = pending is not null,
            PendingReason = pending?.Reason,
            PendingSnapshotId = pending is { SnapshotId: > 0 } p ? p.SnapshotId : null,
            LastKnownGoodId = lastKg,
            AppliedTweakCount = count,
        };
    }

    public void SetCrashDirty(bool dirty)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE crash_flag SET dirty = $d WHERE id = 1";
        cmd.Parameters.AddWithValue("$d", dirty ? 1 : 0);
        cmd.ExecuteNonQuery();
    }

    public bool IsCrashDirty()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT dirty FROM crash_flag WHERE id = 1";
        return Convert.ToInt32(cmd.ExecuteScalar()) != 0;
    }

    public string? GetSetting(string key)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT value FROM settings WHERE key = $k";
        cmd.Parameters.AddWithValue("$k", key);
        return cmd.ExecuteScalar() as string;
    }

    public void SetSetting(string key, string value)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT OR REPLACE INTO settings (key, value) VALUES ($k, $v)";
        cmd.Parameters.AddWithValue("$k", key);
        cmd.Parameters.AddWithValue("$v", value);
        cmd.ExecuteNonQuery();
    }

    public SafetySettings GetSettings()
    {
        var raw = GetSetting("app_settings");
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new SafetySettings();
        }

        return System.Text.Json.JsonSerializer.Deserialize<SafetySettings>(raw) ?? new SafetySettings();
    }

    public void SaveSettings(SafetySettings settings)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(settings);
        SetSetting("app_settings", json);
    }

    public void UpsertProfile(WatcherProfile profile)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT OR REPLACE INTO profiles (id, name, executable, tweak_ids, watcher_enabled, launch_options, notes)
            VALUES ($id, $name, $exe, $tweaks, $watch, $launch, $notes)
            """;
        cmd.Parameters.AddWithValue("$id", profile.Id);
        cmd.Parameters.AddWithValue("$name", profile.Name);
        cmd.Parameters.AddWithValue("$exe", profile.Executable);
        cmd.Parameters.AddWithValue("$tweaks", System.Text.Json.JsonSerializer.Serialize(profile.TweakIds));
        cmd.Parameters.AddWithValue("$watch", profile.WatcherEnabled ? 1 : 0);
        cmd.Parameters.AddWithValue("$launch", profile.LaunchOptions);
        cmd.Parameters.AddWithValue("$notes", System.Text.Json.JsonSerializer.Serialize(profile.Notes));
        cmd.ExecuteNonQuery();
    }

    public List<WatcherProfile> ListProfiles()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, name, executable, tweak_ids, watcher_enabled, launch_options, notes FROM profiles";
        using var reader = cmd.ExecuteReader();
        var list = new List<WatcherProfile>();
        while (reader.Read())
        {
            list.Add(new WatcherProfile
            {
                Id = reader.GetString(0),
                Name = reader.GetString(1),
                Executable = reader.GetString(2),
                TweakIds = System.Text.Json.JsonSerializer.Deserialize<List<string>>(reader.GetString(3)) ?? [],
                WatcherEnabled = reader.GetInt64(4) != 0,
                LaunchOptions = reader.GetString(5),
                Notes = System.Text.Json.JsonSerializer.Deserialize<List<string>>(reader.GetString(6)) ?? [],
            });
        }

        return list;
    }

    public void SetProfileWatcher(string id, bool enabled)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE profiles SET watcher_enabled = $e WHERE id = $id";
        cmd.Parameters.AddWithValue("$e", enabled ? 1 : 0);
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    public long SaveBenchmark(BenchmarkSession session)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO benchmarks (label, taken_at, duration_secs, avg_fps, pct1_low, pct01_low,
                avg_frametime_ms, avg_cpu_pct, avg_gpu_pct, source, notes)
            VALUES ($label, $at, $dur, $fps, $p1, $p01, $ft, $cpu, $gpu, $src, $notes)
            """;
        cmd.Parameters.AddWithValue("$label", session.Label);
        cmd.Parameters.AddWithValue("$at", session.TakenAt);
        cmd.Parameters.AddWithValue("$dur", session.DurationSecs);
        cmd.Parameters.AddWithValue("$fps", (object?)session.AvgFps ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$p1", (object?)session.Pct1Low ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$p01", (object?)session.Pct01Low ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$ft", (object?)session.AvgFrametimeMs ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$cpu", session.AvgCpuPct);
        cmd.Parameters.AddWithValue("$gpu", (object?)session.AvgGpuPct ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$src", session.Source);
        cmd.Parameters.AddWithValue("$notes", (object?)session.Notes ?? DBNull.Value);
        cmd.ExecuteNonQuery();
        using var idCmd = conn.CreateCommand();
        idCmd.CommandText = "SELECT last_insert_rowid()";
        return Convert.ToInt64(idCmd.ExecuteScalar());
    }

    public List<BenchmarkSession> ListBenchmarks()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT id, label, taken_at, duration_secs, avg_fps, pct1_low, pct01_low,
                   avg_frametime_ms, avg_cpu_pct, avg_gpu_pct, source, notes
            FROM benchmarks ORDER BY id DESC LIMIT 20
            """;
        using var reader = cmd.ExecuteReader();
        var list = new List<BenchmarkSession>();
        while (reader.Read())
        {
            list.Add(new BenchmarkSession
            {
                Id = reader.GetInt64(0),
                Label = reader.GetString(1),
                TakenAt = reader.GetString(2),
                DurationSecs = (uint)reader.GetInt64(3),
                AvgFps = reader.IsDBNull(4) ? null : (float)reader.GetDouble(4),
                Pct1Low = reader.IsDBNull(5) ? null : (float)reader.GetDouble(5),
                Pct01Low = reader.IsDBNull(6) ? null : (float)reader.GetDouble(6),
                AvgFrametimeMs = reader.IsDBNull(7) ? null : (float)reader.GetDouble(7),
                AvgCpuPct = (float)reader.GetDouble(8),
                AvgGpuPct = reader.IsDBNull(9) ? null : (float)reader.GetDouble(9),
                Source = reader.GetString(10),
                Notes = reader.IsDBNull(11) ? null : reader.GetString(11),
            });
        }

        return list;
    }

    public void MarkSessionStart()
    {
        _showCrashWatchdog = IsCrashDirty();
        SetCrashDirty(true);
    }

    public void MarkSessionCleanExit()
    {
        SetCrashDirty(false);
        _showCrashWatchdog = false;
    }

    public bool ShouldShowCrashWatchdog() => _showCrashWatchdog;

    public void DismissCrashWatchdog()
    {
        _showCrashWatchdog = false;
        SetCrashDirty(false);
    }

    private SqliteConnection Open()
    {
        var conn = new SqliteConnection($"Data Source={_path}");
        conn.Open();
        return conn;
    }

    private void Migrate()
    {
        using var conn = Open();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS snapshots (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                label TEXT NOT NULL,
                taken_at TEXT NOT NULL,
                state_json TEXT NOT NULL,
                committed_known_good INTEGER NOT NULL DEFAULT 0
            );
            CREATE TABLE IF NOT EXISTS pending_revert (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                snapshot_id INTEGER,
                reason TEXT NOT NULL,
                created_at TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS applied_tweaks (
                tweak_id TEXT PRIMARY KEY,
                previous_value TEXT,
                applied_at TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS settings (key TEXT PRIMARY KEY, value TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS crash_flag (id INTEGER PRIMARY KEY CHECK (id = 1), dirty INTEGER NOT NULL DEFAULT 0);
            INSERT OR IGNORE INTO crash_flag (id, dirty) VALUES (1, 0);
            CREATE TABLE IF NOT EXISTS profiles (
                id TEXT PRIMARY KEY,
                name TEXT NOT NULL,
                executable TEXT NOT NULL,
                tweak_ids TEXT NOT NULL DEFAULT '[]',
                watcher_enabled INTEGER NOT NULL DEFAULT 0,
                launch_options TEXT NOT NULL DEFAULT '',
                notes TEXT NOT NULL DEFAULT '[]'
            );
            CREATE TABLE IF NOT EXISTS benchmarks (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                label TEXT NOT NULL,
                taken_at TEXT NOT NULL,
                duration_secs INTEGER NOT NULL,
                avg_fps REAL,
                pct1_low REAL,
                pct01_low REAL,
                avg_frametime_ms REAL,
                avg_cpu_pct REAL NOT NULL,
                avg_gpu_pct REAL,
                source TEXT NOT NULL,
                notes TEXT
            );
            """;
        cmd.ExecuteNonQuery();
    }
}
