using NAudio.CoreAudioApi;
using NAudio.Wave;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Whisper.net;
using Whisper.net.LibraryLoader;

namespace D02.Feasibility;

internal static class AsrProbes
{
    private static readonly ModelAsset[] Models =
    [
        new("ggml-base.en-q5_1.bin", 59721011, "4baf70dd0d7c4247ba2b81fafd9c01005ac77c2f9ef064e00dcf195d0e2fdd2f"),
        new("ggml-small.en-q5_1.bin", 190098681, "bfdff4894dcb76bbf647d56263ea2a96645423f1669176f4844a1bf8e478ad30"),
        new("ggml-silero-v6.2.0.bin", 885098, "2aa269b785eeb53a82983a20501ddf7c1d9c48e33ab63a41391ac6c9f7fb6987"),
    ];

    public static async Task RunAsync(ProbeResults results)
    {
        foreach (var model in Models)
        {
            var path = ModelPath(model.Name);
            if (!File.Exists(path))
            {
                if (model.Name.Contains("small", StringComparison.Ordinal))
                {
                    results.Add("ASR.SMALL", "BLOCKED", "Optional small model not acquired; no comparison claimed.");
                    continue;
                }
                throw new FileNotFoundException("Run Get-Models.ps1 before native loading.");
            }
            using var stream = File.OpenRead(path);
            var hash = Convert.ToHexStringLower(SHA256.HashData(stream));
            var verified = stream.Length == model.Bytes && hash == model.Sha256;
            results.Check($"ASR.INTEGRITY.{model.Name}", verified, $"bytes={stream.Length}; sha256={hash}; checked before native load");
            if (!verified)
                throw new InvalidDataException("Native load forbidden on integrity mismatch.");
        }

        RuntimeOptions.RuntimeLibraryOrder = [RuntimeLibrary.Cpu];
        var ordinary = ReadFixture("ordinary", results);
        var domain = ReadFixture("domain", results);
        EnumerateDevices(results);
        using var vadFactory = WhisperVadFactory.FromPath(ModelPath("ggml-silero-v6.2.0.bin"));
        using var vad = vadFactory.CreateBuilder().WithUseGpu(false).WithThreads(4)
            .WithThreshold(0.5f).WithMinSilenceDuration(TimeSpan.FromMilliseconds(250)).Build();
        var silence = new float[16000 * 2];
        results.Check("ASR.VAD_SILENCE", vad.DetectSpeech(silence).Count == 0, "Two seconds of injected zero samples: no speech segments.");
        var segments = vad.DetectSpeech(ordinary);
        results.Check("ASR.VAD_SPEECH", segments.Count > 0, $"synthetic ordinary speech segments={segments.Count}");
        vad.ResetState();
        var repeated = vad.DetectSpeechNoReset(ordinary);
        results.Check("ASR.VAD_RESET", segments.Select(SegmentKey).SequenceEqual(repeated.Select(SegmentKey)),
            "Explicit ResetState plus DetectSpeechNoReset matches one-shot DetectSpeech on the same fixture.");
        vad.ResetState();
        var windowSegments = 0;
        for (var offset = 0; offset < ordinary.Length; offset += 512)
            windowSegments += vad.DetectSpeechNoReset(ordinary.AsSpan(offset, Math.Min(512, ordinary.Length - offset))).Count;
        results.Add("ASR.VAD_WINDOWS", "OBSERVED",
            $"windowSamples=512; windowSegments={windowSegments}; oneShotSegments={segments.Count}; short-frame segment emission not suitable here; evaluate official ONNX fallback");
        await OnnxVadProbes.RunAsync(ordinary, silence, results);
        results.Check("ASR.CPU", RuntimeOptions.LoadedLibrary == RuntimeLibrary.Cpu, $"loadedRuntime={RuntimeOptions.LoadedLibrary}; GPU disabled; threads=4");

        using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        foreach (var model in Models.Where(model => !model.Name.Contains("silero", StringComparison.Ordinal) && File.Exists(ModelPath(model.Name))))
        {
            var load = Stopwatch.StartNew();
            using var factory = WhisperFactory.FromPath(ModelPath(model.Name), new WhisperFactoryOptions { UseGpu = false });
            load.Stop();
            results.Add($"ASR.LOAD.{model.Name}", "OBSERVED", $"loadMs={load.Elapsed.TotalMilliseconds:F0}; CPU only");
            await Benchmark(factory, model.Name, "ordinary", ordinary,
                "Create a new session. Open the file and stop the task.", null, results, deadline.Token);
            await Benchmark(factory, model.Name, "domain", domain,
                "Use NAudio with Whisper and Copilot. Keep the project path unchanged.", null, results, deadline.Token);
            await Benchmark(factory, model.Name, "domain-hint", domain,
                "Use NAudio with Whisper and Copilot. Keep the project path unchanged.",
                "Vocabulary: NAudio, Whisper, Copilot.", results, deadline.Token);
        }
        using var process = Process.GetCurrentProcess();
        results.Add("ASR.RESOURCES", "OBSERVED",
            $"peakWorkingSetBytes={process.PeakWorkingSet64}; logicalProcessors={Environment.ProcessorCount}; shared reference host only, not universal performance");
        results.Add("ASR.LIMITS", "OBSERVED", "Synthetic two-phrase corpus only; no microphone recording, device reconnect, noise corpus, long dictation, NoAvx or GPU qualification.");
    }

    private static async Task Benchmark(WhisperFactory factory, string model, string fixture, float[] samples,
        string reference, string? hint, ProbeResults results, CancellationToken ct)
    {
        var builder = factory.CreateBuilder().WithLanguage("en").WithThreads(4).WithNoContext().WithTemperature(0);
        if (hint is not null)
            builder.WithPrompt(hint);
        await using var processor = builder.Build();
        var texts = new List<string>();
        var watch = Stopwatch.StartNew();
        await foreach (var segment in processor.ProcessAsync(samples, ct))
            texts.Add(segment.Text);
        watch.Stop();
        var hypothesis = string.Join(" ", texts).Trim();
        var seconds = samples.Length / 16000d;
        var wer = WordErrorRate(reference, hypothesis);
        results.Check($"ASR.TRANSCRIPTION.{model}.{fixture}", hypothesis.Length > 0,
            $"owned synthetic output={hypothesis}");
        results.Add($"ASR.BENCHMARK.{model}.{fixture}", "OBSERVED",
            FormattableString.Invariant($"audioSeconds={seconds:F3}; processMs={watch.Elapsed.TotalMilliseconds:F0}; RTF={watch.Elapsed.TotalSeconds / seconds:F3}; WER={wer:F3}; initialPrompt={hint is not null}; not an accuracy acceptance threshold"));
    }

    private static float[] ReadFixture(string name, ProbeResults results)
    {
        var path = Path.Combine(".assets", "audio", $"{name}.wav");
        using var reader = new WaveFileReader(path);
        var format = reader.WaveFormat;
        var valid = format.SampleRate == 16000 && format.Channels == 1 && format.BitsPerSample == 16 &&
            format.Encoding == WaveFormatEncoding.Pcm && reader.TotalTime.TotalSeconds is > 0 and < 120;
        results.Check($"ASR.WAV.{name}", valid,
            $"rate={format.SampleRate}; channels={format.Channels}; bits={format.BitsPerSample}; seconds={reader.TotalTime.TotalSeconds:F3}; synthetic file, never a capture");
        if (!valid)
            throw new InvalidDataException("Fixture must be bounded mono16k PCM16.");
        var provider = reader.ToSampleProvider();
        var samples = new float[checked((int)(reader.Length / 2))];
        var read = provider.Read(samples);
        if (read != samples.Length)
            throw new InvalidDataException("Incomplete fixture decoding.");
        return samples;
    }

    private static void EnumerateDevices(ProbeResults results)
    {
        using var enumerator = new MMDeviceEnumerator();
        var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
        results.Add("ASR.DEVICE_METADATA", "OBSERVED", $"activeCaptureDeviceCount={devices.Count}; no device names/IDs or audio captured");
        foreach (var device in devices)
        {
            using (device)
            using (var client = device.CreateAudioClient())
            {
                var format = client.MixFormat;
                results.Add("ASR.DEVICE_FORMAT", "OBSERVED", $"mixRate={format.SampleRate}; channels={format.Channels}; bits={format.BitsPerSample}; AudioClient never initialized/started");
            }
        }
    }

    private static double WordErrorRate(string reference, string hypothesis)
    {
        static string[] Words(string text) => Regex.Matches(text.ToLower(CultureInfo.InvariantCulture), @"[\p{L}\p{N}]+")
            .Select(match => match.Value).ToArray();
        var expected = Words(reference);
        var actual = Words(hypothesis);
        var previous = Enumerable.Range(0, actual.Length + 1).ToArray();
        for (var i = 1; i <= expected.Length; i++)
        {
            var current = new int[actual.Length + 1];
            current[0] = i;
            for (var j = 1; j <= actual.Length; j++)
                current[j] = Math.Min(Math.Min(previous[j] + 1, current[j - 1] + 1),
                    previous[j - 1] + (expected[i - 1] == actual[j - 1] ? 0 : 1));
            previous = current;
        }
        return previous[^1] / (double)expected.Length;
    }

    private static string ModelPath(string name) => Path.Combine(".assets", "models", name);
    private static string SegmentKey(VadSegmentData segment) => $"{segment.Start.Ticks}-{segment.End.Ticks}";
    private sealed record ModelAsset(string Name, long Bytes, string Sha256);
}
