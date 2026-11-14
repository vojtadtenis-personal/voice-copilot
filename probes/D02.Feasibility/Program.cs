using D02.Feasibility;
using System.Runtime.InteropServices;

Console.WriteLine($"D02 diagnostic: {RuntimeInformation.FrameworkDescription}; {Environment.OSVersion}; {RuntimeInformation.ProcessArchitecture}; userInteractive={Environment.UserInteractive}");
if (args.Length != 1)
{
    Console.Error.WriteLine("Choose sdk-intercept, asr, ui-local, or ui-canary.");
    return 1;
}

var results = new ProbeResults();
try
{
    switch (args[0])
    {
        case "sdk-intercept":
            await SdkProbes.RunAsync(results);
            break;
        case "asr":
            await AsrProbes.RunAsync(results);
            break;
        case "ui-local":
            await UiProbes.RunAsync(results, canary: false);
            break;
        case "ui-canary":
            await UiProbes.RunAsync(results, canary: true);
            break;
        default:
            throw new ArgumentException("Unknown probe.");
    }
}
catch (Exception error)
{
    // Runtime error text can contain account data, prompt text, and paths.
    results.Add(args[0], "FAILED", $"exceptionType={error.GetType().Name}; no raw exception exported");
}
return results.Failed ? 1 : 0;
