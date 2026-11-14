using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;

namespace D02.Feasibility;

internal static class OnnxVadProbes
{
    public static Task RunAsync(float[] speech, float[] silence, ProbeResults results)
    {
        var path = Path.Combine(".assets", "models", "silero_vad.onnx");
        using (var file = File.OpenRead(path))
        {
            var hash = Convert.ToHexStringLower(SHA256.HashData(file));
            var verified = file.Length == 2327524 && hash == "1a153a22f4509e292a94e67d6f9b85e8deb25b4988682b7e174c65279d8788e3";
            results.Check("ASR.ONNX_INTEGRITY", verified, $"sha256={hash}; checked before native load");
            if (!verified)
                throw new InvalidDataException("ONNX native loading forbidden on hash mismatch.");
        }
        using var options = new SessionOptions { InterOpNumThreads = 1, IntraOpNumThreads = 1 };
        using var session = new InferenceSession(path, options);
        var graphMatches = session.InputMetadata.Keys.Order().SequenceEqual(new[] { "input", "sr", "state" }) &&
            session.OutputMetadata.Keys.Order().SequenceEqual(new[] { "output", "stateN" });
        results.Check("ASR.ONNX_GRAPH", graphMatches, "Official v6.2.3 graph: input/state/sr -> output/stateN; CPU package, no external inference.");
        if (!graphMatches)
            throw new InvalidDataException("Unknown official graph.");
        var watch = Stopwatch.StartNew();
        var probabilities = Evaluate(session, speech);
        watch.Stop();
        var quiet = Evaluate(session, silence);
        results.Check("ASR.ONNX_FRAME_SPEECH", probabilities.Any(p => p >= 0.5f),
            $"frames={probabilities.Length}; speechFrames={probabilities.Count(p => p >= 0.5f)}; 512 new samples + 64 context, 16000 Hz");
        results.Check("ASR.ONNX_FRAME_SILENCE", quiet.All(p => p < 0.5f), "Injected silence stays below 0.5 with freshly reset hidden state/context.");
        results.Check("ASR.ONNX_RESET", probabilities.SequenceEqual(Evaluate(session, speech)),
            "Reset state/context produces identical probabilities on the same owned fixture.");
        results.Add("ASR.ONNX_BENCHMARK", "OBSERVED",
            $"processMs={watch.Elapsed.TotalMilliseconds:F0}; audioSeconds={speech.Length / 16000d:F3}; bounded per-frame CPU state; no utterance assembler/pre-roll qualification claimed");
        return Task.CompletedTask;
    }

    private static float[] Evaluate(InferenceSession session, float[] samples)
    {
        // Mirror the official v6.2.3 OnnxWrapper contract, not a production VAD pipeline.
        var state = new float[2 * 128];
        var context = new float[64];
        var probabilities = new List<float>();
        for (var offset = 0; offset < samples.Length; offset += 512)
        {
            var input = new float[576];
            context.CopyTo(input, 0);
            Array.Copy(samples, offset, input, 64, Math.Min(512, samples.Length - offset));
            var inputs = new[]
            {
                NamedOnnxValue.CreateFromTensor("input", new DenseTensor<float>(input, new[] { 1, 576 })),
                NamedOnnxValue.CreateFromTensor("state", new DenseTensor<float>(state, new[] { 2, 1, 128 })),
                NamedOnnxValue.CreateFromTensor("sr", new DenseTensor<long>(new[] { 16000L }, Array.Empty<int>())),
            };
            using var outputs = session.Run(inputs);
            var probability = outputs.First(value => value.Name == "output").AsEnumerable<float>().Single();
            if (!float.IsFinite(probability) || probability is < 0 or > 1)
                throw new InvalidDataException("Invalid native VAD probability.");
            probabilities.Add(probability);
            state = outputs.First(value => value.Name == "stateN").AsEnumerable<float>().ToArray();
            Array.Copy(input, 512, context, 0, 64);
        }
        return probabilities.ToArray();
    }
}
