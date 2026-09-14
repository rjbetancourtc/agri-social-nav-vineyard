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
  <img alt="Navigation dataset" src="https://img.shields.io/badge/navigation_runs-360-2563EB">
  <img alt="UDP" src="https://img.shields.io/badge/default_UDP_port-55000-0F766E">
</p>

<p>
  <a href="../../results/Data2/Results/"><strong>Navigation dataset</strong></a>
  ·
  <a href="../../results/Data2/ControllerBenchmark/"><strong>Computational benchmark</strong></a>
</p>

</div>

<hr>

<h2>What V7.5 does</h2>

<table>
  <thead>
    <tr>
      <th>Stage</th>
      <th>Implementation</th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td><strong>Receive</strong></td>
      <td>Reads comma-separated numeric UDP datagrams on port <code>55000</code> by default.</td>
    </tr>
    <tr>
      <td><strong>Identify</strong></td>
      <td>Decodes method and scenario from Unity packet fields 9 and 10. Assigns the next RunID using the local master log and telemetry filenames.</td>
    </tr>
    <tr>
      <td><strong>Track</strong></td>
      <td>Uses Unity mission statuses to delimit the A → B → A run and retain the samples within its mission window.</td>
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

<h2>Run the dashboard</h2>

<ol>
  <li>Use a MATLAB installation that supports <code>udpport</code> and the functions called by the V7.5 script.</li>
  <li>Make this directory available on the MATLAB path.</li>
  <li>Configure Unity to transmit to the MATLAB computer on UDP port <code>55000</code>, or change the dashboard port before acquisition.</li>
  <li>Choose a new, isolated output directory for a new campaign. The script default <code>cfg.baseFolder = 'results'</code> is relative to MATLAB's current working directory; it does not automatically select <code>results/Data2/Results/</code> in this repository.</li>
  <li>Start the dashboard with the command below and select <strong>START ACQUISITION</strong>. Leave reception active while Unity advances through its runs.</li>
  <li>Check the saved method, scenario, RunID, terminal result, and packet-integrity counters before analyzing new recordings.</li>
</ol>

<pre><code>RobotExperimentDashboard_V7_5_AutoCampaign</code></pre>

<p>
  Automatic campaign reception is enabled by default. Unity supplies the
  method and scenario; MATLAB assigns the RunID. The proximity-based
  success fallback is disabled by default.
</p>

<h2>Experimental identifiers and mission protocol</h2>

<table>
  <thead>
    <tr>
      <th>Factor</th>
      <th>Expected values</th>
    </tr>
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
      <td><strong>Navigation matrix</strong></td>
      <td>9 methods × 4 scenarios × 10 repetitions = 360 navigation runs</td>
    </tr>
    <tr>
      <td><strong>Unity statuses</strong></td>
      <td><code>0</code> toward B; <code>1</code> B reached/returning; <code>2</code> success; <code>3</code> timeout; <code>4</code> collision; <code>5</code> stuck; <code>6</code> aborted</td>
    </tr>
  </tbody>
</table>

<p>
  V7.5 marks <code>SUCCESS</code> after the run has started, the B state
  has been observed, and Unity sends terminal status <code>2</code>.
  Failure statuses can also close and save a run. If a method–scenario
  combination already contains ten repetitions, RunID assignment warns
  but can advance beyond ten; verify the ten-per-cell design explicitly.
</p>

<h2>UDP packet contract</h2>

<p>
  The first ten fields must be finite numeric values separated by commas
  and transmitted in this order:
</p>

<pre><code>t, distance, velocity, acceleration, posX, posY, posZ, status, methodID, scenarioID</code></pre>

<p>
  Optional additional fields can carry robot heading and human or
  environmental returns for the LiDAR-style visualization. The
  <a href="../../unity/scripts/Controller_Scripts/UnityToMatlabUDP_Lidar_CorridorRows_Row4_Central_CSV.cs">Unity telemetry transmitter</a>
  documents the same ten-field campaign protocol. Verify the UDP
  destination and message fields in the Unity scene before an experiment.
</p>

<p>
  <strong>Acquisition timers are not the observed sampling frequency.</strong>
  The MATLAB timer period is <code>0.05 s</code>, the dashboard plotting
  period is <code>0.10 s</code>, and Unity's nominal transmission interval
  is <code>0.10 s</code>. The 360-run study reports approximately
  <code>4.387 Hz</code> average recorded telemetry frequency, calculated
  from timestamps.
</p>

<h2>Metrics calculated for each mission</h2>

<table>
  <thead>
    <tr>
      <th>Output</th>
      <th>Definition in V7.5</th>
    </tr>
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
      <td>Timestamp-weighted time below 3.60 m, 1.20 m, and 0.45 m, respectively. The zones are nested rather than mutually exclusive.</td>
    </tr>
    <tr>
      <td><code>StopTime_s</code></td>
      <td>Timestamp-weighted time with recorded speed below 0.05 m/s.</td>
    </tr>
    <tr>
      <td><code>NumberOfStops</code> (<code>Nstop</code>)</td>
      <td>Number of transitions into recorded speed below 0.05 m/s. Arrival-related stopping can be included.</td>
    </tr>
    <tr>
      <td>Acceleration and jerk</td>
      <td>Numerical diagnostics; they are not criteria in the primary five-criterion TOPSIS analysis.</td>
    </tr>
    <tr>
      <td><code>PathEfficiency</code></td>
      <td>Computed as <code>PathLength_m / TotalTime_s</code>, with units of m/s. Despite the column name, it is a rate rather than a dimensionless efficiency ratio.</td>
    </tr>
  </tbody>
</table>

<p>
  Zone and stopped times use recorded timestamp differences. The
  implementation substitutes a median interval for nonpositive gaps
  and gaps above 2 s, and caps accumulated times at mission duration.
</p>

<h2>Local output files and study datasets</h2>

<p>
  With the default <code>cfg.baseFolder = 'results'</code>, a newly
  saved run is written relative to MATLAB's current working directory:
</p>

<pre><code>results/{METHOD}/{SCENARIO}/{METHOD}_{SCENARIO}_runNN.csv
results/{METHOD}/{SCENARIO}/{METHOD}_{SCENARIO}_runNN_metrics.csv
results/{METHOD}/{SCENARIO}/{METHOD}_{SCENARIO}_runNN.mat
results/master_log.csv</code></pre>

<p>
  The organized 360-run navigation campaign is under
  <a href="../../results/Data2/Results/"><code>results/Data2/Results/</code></a>.
  This directory contains 360 telemetry CSV files, 360 matching metrics
  CSV files, and 360 MAT files. The
  <a href="../../results/Data2/ControllerBenchmark/">54-run controller benchmark</a>
  has separate files and was measured through a separate profiling
  procedure. Its 54 records are separate from the 360 navigation runs.
</p>

<h2>Scope of this script</h2>

<ul>
  <li>V7.5 receives Unity telemetry and computes run-level metrics. A compatible Unity scene and configuration are required for new recordings.</li>
  <li>The complete Unity controller, scene, and serialized baseline configurations are needed to rerun the simulation independently.</li>
  <li>Factorial statistics, scenario-specific contrasts, TOPSIS, and Pareto require their corresponding analysis scripts or documented outputs. Supplementary Table S1 should accompany the manuscript's supplementary material.</li>
  <li>The reported evaluation uses simulation. The separate computational benchmark characterizes its measured environment; it is not a physical-robot real-time guarantee.</li>
</ul>

<p align="center">
  <a href="../../README.md">← Repository overview</a>
  ·
  <a href="../../results/Data2/Results/">Explore the 360-run dataset →</a>
</p>
