<!-- GitHub-rendered HTML within README.md -->
<div align="center">

# 📡 MATLAB · Automated Experiment Dashboard

<p>
  <strong>Unity–MATLAB acquisition, mission telemetry, run-level metrics,
  and campaign monitoring</strong>
</p>

<p>
  <img alt="Dashboard" src="https://img.shields.io/badge/dashboard-V7.5_AutoCampaign-17365D">
  <img alt="Methods" src="https://img.shields.io/badge/methods-9-2F855A">
  <img alt="Scenarios" src="https://img.shields.io/badge/scenarios-4-7C3AED">
  <img alt="Navigation dataset" src="https://img.shields.io/badge/published_navigation_runs-360-2563EB">
  <img alt="UDP" src="https://img.shields.io/badge/default_UDP_port-55000-0F766E">
</p>

<p>
  <a href="../../results/Data2/Results/"><strong>Navigation dataset</strong></a>
  ·
  <a href="../../results/Data2/ControllerBenchmark/"><strong>Separate computational benchmark</strong></a>
</p>

</div>

---

<blockquote>
  <p>
    <strong>Version and scope.</strong> The MATLAB source published in this
    directory is <code>RobotExperimentDashboard_V7_5_AutoCampaign.m</code>.
    The former README recommended a V7.4 file that is not present here.
    V7.5 acquires telemetry and calculates per-run metrics. The factorial
    inference, mixed-effects models, TOPSIS, and Pareto analyses reported
    in the revised manuscript are separate analyses; this dashboard alone
    does not execute them.
  </p>
</blockquote>

<h2>What V7.5 does</h2>

<table>
  <thead>
    <tr><th>Stage</th><th>Implementation in the published source</th></tr>
  </thead>
  <tbody>
    <tr>
      <td><strong>Receive</strong></td>
      <td>Reads comma-separated numeric UDP datagrams on port <code>55000</code> by default.</td>
    </tr>
    <tr>
      <td><strong>Identify</strong></td>
      <td>In automatic mode, decodes method and scenario from Unity packet fields 9 and 10. Assigns the next unused RunID using the local master log and telemetry filenames.</td>
    </tr>
    <tr>
      <td><strong>Track</strong></td>
      <td>Uses Unity mission statuses to delimit the A → B → A run and retains the samples within that mission window.</td>
    </tr>
    <tr>
      <td><strong>Measure</strong></td>
      <td>Calculates zone occupancy, mission time, horizontal path length, speed summaries, stop events, and numerical acceleration and jerk diagnostics.</td>
    </tr>
    <tr>
      <td><strong>Save</strong></td>
      <td>Writes a telemetry CSV, a per-run metrics CSV, a MAT file, and an updated local <code>master_log.csv</code>.</td>
    </tr>
    <tr>
      <td><strong>Continue</strong></td>
      <td>After automatic saving, clears the run buffers while keeping the UDP socket and MATLAB timer open for the next Unity run.</td>
    </tr>
  </tbody>
</table>

<h2>Run the published source</h2>

<ol>
  <li>Use a MATLAB installation supporting <code>udpport</code>, the figure-based interface, and the functions called by this source.</li>
  <li>Make this directory available on the MATLAB path.</li>
  <li>Configure Unity to send to the MATLAB computer on UDP port <code>55000</code>, or adjust the dashboard port before starting acquisition.</li>
  <li>Use a <strong>new, isolated output directory</strong> for any new campaign. The source default <code>cfg.baseFolder = 'results'</code> is relative to MATLAB's current working directory. It does not automatically select this repository's <code>results/Data2/Results/</code>.</li>
  <li>Start the dashboard with the command below and select <strong>START ACQUISITION</strong> once. In automatic mode, leave reception active while Unity advances through its runs.</li>
  <li>Check the saved method, scenario, RunID, terminal result, and packet-integrity counters before using newly recorded data for analysis.</li>
</ol>

<pre><code>RobotExperimentDashboard_V7_5_AutoCampaign</code></pre>

<p>
  Automatic campaign reception is enabled by default. Unity supplies the
  method and scenario; MATLAB assigns the RunID. The proximity-based
  success fallback is <strong>disabled by default</strong>. The source
  retains a legacy manual mode, but it is not the automatic protocol
  described here.
</p>

<h2>Experimental identifiers and mission protocol</h2>

<table>
  <thead>
    <tr><th>Factor</th><th>Expected values</th></tr>
  </thead>
  <tbody>
    <tr>
      <td><strong>Methods</strong></td>
      <td><code>0=M0, 1=M1, 2=M2, 3=M3, 4=M4, 5=B1, 6=B2, 7=B3, 8=B4</code></td>
    </tr>
    <tr>
      <td><strong>Scenarios</strong></td>
      <td><code>1=E1, 2=E2, 3=E3, 4=E4</code></td>
    </tr>
    <tr>
      <td><strong>Planned matrix</strong></td>
      <td>9 methods × 4 scenarios × 10 repetitions = 360 navigation runs</td>
    </tr>
    <tr>
      <td><strong>Unity statuses</strong></td>
      <td><code>0</code> toward B; <code>1</code> B reached/returning; <code>2</code> success; <code>3</code> timeout; <code>4</code> collision; <code>5</code> stuck; <code>6</code> aborted</td>
    </tr>
  </tbody>
</table>

<p>
  V7.5 marks <code>SUCCESS</code> only after the run has started,
  the B state has been observed, and Unity sends terminal status
  <code>2</code>. Failure statuses can also close and save a run.
  When a method–scenario combination already contains ten repetitions,
  RunID assignment issues a warning but can advance beyond ten;
  the ten-per-cell design must therefore be checked explicitly.
</p>

<h2>UDP packet contract</h2>

<p>
  The first ten comma-separated finite numeric fields are required
  in this exact order:
</p>

<pre><code>t, distance, velocity, acceleration, posX, posY, posZ, status, methodID, scenarioID</code></pre>

<p>
  Optional additional fields can carry robot heading and human, plant,
  bush, or object returns for the LiDAR-style visualization. The repository
  contains
  <a href="../../unity/scripts/Controller_Scripts/UnityToMatlabUDP_Lidar_CorridorRows_Row4_Central_CSV.cs"><code>UnityToMatlabUDP_Lidar_CorridorRows_Row4_Central_CSV.cs</code></a>,
  which documents the same ten-field campaign protocol. Check the
  transmitter configuration in the Unity scene before a new experiment:
  the MATLAB source header still mentions an older transmitter filename
  that is not present in this repository.
</p>

<p>
  <strong>Acquisition timers are not observed sampling frequency.</strong>
  The MATLAB timer period is <code>0.05 s</code>, the dashboard plotting
  period is <code>0.10 s</code>, and Unity's nominal transmission interval
  is <code>0.10 s</code>. The revised manuscript reports approximately
  <code>4.387 Hz</code> average <em>recorded</em> telemetry frequency,
  determined from timestamps.
</p>

<h2>Metrics calculated for each mission</h2>

<table>
  <thead>
    <tr><th>Output</th><th>Definition in V7.5</th></tr>
  </thead>
  <tbody>
    <tr>
      <td><code>TotalTime_s</code></td>
      <td>Last minus first timestamp within the mission window.</td>
    </tr>
    <tr>
      <td><code>PathLength_m</code></td>
      <td>Sum of successive horizontal x–z position increments.</td>
    </tr>
    <tr>
      <td><code>SocialTime_s</code>, <code>PersonalTime_s</code>, <code>IntimateTime_s</code></td>
      <td>Timestamp-weighted time below 3.60 m, 1.20 m, and 0.45 m, respectively. These nested zones are not mutually exclusive.</td>
    </tr>
    <tr>
      <td><code>StopTime_s</code></td>
      <td>Timestamp-weighted time with recorded speed below 0.05 m/s.</td>
    </tr>
    <tr>
      <td><code>NumberOfStops</code> (<code>Nstop</code>)</td>
      <td>Number of transitions into recorded speed below 0.05 m/s. Arrival-related stopping can be included; this is not automatically a comfort measure.</td>
    </tr>
    <tr>
      <td>Speed, distance, acceleration, and jerk</td>
      <td>Descriptive and numerical diagnostics. Acceleration and jerk are not criteria in the revised primary TOPSIS analysis.</td>
    </tr>
    <tr>
      <td><code>PathEfficiency</code></td>
      <td>Historical field name for <code>PathLength_m / TotalTime_s</code>, with units of m/s. It is not a dimensionless path-efficiency ratio.</td>
    </tr>
  </tbody>
</table>

<p>
  Zone and stopped times use differences between recorded timestamps.
  The implementation substitutes a median interval for nonpositive gaps
  and gaps above 2 s, and caps accumulated times at mission duration.
  Inspect those rules when interpreting irregular telemetry.
</p>

<h2>Local output files and published data</h2>

<p>
  With the source default <code>cfg.baseFolder = 'results'</code>,
  a newly saved run is written relative to MATLAB's current working
  directory as follows:
</p>

<pre><code>results/{METHOD}/{SCENARIO}/{METHOD}_{SCENARIO}_runNN.csv
results/{METHOD}/{SCENARIO}/{METHOD}_{SCENARIO}_runNN_metrics.csv
results/{METHOD}/{SCENARIO}/{METHOD}_{SCENARIO}_runNN.mat
results/master_log.csv</code></pre>

<p>
  The separately organized <strong>published</strong> navigation campaign
  is under
  <a href="../../results/Data2/Results/"><code>results/Data2/Results/</code></a>.
  By filename, that directory contains 360 telemetry CSV, 360 matching
  metrics CSV, and 360 MAT files. The
  <a href="../../results/Data2/ControllerBenchmark/">54-run controller benchmark</a>
  has separate files and was <strong>not</strong> produced by the
  functions in this dashboard. Its records must not be pooled with
  the 360 navigation runs.
</p>

<h2>Reproducibility boundaries</h2>

<ul>
  <li>This README documents the committed V7.5 source and available dataset layout; it does not claim that the script was executed again during this documentation update.</li>
  <li>The complete Unity controller, scene, and serialized baseline assets described by the manuscript must be archived before claiming that this repository alone can rerun the simulation.</li>
  <li>Final factorial statistics, scenario-specific contrasts, TOPSIS, and Pareto calculations require separate analysis scripts or documented outputs. Supplementary Table S1 is referenced in the revised manuscript but was not identified in the current repository inventory.</li>
  <li>The revised study is simulation-only. Dashboard timing and the separate Unity Editor benchmark are not a physical-robot real-time guarantee.</li>
</ul>

<p align="center">
  <a href="../../README.md">← Repository overview</a>
  ·
  <a href="../../results/Data2/Results/">Explore the 360-run dataset →</a>
</p>
