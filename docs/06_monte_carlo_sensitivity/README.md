<div align="center">

# 06 · Monte Carlo Sensitivity

<p>Local robustness of TOPSIS ranks under randomized weight perturbations.</p>

<p>
  <img alt="Simulation" src="https://img.shields.io/badge/method-Monte%20Carlo-17365D">
  <img alt="Samples" src="https://img.shields.io/badge/samples-2000%20per%20level-2F5597">
  <img alt="Levels" src="https://img.shields.io/badge/perturbations-%C2%B110%25%20%7C%20%C2%B120%25-4472C4">
</p>

</div>

---

<h2>Files</h2>

<table>
  <thead><tr><th>Language</th><th>Document</th></tr></thead>
  <tbody>
    <tr><td>English</td><td><a href="./06_monte_carlo_en.pdf"><code>06_monte_carlo_en.pdf</code></a></td></tr>
    <tr><td>Español</td><td><a href="./06_monte_carlo_es.pdf"><code>06_monte_carlo_es.pdf</code></a></td></tr>
  </tbody>
</table>

<h2>Purpose</h2>

<p>
  Quantify whether the primary TOPSIS ranking remains stable when each criterion
  weight is independently perturbed and the resulting vector is renormalized.
</p>

<h2>Simulation configuration</h2>

<table>
  <thead><tr><th>Component</th><th>Configuration</th></tr></thead>
  <tbody>
    <tr><td>Decision matrix</td><td>Nine methods by seven criteria.</td></tr>
    <tr><td>Nominal scheme</td><td>W1.</td></tr>
    <tr><td>Perturbation levels</td><td>±10% and ±20%.</td></tr>
    <tr><td>Sampling</td><td>Independent uniform perturbations.</td></tr>
    <tr><td>Replicates</td><td>2000 per perturbation level.</td></tr>
    <tr><td>Recorded outputs</td><td>Rank-one frequency, Top-2 membership, mean rank, and coefficient dispersion.</td></tr>
  </tbody>
</table>

<h2>Algorithm</h2>

<ol>
  <li>Fix the normalized decision matrix and W1.</li>
  <li>Generate seven independent weight perturbations.</li>
  <li>Renormalize the perturbed vector so its components sum to one.</li>
  <li>Recalculate weighted values, ideal solutions, distances, and coefficients.</li>
  <li>Rank all nine methods and record the requested indicators.</li>
  <li>Repeat 2000 times for each perturbation level.</li>
</ol>

<h2>Result summary</h2>

<p>
  B4 retains rank one in 71–89% of the simulations around W1. The result describes
  local stability around that weighting scheme rather than an invariant ranking.
</p>

<h2>Navigation</h2>

<p>
  <a href="../05_topsis_multicriteria/">← Document 05</a>
  &nbsp;·&nbsp;
  <a href="../README.md">Index</a>
  &nbsp;·&nbsp;
  <a href="../07_state_of_the_art_and_references/">Document 07 →</a>
</p>
