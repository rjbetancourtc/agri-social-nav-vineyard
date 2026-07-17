<div align="center">

# Technical Calculation Support

<p>
  Bilingual documentation for the statistical, inferential, multicriteria,
  and sensitivity analyses of the agricultural social-navigation experiments.
</p>

<p>
  <img alt="Documents" src="https://img.shields.io/badge/documents-01--07-17365D">
  <img alt="Languages" src="https://img.shields.io/badge/languages-English%20%7C%20Espa%C3%B1ol-2F5597">
  <img alt="Format" src="https://img.shields.io/badge/format-PDF-B31B1B">
</p>

</div>

---

<h2>Directory map</h2>

<table>
  <thead>
    <tr><th>No.</th><th>Directory</th><th>Scope</th></tr>
  </thead>
  <tbody>
    <tr><td>01</td><td><a href="./01_descriptive_statistics/">Descriptive statistics</a></td><td>Experimental matrix, summary statistics, and scenario-resolved safety.</td></tr>
    <tr><td>02</td><td><a href="./02_omnibus_tests/">Omnibus tests</a></td><td>Nine-group Welch ANOVA and restricted Kruskal–Wallis tests.</td></tr>
    <tr><td>03</td><td><a href="./03_pairwise_comparisons/">Pairwise comparisons</a></td><td>Welch comparisons, Hedges’ g, rank effects, and multiplicity control.</td></tr>
    <tr><td>04</td><td><a href="./04_success_rate_tests/">Success-rate tests</a></td><td>Fisher exact tests, global chi-square, and Cramér’s V.</td></tr>
    <tr><td>05</td><td><a href="./05_topsis_multicriteria/">TOPSIS multicriteria</a></td><td>Decision matrix, weighting schemes, closeness coefficients, and ranks.</td></tr>
    <tr><td>06</td><td><a href="./06_monte_carlo_sensitivity/">Monte Carlo sensitivity</a></td><td>Weight perturbation and ranking-stability analysis.</td></tr>
    <tr><td>07</td><td><a href="./07_state_of_the_art_and_references/">State of the art and references</a></td><td>Technical foundations and bibliography.</td></tr>
    <tr><td>—</td><td><a href="./supporting_data/">Supporting data</a></td><td>Calculation workbook and supporting tabular resources.</td></tr>
  </tbody>
</table>

<h2>Analysis sequence</h2>

<div align="center">
  <code>Descriptive statistics → Omnibus tests → Pairwise effects → Success tests → TOPSIS → Sensitivity</code>
</div>

<h2>Shared experimental scope</h2>

<ul>
  <li><strong>343</strong> valid runs.</li>
  <li><strong>9</strong> navigation methods: M0–M4 and B1–B4.</li>
  <li><strong>4</strong> experimental scenarios: E1–E4.</li>
  <li>Continuous metrics summarized conditionally on mission success.</li>
  <li>Success treated separately as a binary outcome.</li>
</ul>

<h2>Naming convention</h2>

<pre><code>NN_descriptive_name_language.pdf
</code></pre>

<table>
  <thead><tr><th>Component</th><th>Rule</th><th>Example</th></tr></thead>
  <tbody>
    <tr><td>Sequence</td><td>Two digits</td><td><code>01</code></td></tr>
    <tr><td>Words</td><td>Lowercase snake_case</td><td><code>descriptive_statistics</code></td></tr>
    <tr><td>Language</td><td><code>en</code> or <code>es</code></td><td><code>_en.pdf</code></td></tr>
    <tr><td>Directory guide</td><td>Uppercase standard name</td><td><code>README.md</code></td></tr>
  </tbody>
</table>

<h2>Navigation</h2>

<p align="center">
  <a href="./01_descriptive_statistics/"><strong>Start with Document 01 →</strong></a>
</p>
