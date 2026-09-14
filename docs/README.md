<div align="center">

# Technical Documentation

<p>Scientific background, archived calculations, and the revised evaluation of agricultural social navigation.</p>

<p>
  <img alt="Navigation runs" src="https://img.shields.io/badge/navigation%20runs-360-2563EB">
  <img alt="Benchmark runs" src="https://img.shields.io/badge/benchmark%20runs-54-7C3AED">
  <img alt="Methods" src="https://img.shields.io/badge/methods-9-2F855A">
  <img alt="Languages" src="https://img.shields.io/badge/languages-English%20%7C%20Espa%C3%B1ol-2F5597">
</p>

</div>

---

<h2>Choose the correct analysis version</h2>

<table>
  <thead><tr><th>Version</th><th>Material</th><th>Use</th></tr></thead>
  <tbody>
    <tr>
      <td><strong>Revised manuscript</strong></td>
      <td><a href="./08_revised_manuscript_2026/">Current analysis guide</a>;
          <a href="../results/Data2/Results/">360-run data</a>;
          <a href="../results/Data2/ControllerBenchmark/">54-run benchmark</a></td>
      <td>Use for the revised paper and its reported analyses.</td>
    </tr>
    <tr>
      <td><strong>Earlier analysis</strong></td>
      <td>Documents 01–06 and their linked PDFs</td>
      <td>Historical calculations. Do not cite their 343-run or seven-criterion results as results of the revised paper.</td>
    </tr>
    <tr>
      <td><strong>Technical background</strong></td>
      <td><a href="./07_State_of_the_Art_and_References/">Document 07</a></td>
      <td>Literature and conceptual foundations, not final campaign results.</td>
    </tr>
  </tbody>
</table>

<h2>Directory map</h2>

<table>
  <thead><tr><th>No.</th><th>Directory</th><th>Scope</th></tr></thead>
  <tbody>
    <tr><td>01</td><td><a href="./01_descriptive_statistics/">Descriptive statistics</a></td><td>Archived descriptive calculations.</td></tr>
    <tr><td>02</td><td><a href="./02_omnibus_tests/">Omnibus tests</a></td><td>Archived Welch and Kruskal–Wallis calculations.</td></tr>
    <tr><td>03</td><td><a href="./03_pairwise_comparisons/">Pairwise comparisons</a></td><td>Archived Welch, Hedges, and rank-effect calculations.</td></tr>
    <tr><td>04</td><td><a href="./04_success_rate_tests/">Success-rate tests</a></td><td>Archived categorical comparisons; final-campaign success is constant.</td></tr>
    <tr><td>05</td><td><a href="./05_topsis_multicriteria/">TOPSIS multicriteria</a></td><td>Archived seven-criterion model.</td></tr>
    <tr><td>06</td><td><a href="./06_monte_carlo_sensitivity/">Monte Carlo sensitivity</a></td><td>Archived sensitivity analysis of the earlier weighting model.</td></tr>
    <tr><td>07</td><td><a href="./07_State_of_the_Art_and_References/">State of the art and references</a></td><td>Background literature in English and Spanish.</td></tr>
    <tr><td>08</td><td><a href="./08_revised_manuscript_2026/">Revised manuscript analysis</a></td><td>Current 360-run protocol and separate 54-run benchmark.</td></tr>
  </tbody>
</table>

<h2>Current analysis sequence</h2>

<div align="center">
  <code>Run accounting → Timestamp-based metrics → Method × Scenario → Paired contrasts → Five-criterion TOPSIS → Full-space Pareto</code>
</div>

<h2>Availability and verification scope</h2>

<ul>
  <li>The expected 360 telemetry CSV, 360 MAT, 360 metrics CSV, and 54 benchmark CSV files are present by filename. This inventory does not independently validate every numerical value.</li>
  <li>The earlier root-level <code>results/master_log*.csv</code> files must not be pooled with <code>results/Data2/Results/</code>.</li>
  <li>Supplementary Table S1 must be supplied and validated separately.</li>
  <li>The currently available repository does not contain every source file and serialized asset needed to reproduce the Unity experiment from an empty installation.</li>
</ul>
