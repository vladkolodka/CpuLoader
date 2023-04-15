using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

var appCts = new CancellationTokenSource();

AppDomain.CurrentDomain.ProcessExit += (_, _) =>
{
    if (!appCts.IsCancellationRequested)
    {
        appCts.Cancel();
        Thread.Sleep(500);
    }

    Console.WriteLine("Exited");
};

var mappedArgs = MapArgs(args);

var mappedArgsString = JsonSerializer.Serialize(mappedArgs, new JsonSerializerOptions
{
    WriteIndented = true
});

Console.WriteLine($"Starting with parameters: {mappedArgsString}");

var threadsPercentage = Convert.ToDouble(mappedArgs["vProcCountToUsePercentage"], NumberFormatInfo.InvariantInfo);
var loadPerThread = Convert.ToInt32(mappedArgs["procLoadPercentage"]);

var minsToWork =
    TimeSpan.FromMinutes(Convert.ToDouble(mappedArgs["workMinutes"], NumberFormatInfo.InvariantInfo));

var minsToWait =
    TimeSpan.FromMinutes(Convert.ToDouble(mappedArgs["waitMinutes"], NumberFormatInfo.InvariantInfo));

var threadsNum = (int) (Environment.ProcessorCount * threadsPercentage);

var appTask = Task.Factory.StartNew(_ => AppLifecycle(appCts.Token), appCts.Token, TaskCreationOptions.LongRunning);

Console.WriteLine("Press any key to finish the process.");
Console.WriteLine();

if (!Environment.UserInteractive)
{
    Console.ReadKey();
    appCts.Cancel();
    Console.WriteLine("Stopping loader threads...");
    Thread.Sleep(500);
}
else
{
    await appTask.Unwrap();
}

async Task AppLifecycle(CancellationToken appCancellationToken)
{
    List<Thread> loaderThreads = null;

    void PulseThreads() => loaderThreads?.ForEach(threadAsMonitor =>
    {
        lock (threadAsMonitor)
        {
            Monitor.Pulse(threadAsMonitor);
        }
    });

    try
    {
        while (true)
        {
            appCancellationToken.ThrowIfCancellationRequested();

            Console.WriteLine($"Starting new loader run for [{minsToWork.TotalMinutes}] minutes...");

            var runCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(appCancellationToken);

            loaderThreads = Enumerable.Range(0, threadsNum).Select(i =>
                new Thread(monitor =>
                {
                    Console.WriteLine($"Started thread [{i}];");
                    ConsumeCpu(loadPerThread, monitor, runCancellationSource.Token);
                    Console.WriteLine($"Stopped thread [{i}];");
                }) {IsBackground = true}
            ).ToList();

            loaderThreads.ForEach(threadAsMonitor => threadAsMonitor.Start(threadAsMonitor));

            await Task.Delay(minsToWork, appCancellationToken);

            runCancellationSource.Cancel();
            PulseThreads();

            Console.WriteLine($"Waiting for [{minsToWait.TotalMinutes}] minutes before next load run");
            await Task.Delay(minsToWait, appCancellationToken);

            // TODO check main cancellation
        }
    }
    catch (TaskCanceledException)
    {
        Console.WriteLine("Loader run cancelled");
        PulseThreads();
    }
}

static Dictionary<string, string> MapArgs(string[] strings)
{
    var defaultArgs = new Dictionary<string, string>
    {
        ["vProcCountToUsePercentage"] = "0.8",
        ["procLoadPercentage"] = "40",
        ["workMinutes"] = "10",
        ["waitMinutes"] = "50"
    };
    // TODO time to work and time to wait

    var mappedArgs = strings
        .Where(arg => !string.IsNullOrWhiteSpace(arg))
        .Select(arg => arg.Split(":"))
        .ToDictionary(argParts => argParts[0], argParts => argParts.Length > 1
            ? argParts[1]
            : string.Empty
        );

    foreach (var pair in defaultArgs)
    {
        mappedArgs.TryAdd(pair.Key, pair.Value);
    }

    return mappedArgs;
}

static void ConsumeCpu(int percentage, object monitor, CancellationToken cancellationToken)
{
    if (percentage is < 0 or > 100)
    {
        throw new ArgumentException("Invalid value", nameof(percentage));
    }

    var watch = new Stopwatch();
    watch.Start();

    while (true)
    {
        lock (monitor)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            if (watch.ElapsedMilliseconds <= percentage)
            {
                continue;
            }

            Monitor.Wait(monitor, 100 - percentage);

            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }

        watch.Reset();
        watch.Start();
    }
}