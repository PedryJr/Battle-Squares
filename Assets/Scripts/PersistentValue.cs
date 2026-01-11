using System;
using System.IO;
using System.Threading;
using System.Runtime.CompilerServices;

public unsafe sealed class PersistentValue<T> where T : unmanaged
{
    private readonly string _path;
    private T _value;

    private int _writeScheduled;
    private long _lastWriteRequestTicks;

    private const int DebounceMs = 350;

    public PersistentValue(string key, T defaultValue)
    {
        _path = GetPathForKey(key);

        if (File.Exists(_path))
            Load(out _value);
        else
            _value = defaultValue;
    }

    public T Value
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            _value = value;
            QueueWriteDebounced();
        }
    }

    private void Load(out T value)
    {
        try
        {
            using var fs = new FileStream(
                _path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                sizeof(T),
                false);

            if (fs.Length != sizeof(T))
            {
                value = default;
                return;
            }

            T temp;
            fs.Read(new Span<byte>(&temp, sizeof(T)));
            value = temp;
        }
        catch
        {
            value = default;
        }
    }

    private void QueueWriteDebounced()
    {
        Volatile.Write(ref _lastWriteRequestTicks, DateTime.UtcNow.Ticks);

        if (Interlocked.Exchange(ref _writeScheduled, 1) == 1)
            return;

        ThreadPool.QueueUserWorkItem(static state =>
        {
            ((PersistentValue<T>)state).WriteWorker();
        }, this);
    }

    private void WriteWorker()
    {
        while (true)
        {
            long lastTicks = Volatile.Read(ref _lastWriteRequestTicks);
            long targetTicks = lastTicks + TimeSpan.TicksPerMillisecond * DebounceMs;

            long now;
            while ((now = DateTime.UtcNow.Ticks) < targetTicks)
            {
                int sleep = (int)((targetTicks - now) / TimeSpan.TicksPerMillisecond);
                if (sleep > 0)
                    Thread.Sleep(sleep);
            }

            if (Volatile.Read(ref _lastWriteRequestTicks) != lastTicks) continue;

            try
            {
                using var fs = new FileStream(
                    _path,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    sizeof(T),
                    false);

                T temp = _value;
                fs.Write(new ReadOnlySpan<byte>(&temp, sizeof(T)));
            }
            catch
            {
            }
            finally
            {
                Interlocked.Exchange(ref _writeScheduled, 0);
            }

            return;
        }
    }

    private static string GetPathForKey(string key) => Path.Combine(SaveManager.smallValuesPath, key + ".bsl");
}
