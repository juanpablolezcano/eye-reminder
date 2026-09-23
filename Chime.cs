using System.IO;
using System.Media;

namespace EyeReminder;

/// <summary>
/// The reminder sound. By default it synthesises a soft two-note bell so the app ships
/// without audio assets and without borrowing a Windows system sound (those share their
/// timbre with error alerts, which reads as "something went wrong" rather than "look away").
/// A custom .wav can be supplied through settings instead.
/// </summary>
internal static class Chime
{
    private static SoundPlayer? _chimePlayer;
    private static double _chimeVolume = -1;

    private static SoundPlayer? _filePlayer;
    private static string? _filePlayerPath;

    public static void Play(Settings settings)
    {
        if (!settings.Sound) return;

        try
        {
            var player = Resolve(settings);
            player?.Play(); // asynchronous: never blocks the UI thread
        }
        catch
        {
            // A missing codec or a locked audio device must not break the reminder.
        }
    }

    private static SoundPlayer? Resolve(Settings settings)
    {
        var custom = settings.SoundFile?.Trim();

        if (!string.IsNullOrEmpty(custom))
        {
            var path = Path.IsPathRooted(custom)
                ? custom
                : Path.Combine(AppContext.BaseDirectory, custom);

            if (File.Exists(path))
            {
                if (_filePlayer is null || !string.Equals(_filePlayerPath, path, StringComparison.OrdinalIgnoreCase))
                {
                    var player = new SoundPlayer(path);
                    player.Load(); // throws here rather than silently failing on Play
                    _filePlayer = player;
                    _filePlayerPath = path;
                }

                return _filePlayer;
            }
            // Falls through to the built-in chime when the file is missing.
        }

        // Built once and reused. Load() copies the samples out, so the player keeps working
        // after the stream is gone, and no SoundPlayer is left for the finaliser each break.
        if (_chimePlayer is null || Math.Abs(_chimeVolume - settings.SoundVolume) > 0.001)
        {
            var wav = GenerateChime(settings.SoundVolume);

            using var samples = new MemoryStream(wav, writable: false);
            var player = new SoundPlayer(samples);
            player.Load();

            _chimePlayer?.Dispose();
            _chimePlayer = player;
            _chimeVolume = settings.SoundVolume;
        }

        return _chimePlayer;
    }

    // ---- synthesis --------------------------------------------------------

    private const int SampleRate = 44100;

    private static byte[] GenerateChime(double volume)
    {
        var samples = new double[(int)(SampleRate * 1.5)];

        // E5 then B5 a beat later: a rising fifth, which reads as a gentle prompt.
        AddBellNote(samples, freq: 659.25, startSeconds: 0.00, lengthSeconds: 1.40, amplitude: 1.00);
        AddBellNote(samples, freq: 987.77, startSeconds: 0.17, lengthSeconds: 1.30, amplitude: 0.80);

        var peak = 0.0;
        foreach (var s in samples) peak = Math.Max(peak, Math.Abs(s));
        var gain = peak > 0 ? volume / peak : 0;

        return WriteWav(samples, gain);
    }

    private static void AddBellNote(double[] buffer, double freq, double startSeconds, double lengthSeconds, double amplitude)
    {
        var start = (int)(startSeconds * SampleRate);
        var length = (int)(lengthSeconds * SampleRate);

        for (var i = 0; i < length && start + i < buffer.Length; i++)
        {
            var t = (double)i / SampleRate;

            var decay = Math.Exp(-3.0 * t);            // bell-like exponential tail
            var attack = Math.Min(1.0, t / 0.008);     // short ramp so the onset does not click

            // Fundamental plus two quiet harmonics gives it body without sounding synthetic.
            var wave = Math.Sin(2 * Math.PI * freq * t)
                     + 0.26 * Math.Sin(2 * Math.PI * freq * 2 * t)
                     + 0.09 * Math.Sin(2 * Math.PI * freq * 3 * t);

            buffer[start + i] += amplitude * decay * attack * wave;
        }
    }

    private static byte[] WriteWav(double[] samples, double gain)
    {
        var dataBytes = samples.Length * 2; // 16-bit mono

        using var stream = new MemoryStream(44 + dataBytes);
        using var w = new BinaryWriter(stream);

        w.Write("RIFF"u8.ToArray());
        w.Write(36 + dataBytes);
        w.Write("WAVE"u8.ToArray());
        w.Write("fmt "u8.ToArray());
        w.Write(16);                      // PCM header size
        w.Write((short)1);                // PCM
        w.Write((short)1);                // mono
        w.Write(SampleRate);
        w.Write(SampleRate * 2);          // byte rate
        w.Write((short)2);                // block align
        w.Write((short)16);               // bits per sample
        w.Write("data"u8.ToArray());
        w.Write(dataBytes);

        foreach (var sample in samples)
        {
            var scaled = Math.Clamp(sample * gain, -1.0, 1.0);
            w.Write((short)(scaled * short.MaxValue));
        }

        w.Flush();
        return stream.ToArray();
    }
}
