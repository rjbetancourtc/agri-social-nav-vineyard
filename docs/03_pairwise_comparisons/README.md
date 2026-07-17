README.md


<div align="center">

# 03 · Pairwise Comparisons and Effect Sizes

<p>Post-hoc comparisons anchored on M4 and rank effects among B2, B3, and B4.</p>

<p>
  <img alt="Welch" src="https://img.shields.io/badge/comparison-Welch-17365D">
  <img alt="Hedges" src="https://img.shields.io/badge/effect-Hedges%20g-2F5597">
  <img alt="Cliff" src="https://img.shields.io/badge/effect-Cliff%20%CE%B4-4472C4">
</p>

</div>

---

<h2>Files</h2>

<table>
  <thead><tr><th>Language</th><th>Document</th></tr></thead>
  <tbody>
    <tr><td>English</td><td><a href="./03_pairwise_comparisons_en.pdf"><code>03_pairwise_comparisons_en.pdf</code></a></td></tr>
    <tr><td>Español</td><td><a href="./03_comparaciones_pareadas_es.pdf"><code>03_comparaciones_pareadas_es.pdf</code></a></td></tr>
  </tbody>
</table>

<h2>Purpose</h2>

<p>
  Identify pairwise differences after the omnibus analysis, quantify their
  magnitude, and control multiplicity separately for each metric.
</p>

<h2>Methods</h2>

<table>
  <thead><tr><th>Method</th><th>Application</th></tr></thead>
  <tbody>
    <tr><td>Welch comparison</td><td>M4 against each of the other eight methods.</td></tr>
    <tr><td>Hedges’ g</td><td>Bias-corrected standardized mean difference.</td></tr>
    <tr><td>Benjamini–Hochberg</td><td>Multiplicity control over eight comparisons per metric.</td></tr>
    <tr><td>Mann–Whitney U</td><td>Rank-based comparisons among B2, B3, and B4.</td></tr>
    <tr><td>Cliff’s delta</td><td>Direction and magnitude of stochastic separation.</td></tr>
  </tbody>
</table>

<h2>Sign convention</h2>

<div align="center">
  <code>g &gt; 0 → comparator is numerically greater than M4</code>
</div>

<p>
  Operational preference still depends on the metric direction: separation and
  success are benefits, while time, path length, speed variability, and stops are costs.
</p>

<details>
  <summary><strong>Interpretation boundary</strong></summary>
  <p>
    Statistical significance and effect magnitude answer different questions.
    Neither should be interpreted without the metric direction and operational context.
  </p>
</details>

<h2>Navigation</h2>

<p>
  <a href="../02_omnibus_tests/">← Document 02</a>
  &nbsp;·&nbsp;
  <a href="../README.md">Index</a>
  &nbsp;·&nbsp;
  <a href="../04_success_rate_tests/">Document 04 →</a>
</p>
