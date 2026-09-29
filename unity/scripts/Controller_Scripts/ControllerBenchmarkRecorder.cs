using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>
/// Lightweight benchmark recorder for navigation-controller experiments.
///
/// What it measures:
/// 1) Method-specific computation time per controller invocation (ms).
/// 2) Effective controller invocation frequency (Hz).
/// 3) Unity process CPU utilization during the valid mission window.
/// 4) Hardware/software metadata.
///
/// The benchmark is active only between BeginRun(...) and EndRun(...).
/// Raw samples and one summary row per run are written automatically.
/// </summary>
public class ControllerBenchmarkRecorder : MonoBehaviour
{
    public static ControllerBenchmarkRecorder Instance { get; private set; }

    [Header("Output")]
    public bool writeRawSamples = true;
    public bool logSummaryToConsole = true;
    public string outputFolderName = "ControllerBenchmark";

    [Header("CPU sampling")]
    [Tooltip("Process CPU sampling period in seconds.")]
    public float cpuSamplingPeriodSeconds = 1.0f;

    private bool recording = false;

    private string activeMethod = "UNKNOWN";
    private string activeScenario = "UNKNOWN";
    private int activeRunID = -1;

    private readonly List<double> computationMs = new List<double>();
    private readonly List<double> invocationTimesSec = new List<double>();
    private readonly List<double> cpuTotalCapacityPct = new List<double>();
    private readonly List<double> cpuOneCoreEquivalentPct = new List<double>();

    private readonly Stopwatch wallClock = new Stopwatch();
    private Process currentProcess;

    private TimeSpan previousCpuTime;
    private double previousCpuWallSec = 0.0;
    private double nextCpuSampleWallSec = 0.0;

    private string outputDirectory;

    public static bool IsRecording =>
        Instance != null &&
        Instance.recording;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            UnityEngine.Debug.LogWarning(
                "[BENCHMARK] Ya existe otro ControllerBenchmarkRecorder. " +
                "Se destruye la copia duplicada."
            );

            Destroy(this);
            return;
        }

        Instance = this;

        outputDirectory = Path.Combine(
            Application.persistentDataPath,
            outputFolderName
        );

        Directory.CreateDirectory(outputDirectory);

        currentProcess = Process.GetCurrentProcess();

        UnityEngine.Debug.Log(
            "[BENCHMARK] Recorder listo.\n" +
            "Output = " + outputDirectory
        );
    }

    private void Update()
    {
        if (!recording)
        {
            return;
        }

        double now = wallClock.Elapsed.TotalSeconds;

        if (now >= nextCpuSampleWallSec)
        {
            SampleCpu();
            nextCpuSampleWallSec =
                now + Math.Max(0.25, cpuSamplingPeriodSeconds);
        }
    }

    /// <summary>
    /// Starts one benchmark run. Call exactly when the valid mission window starts.
    /// </summary>
    public static void BeginRun(
        string method,
        string scenario,
        int runID)
    {
        if (Instance == null)
        {
            UnityEngine.Debug.LogWarning(
                "[BENCHMARK] BeginRun ignorado: no existe " +
                "ControllerBenchmarkRecorder en la escena."
            );
            return;
        }

        Instance.BeginRunInternal(
            method,
            scenario,
            runID
        );
    }

    /// <summary>
    /// Starts a method-specific timing sample.
    /// Returns 0 when the benchmark is inactive.
    /// </summary>
    public static long BeginCycle()
    {
        if (!IsRecording)
        {
            return 0L;
        }

        return Stopwatch.GetTimestamp();
    }

    /// <summary>
    /// Ends one method-specific timing sample.
    /// </summary>
    public static void EndCycle(long startTicks)
    {
        if (startTicks == 0L ||
            Instance == null ||
            !Instance.recording)
        {
            return;
        }

        long endTicks = Stopwatch.GetTimestamp();

        double elapsedMs =
            (endTicks - startTicks) *
            1000.0 /
            Stopwatch.Frequency;

        Instance.computationMs.Add(elapsedMs);
        Instance.invocationTimesSec.Add(
            Instance.wallClock.Elapsed.TotalSeconds
        );
    }

    /// <summary>
    /// Ends the benchmark run, exports raw samples and appends one summary row.
    /// </summary>
    public static void EndRun(int terminalStatus)
    {
        if (Instance == null ||
            !Instance.recording)
        {
            return;
        }

        Instance.EndRunInternal(terminalStatus);
    }

    private void BeginRunInternal(
        string method,
        string scenario,
        int runID)
    {
        if (recording)
        {
            UnityEngine.Debug.LogWarning(
                "[BENCHMARK] Había una medición activa. " +
                "Se cierra antes de comenzar la nueva."
            );

            EndRunInternal(-999);
        }

        activeMethod =
            string.IsNullOrWhiteSpace(method)
            ? "UNKNOWN"
            : method;

        activeScenario =
            string.IsNullOrWhiteSpace(scenario)
            ? "UNKNOWN"
            : scenario;

        activeRunID = runID;

        computationMs.Clear();
        invocationTimesSec.Clear();
        cpuTotalCapacityPct.Clear();
        cpuOneCoreEquivalentPct.Clear();

        currentProcess.Refresh();

        wallClock.Reset();
        wallClock.Start();

        previousCpuTime =
            currentProcess.TotalProcessorTime;

        previousCpuWallSec = 0.0;
        nextCpuSampleWallSec =
            Math.Max(0.25, cpuSamplingPeriodSeconds);

        recording = true;

        UnityEngine.Debug.Log(
            "========================================\n" +
            "[BENCHMARK] RUN START\n" +
            "Method = " + activeMethod + "\n" +
            "Scenario = " + activeScenario + "\n" +
            "Run = " + activeRunID + "\n" +
            "Output = " + outputDirectory + "\n" +
            "========================================"
        );
    }

    private void SampleCpu()
    {
        if (!recording)
        {
            return;
        }

        currentProcess.Refresh();

        double nowWallSec =
            wallClock.Elapsed.TotalSeconds;

        TimeSpan nowCpu =
            currentProcess.TotalProcessorTime;

        double deltaWall =
            nowWallSec - previousCpuWallSec;

        double deltaCpu =
            (nowCpu - previousCpuTime).TotalSeconds;

        if (deltaWall > 1e-9)
        {
            // 100% = one fully occupied logical processor.
            double oneCoreEq =
                (deltaCpu / deltaWall) * 100.0;

            // 100% = all logical processors fully occupied.
            double totalCapacity =
                oneCoreEq /
                Math.Max(1, Environment.ProcessorCount);

            cpuOneCoreEquivalentPct.Add(oneCoreEq);
            cpuTotalCapacityPct.Add(totalCapacity);
        }

        previousCpuWallSec = nowWallSec;
        previousCpuTime = nowCpu;
    }

    private void EndRunInternal(int terminalStatus)
    {
        // Capture the final partial CPU interval if sufficiently long.
        double remainingCpuWindow =
            wallClock.Elapsed.TotalSeconds -
            previousCpuWallSec;

        if (remainingCpuWindow >= 0.20)
        {
            SampleCpu();
        }

        recording = false;

        wallClock.Stop();

        double durationSec =
            wallClock.Elapsed.TotalSeconds;

        int n =
            computationMs.Count;

        double meanMs =
            n > 0 ? computationMs.Average() : double.NaN;

        double medianMs =
            Percentile(computationMs, 0.50);

        double p95Ms =
            Percentile(computationMs, 0.95);

        double p99Ms =
            Percentile(computationMs, 0.99);

        double maxMs =
            n > 0 ? computationMs.Max() : double.NaN;

        double controlHz =
            ComputeEffectiveFrequencyHz(
                invocationTimesSec,
                durationSec
            );

        double cpuMeanTotal =
            MeanOrNaN(cpuTotalCapacityPct);

        double cpuMedianTotal =
            Percentile(cpuTotalCapacityPct, 0.50);

        double cpuP95Total =
            Percentile(cpuTotalCapacityPct, 0.95);

        double cpuMaxTotal =
            MaxOrNaN(cpuTotalCapacityPct);

        double cpuMeanOneCore =
            MeanOrNaN(cpuOneCoreEquivalentPct);

        ExportSummary(
            terminalStatus,
            durationSec,
            n,
            meanMs,
            medianMs,
            p95Ms,
            p99Ms,
            maxMs,
            controlHz,
            cpuMeanTotal,
            cpuMedianTotal,
            cpuP95Total,
            cpuMaxTotal,
            cpuMeanOneCore
        );

        if (writeRawSamples)
        {
            ExportRawSamples();
        }

        if (logSummaryToConsole)
        {
            UnityEngine.Debug.Log(
                "========================================\n" +
                "[BENCHMARK] RUN COMPLETE\n" +
                "Method = " + activeMethod + "\n" +
                "Scenario = " + activeScenario + "\n" +
                "Run = " + activeRunID + "\n" +
                "Status = " + terminalStatus + "\n" +
                "Samples = " + n + "\n" +
                "Mean = " + Format(meanMs) + " ms\n" +
                "Median = " + Format(medianMs) + " ms\n" +
                "P95 = " + Format(p95Ms) + " ms\n" +
                "P99 = " + Format(p99Ms) + " ms\n" +
                "Max = " + Format(maxMs) + " ms\n" +
                "Control frequency = " +
                Format(controlHz) + " Hz\n" +
                "CPU mean (total capacity) = " +
                Format(cpuMeanTotal) + " %\n" +
                "CPU mean (one-core equivalent) = " +
                Format(cpuMeanOneCore) + " %\n" +
                "Output = " + outputDirectory + "\n" +
                "========================================"
            );
        }
    }

    private void ExportRawSamples()
    {
        string safeMethod =
            MakeSafeFileName(activeMethod);

        string safeScenario =
            MakeSafeFileName(activeScenario);

        string path = Path.Combine(
            outputDirectory,
            string.Format(
                CultureInfo.InvariantCulture,
                "benchmark_{0}_{1}_run{2:00}_raw.csv",
                safeMethod,
                safeScenario,
                activeRunID
            )
        );

        using (StreamWriter writer =
               new StreamWriter(path, false))
        {
            writer.WriteLine(
                "Sample,Elapsed_s,Computation_ms"
            );

            int n =
                Math.Min(
                    computationMs.Count,
                    invocationTimesSec.Count
                );

            for (int i = 0; i < n; i++)
            {
                writer.WriteLine(
                    (i + 1).ToString(
                        CultureInfo.InvariantCulture
                    ) + "," +
                    invocationTimesSec[i].ToString(
                        "F6",
                        CultureInfo.InvariantCulture
                    ) + "," +
                    computationMs[i].ToString(
                        "F9",
                        CultureInfo.InvariantCulture
                    )
                );
            }
        }
    }

    private void ExportSummary(
        int terminalStatus,
        double durationSec,
        int n,
        double meanMs,
        double medianMs,
        double p95Ms,
        double p99Ms,
        double maxMs,
        double controlHz,
        double cpuMeanTotal,
        double cpuMedianTotal,
        double cpuP95Total,
        double cpuMaxTotal,
        double cpuMeanOneCore)
    {
        string summaryPath =
            Path.Combine(
                outputDirectory,
                "benchmark_summary.csv"
            );

        bool newFile =
            !File.Exists(summaryPath);

        using (StreamWriter writer =
               new StreamWriter(summaryPath, true))
        {
            if (newFile)
            {
                writer.WriteLine(
                    "Timestamp,Method,Scenario,RunID,TerminalStatus," +
                    "Duration_s,Samples," +
                    "Mean_ms,Median_ms,P95_ms,P99_ms,Max_ms," +
                    "EffectiveControlHz," +
                    "CPU_Mean_TotalCapacity_pct," +
                    "CPU_Median_TotalCapacity_pct," +
                    "CPU_P95_TotalCapacity_pct," +
                    "CPU_Max_TotalCapacity_pct," +
                    "CPU_Mean_OneCoreEquivalent_pct," +
                    "LogicalProcessors," +
                    "ProcessorType,SystemMemoryMB," +
                    "GraphicsDevice,UnityVersion,Platform," +
                    "TargetFrameRate,VSyncCount"
                );
            }

            writer.WriteLine(
                Csv(DateTime.Now.ToString("o")) + "," +
                Csv(activeMethod) + "," +
                Csv(activeScenario) + "," +
                activeRunID.ToString(
                    CultureInfo.InvariantCulture
                ) + "," +
                terminalStatus.ToString(
                    CultureInfo.InvariantCulture
                ) + "," +
                Num(durationSec) + "," +
                n.ToString(
                    CultureInfo.InvariantCulture
                ) + "," +
                Num(meanMs) + "," +
                Num(medianMs) + "," +
                Num(p95Ms) + "," +
                Num(p99Ms) + "," +
                Num(maxMs) + "," +
                Num(controlHz) + "," +
                Num(cpuMeanTotal) + "," +
                Num(cpuMedianTotal) + "," +
                Num(cpuP95Total) + "," +
                Num(cpuMaxTotal) + "," +
                Num(cpuMeanOneCore) + "," +
                Environment.ProcessorCount.ToString(
                    CultureInfo.InvariantCulture
                ) + "," +
                Csv(SystemInfo.processorType) + "," +
                SystemInfo.systemMemorySize.ToString(
                    CultureInfo.InvariantCulture
                ) + "," +
                Csv(SystemInfo.graphicsDeviceName) + "," +
                Csv(Application.unityVersion) + "," +
                Csv(Application.platform.ToString()) + "," +
                Application.targetFrameRate.ToString(
                    CultureInfo.InvariantCulture
                ) + "," +
                QualitySettings.vSyncCount.ToString(
                    CultureInfo.InvariantCulture
                )
            );
        }
    }

    private static double ComputeEffectiveFrequencyHz(
        List<double> times,
        double durationSec)
    {
        if (times.Count >= 2)
        {
            double span =
                times[times.Count - 1] -
                times[0];

            if (span > 1e-9)
            {
                return
                    (times.Count - 1) /
                    span;
            }
        }

        if (durationSec > 1e-9)
        {
            return
                times.Count /
                durationSec;
        }

        return double.NaN;
    }

    private static double Percentile(
        List<double> values,
        double p)
    {
        if (values == null ||
            values.Count == 0)
        {
            return double.NaN;
        }

        double[] sorted =
            values.OrderBy(v => v).ToArray();

        if (sorted.Length == 1)
        {
            return sorted[0];
        }

        double position =
            (sorted.Length - 1) * p;

        int lower =
            (int)Math.Floor(position);

        int upper =
            (int)Math.Ceiling(position);

        if (lower == upper)
        {
            return sorted[lower];
        }

        double fraction =
            position - lower;

        return
            sorted[lower] +
            fraction *
            (sorted[upper] - sorted[lower]);
    }

    private static double MeanOrNaN(
        List<double> values)
    {
        return
            values != null &&
            values.Count > 0
            ? values.Average()
            : double.NaN;
    }

    private static double MaxOrNaN(
        List<double> values)
    {
        return
            values != null &&
            values.Count > 0
            ? values.Max()
            : double.NaN;
    }

    private static string Num(double value)
    {
        if (double.IsNaN(value) ||
            double.IsInfinity(value))
        {
            return "";
        }

        return value.ToString(
            "F9",
            CultureInfo.InvariantCulture
        );
    }

    private static string Format(double value)
    {
        if (double.IsNaN(value) ||
            double.IsInfinity(value))
        {
            return "NA";
        }

        return value.ToString(
            "F4",
            CultureInfo.InvariantCulture
        );
    }

    private static string Csv(string value)
    {
        if (value == null)
        {
            return "\"\"";
        }

        return "\"" +
               value.Replace("\"", "\"\"") +
               "\"";
    }

    private static string MakeSafeFileName(
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "UNKNOWN";
        }

        foreach (char c in
                 Path.GetInvalidFileNameChars())
        {
            value =
                value.Replace(c, '_');
        }

        return value.Replace(' ', '_');
    }
}
