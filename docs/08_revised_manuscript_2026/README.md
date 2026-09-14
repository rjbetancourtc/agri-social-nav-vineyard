<div align="center">

# 08 · Revised Manuscript Analysis (2026)

<p>Current protocol for the unified navigation campaign and the separate computational benchmark.</p>

<p>
  <img alt="Navigation design" src="https://img.shields.io/badge/navigation-9%20methods%20%C3%97%204%20scenarios%20%C3%97%2010%20runs-17365D">
  <img alt="Navigation runs" src="https://img.shields.io/badge/valid%20navigation%20runs-360-2563EB">
  <img alt="Benchmark runs" src="https://img.shields.io/badge/computational%20benchmark-54%20runs-7C3AED">
</p>

</div>

---

<h2>Data and run accounting</h2>

<table>
  <thead><tr><th>Campaign</th><th>Design</th><th>Data</th></tr></thead>
  <tbody>
    <tr>
      <td><strong>Navigation</strong></td>
      <td>9 methods × 4 scenarios × 10 repetitions = 360 runs</td>
      <td><a href="../../results/Data2/Results/">Telemetry CSV, MAT, and metrics CSV</a></td>
    </tr>
    <tr>
      <td><strong>Computational benchmark</strong></td>
      <td>9 methods × 2 scenarios × 3 repetitions = 54 runs</td>
      <td><a href="../../results/Data2/ControllerBenchmark/">Benchmark CSV and summary</a></td>
    </tr>
  </tbody>
</table>

<p>
  The revised manuscript reports 360 planned, executed, valid, and successful
  navigation runs, with zero exclusions and failures. The 54 computational
  runs are a separate experiment. The earlier 343-run analysis is historical.
</p>

<h2>Measurement definitions</h2>

<ul>
  <li>Mission: A → B → A, with the arrival region specified in the manuscript.</li>
  <li>Path length: accumulated displacement in Unity's horizontal x–z plane.</li>
  <li>Time-dependent metrics: calculated using recorded timestamps.</li>
  <li>Telemetry: nominal 0.10 s interval; approximately 4.387 Hz mean recorded frequency reported in the manuscript.</li>
  <li>Stop events: transitions into speed below 0.05 m/s in the archived MATLAB dashboard; an ordinary arrival stop may be counted.</li>
</ul>

<h2>Statistical analysis</h2>

<table>
  <thead><tr><th>Question</th><th>Revised approach</th></tr></thead>
  <tbody>
    <tr>
      <td>Method and scenario effects</td>
      <td>Repeated-measures Method × Scenario analysis, with Greenhouse–Geisser correction where applicable.</td>
    </tr>
    <tr>
      <td>Paired-design sensitivity</td>
      <td>Linear mixed-effects analysis with a paired RunID/seed intercept.</td>
    </tr>
    <tr>
      <td>Scenario-specific differences</td>
      <td>Paired contrasts, 95% confidence intervals, Cohen's d<sub>z</sub>, and Holm adjustment.</td>
    </tr>
    <tr>
      <td>Complete contrasts</td>
      <td>Supplementary Table S1 is referenced in the manuscript and must be supplied and checked separately.</td>
    </tr>
  </tbody>
</table>

<p>
  Success is constant across the final navigation campaign and cannot
  discriminate among methods. Where the repeated-measures and mixed-effects
  results differ, both results should remain visible.
</p>

<h2>TOPSIS and Pareto</h2>

<p>
  The revised primary decision model uses five criteria: saturated
  safety-distance utility, personal-zone time, mission time, stop-event
  count, and within-run speed variability. Equal weights provide a
  reference; three further author-defined profiles assess sensitivity.
</p>

<p>
  Success and maximum numerical acceleration are excluded from the primary
  TOPSIS matrix. Pareto dominance is assessed over all five criteria using
  the full decision vectors and a 9 × 9 dominance matrix. A two-dimensional
  plot is a visualization, not a full-space dominance test.
</p>

<h2>Controller and benchmark scope</h2>

<p>
  The evaluated M4 field has longitudinal–lateral anisotropy with
  front–rear symmetry and refers to the nearest detected human.
  This description does not assert guaranteed nonzero escape speed
  or multi-human influence aggregation.
</p>

<p>
  The 54-run benchmark measures computational behavior in the documented
  Unity Editor environment. Editor CPU utilization and controller timing
  do not establish worst-case real-time performance or validation on a
  deployed physical robot.
</p>

<h2>Release status</h2>

<ul>
  <li>The expected run-level navigation and benchmark files are present by filename; a full numerical audit is separate.</li>
  <li>Supplementary Table S1 was not identified in the current repository inventory.</li>
  <li>The main Unity controller and benchmark recorder described by the project are not currently present in the repository; archive them and the evaluated serialized configuration before claiming a complete executable release.</li>
  <li>Documents 01–06 are retained as records of earlier calculations, not as the current statistical protocol.</li>
</ul>

<p><a href="../README.md">← Documentation index</a></p>
