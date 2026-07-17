README.md


<div align="center">

# 01 · Descriptive Statistics

<p>Experimental design, global summaries, and scenario-resolved safety metrics.</p>

<p>
  <img alt="Analysis" src="https://img.shields.io/badge/analysis-descriptive-17365D">
  <img alt="Runs" src="https://img.shields.io/badge/valid%20runs-343-2F5597">
  <img alt="Methods" src="https://img.shields.io/badge/methods-9-4472C4">
</p>

</div>

---

<h2>Files</h2>

<table>
  <thead><tr><th>Language</th><th>Document</th></tr></thead>
  <tbody>
    <tr><td>English</td><td><a href="./01_descriptive_statistics_en.pdf"><code>01_descriptive_statistics_en.pdf</code></a></td></tr>
    <tr><td>Español</td><td><a href="./01_estadistica_descriptiva_es.pdf"><code>01_estadistica_descriptiva_es.pdf</code></a></td></tr>
  </tbody>
</table>

<h2>Purpose</h2>

<p>
  Provide a reproducible description of the Unity–MATLAB experimental corpus
  before inferential testing. The document separates continuous metrics from
  the binary mission-success outcome and preserves method-specific sample sizes.
</p>

<h2>Experimental scope</h2>

<ul>
  <li>343 valid runs across nine navigation methods.</li>
  <li>Four scenarios, E1 through E4.</li>
  <li>Continuous metrics summarized over successful runs.</li>
  <li>Success rate computed from all valid runs.</li>
  <li>One excluded B1 record retained in the denominator audit.</li>
</ul>

<h2>Included calculations</h2>

<table>
  <thead><tr><th>Category</th><th>Metrics and outputs</th></tr></thead>
  <tbody>
    <tr><td>Safety</td><td>Minimum and mean distance, zone-occupancy time, scenario-resolved minimum distance.</td></tr>
    <tr><td>Mission</td><td>Mission duration, path length, path efficiency, stopped time, and stop count.</td></tr>
    <tr><td>Kinematics</td><td>Mean speed, maximum speed, speed variability, and acceleration indicators.</td></tr>
    <tr><td>Reliability</td><td>Successful and planned runs by method and scenario.</td></tr>
    <tr><td>Summary</td><td>Mean, sample standard deviation, and worked descriptive calculation.</td></tr>
  </tbody>
</table>

<details>
  <summary><strong>Interpretation boundary</strong></summary>
  <p>
    Descriptive differences do not establish statistical significance. Omnibus
    testing begins in Document 02, while pairwise effect sizes are handled in Document 03.
  </p>
</details>

<h2>Navigation</h2>

<p>
  <a href="../README.md">← Documentation index</a>
  &nbsp;·&nbsp;
  <a href="../02_omnibus_tests/">Document 02 →</a>
</p>
