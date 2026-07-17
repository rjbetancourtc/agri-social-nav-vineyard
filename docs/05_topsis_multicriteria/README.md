<div align="center">

# 05 · TOPSIS Multicriteria Analysis

<p>Seven-criterion evaluation of nine navigation methods under three weighting schemes.</p>

<p>
  <img alt="Matrix" src="https://img.shields.io/badge/matrix-9%C3%977-17365D">
  <img alt="Weights" src="https://img.shields.io/badge/weighting%20schemes-3-2F5597">
  <img alt="Methods" src="https://img.shields.io/badge/methods-9-4472C4">
</p>

</div>

---

<h2>Files</h2>

<table>
  <thead><tr><th>Language</th><th>Document</th></tr></thead>
  <tbody>
    <tr><td>English</td><td><a href="./05_topsis_en.pdf"><code>05_topsis_en.pdf</code></a></td></tr>
    <tr><td>Español</td><td><a href="./05_topsis_es.pdf"><code>05_topsis_es.pdf</code></a></td></tr>
  </tbody>
</table>

<h2>Decision model</h2>

<p>The decision matrix contains nine rows and seven criteria:</p>

<div align="center">
  <code>d* · Tpersonal · Tmission · Nstop · σv · amax · ηs</code>
</div>

<table>
  <thead><tr><th>Direction</th><th>Criteria</th></tr></thead>
  <tbody>
    <tr><td>Benefit</td><td>Minimum separation <code>d*</code> and success rate <code>ηs</code>.</td></tr>
    <tr><td>Cost</td><td>Personal-zone time, mission time, stops, speed variability, and acceleration indicator.</td></tr>
  </tbody>
</table>

<h2>Calculation sequence</h2>

<ol>
  <li>Build the decision matrix from the metric summaries.</li>
  <li>Normalize every criterion by its Euclidean norm.</li>
  <li>Apply the selected weight vector.</li>
  <li>Construct positive and negative ideal solutions.</li>
  <li>Calculate distances to both ideals.</li>
  <li>Calculate the closeness coefficient.</li>
  <li>Rank methods from the highest coefficient to the lowest.</li>
</ol>

<h2>Weighting schemes</h2>

<table>
  <thead><tr><th>Scheme</th><th>Emphasis</th></tr></thead>
  <tbody>
    <tr><td>W1</td><td>Primary balanced evaluation.</td></tr>
    <tr><td>W2</td><td>Social-navigation emphasis.</td></tr>
    <tr><td>W3</td><td>Kinematic emphasis.</td></tr>
  </tbody>
</table>

<details>
  <summary><strong>Interpretation boundary</strong></summary>
  <p>
    TOPSIS leadership depends on the represented priorities. A rank is not an
    intrinsic statement of universal superiority.
  </p>
</details>

<h2>Navigation</h2>

<p>
  <a href="../04_success_rate_tests/">← Document 04</a>
  &nbsp;·&nbsp;
  <a href="../README.md">Index</a>
  &nbsp;·&nbsp;
  <a href="../06_monte_carlo_sensitivity/">Document 06 →</a>
</p>

