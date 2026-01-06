using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using Newtonsoft.Json;

internal static class PersistentValueManager
{
    private static readonly ConcurrentDictionary<IPersistentValue, byte> values = new();
    public static void Register(IPersistentValue pv) => values[pv] = 0;

    public static void SaveAll()
    {
        foreach (var pv in values.Keys) pv.SaveImmediateInternal();
    }

    static PersistentValueManager()
    {
        AppDomain.CurrentDomain.ProcessExit += (_, __) => SaveAll();
        AppDomain.CurrentDomain.DomainUnload += (_, __) => SaveAll();
    }
}

internal interface IPersistentValue
{
    void SaveImmediateInternal();
}

public sealed class PersistentValue<T> : IPersistentValue
{
    private readonly string filePath;
    private T value;
    private readonly object lockObj = new();
    private Timer? debounceTimer;
    private readonly TimeSpan debounceDelay = TimeSpan.FromSeconds(0.5); // adjustable

    public T Value
    {
        get
        {
            lock (lockObj) return value;
        }
        set
        {
            lock (lockObj)
            {
                this.value = value;
                // Reset debounce timer
                debounceTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                debounceTimer ??= new Timer(_ => SaveDebounced(), null, Timeout.Infinite, Timeout.Infinite);
                debounceTimer.Change(debounceDelay, Timeout.InfiniteTimeSpan);
            }
        }
    }

    public PersistentValue(string key, T defaultValue)
    {
        string directory = SaveManager.saveFolderPath;

        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        foreach (var c in Path.GetInvalidFileNameChars())
            key = key.Replace(c, '_');

        filePath = Path.Combine(directory, key + ".json");

        if (File.Exists(filePath))
            Load(defaultValue);
        else
        {
            value = defaultValue;
            SaveImmediateInternal();
        }

        PersistentValueManager.Register(this);
    }

    private void Load(T defaultValue)
    {
        try
        {
            string json = File.ReadAllText(filePath);
            value = JsonConvert.DeserializeObject<T>(json) ?? defaultValue;
        }
        catch
        {
            value = defaultValue;
            SaveImmediateInternal();
        }
    }

    void IPersistentValue.SaveImmediateInternal() => SaveImmediateInternal();

    internal void SaveImmediateInternal()
    {
        lock (lockObj)
        {
            try
            {
                string json = JsonConvert.SerializeObject(value, Formatting.Indented);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to save PersistentValue at {filePath}: {ex.Message}");
            }
        }
    }

    private void SaveDebounced()
    {
        lock (lockObj)
        {
            SaveImmediateInternal();
            debounceTimer?.Dispose();
            debounceTimer = null;
        }
    }

    public void Reset(T newValue)
    {
        lock (lockObj)
        {
            value = newValue;
            SaveImmediateInternal();
        }
    }
}

