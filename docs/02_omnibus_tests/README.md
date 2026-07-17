<div align="center">

# 02 · Omnibus Tests

<p>Global comparison of navigation-method scalar metrics.</p>

<p>
  <img alt="Welch" src="https://img.shields.io/badge/test-Welch%20ANOVA-17365D">
  <img alt="Kruskal" src="https://img.shields.io/badge/test-Kruskal%E2%80%93Wallis-2F5597">
  <img alt="Groups" src="https://img.shields.io/badge/groups-9-4472C4">
</p>

</div>

---

<h2>Files</h2>

<table>
  <thead><tr><th>Language</th><th>Document</th></tr></thead>
  <tbody>
    <tr><td>English</td><td><a href="./02_omnibus_tests_en.pdf"><code>02_omnibus_tests_en.pdf</code></a></td></tr>
    <tr><td>Español</td><td><a href="./02_pruebas_omnibus_es.pdf"><code>02_pruebas_omnibus_es.pdf</code></a></td></tr>
  </tbody>
</table>

<h2>Purpose</h2>

<p>
  Test whether at least one navigation method differs on each scalar metric,
  while respecting unequal variances and the availability of run-level observations.
</p>

<h2>Statistical design</h2>

<table>
  <thead><tr><th>Procedure</th><th>Scope</th><th>Role</th></tr></thead>
  <tbody>
    <tr><td>Welch ANOVA</td><td>M0–M4 and B1–B4</td><td>Primary nine-group mean comparison using summary statistics.</td></tr>
    <tr><td>Kruskal–Wallis</td><td>B2, B3, and B4</td><td>Rank-based comparison where run-level scalars are available.</td></tr>
    <tr><td>Tie correction</td><td>Ranked observations</td><td>Corrects the Kruskal–Wallis statistic when values are tied.</td></tr>
  </tbody>
</table>

<h2>Reported outputs</h2>

<ul>
  <li>Welch statistic, numerator degrees of freedom, and probability value.</li>
  <li>Kruskal–Wallis statistic, degrees of freedom, and probability value.</li>
  <li>Separate reporting for each evaluated navigation metric.</li>
  <li>Explicit limitation: an omnibus result does not identify the differing pairs.</li>
</ul>

<details>
  <summary><strong>Reproducibility controls</strong></summary>
  <ul>
    <li>Preserve units, group sizes, and success conditioning.</li>
    <li>Use double-precision arithmetic.</li>
    <li>Avoid premature rounding of test statistics and probability values.</li>
    <li>Do not reconstruct unavailable run-level observations.</li>
  </ul>
</details>

<h2>Navigation</h2>

<p>
  <a href="../01_descriptive_statistics/">← Document 01</a>
  &nbsp;·&nbsp;
  <a href="../README.md">Index</a>
  &nbsp;·&nbsp;
  <a href="../03_pairwise_comparisons/">Document 03 →</a>
</p>

