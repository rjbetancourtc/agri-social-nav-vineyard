README.md


<div align="center">

# 04 · Success-Rate Tests

<p>Exact and global analysis of mission success and failure counts.</p>

<p>
  <img alt="Fisher" src="https://img.shields.io/badge/test-Fisher%20exact-17365D">
  <img alt="Chi-square" src="https://img.shields.io/badge/test-%CF%87%C2%B2-2F5597">
  <img alt="Cramer" src="https://img.shields.io/badge/effect-Cram%C3%A9r%20V-4472C4">
</p>

</div>

---

<h2>Files</h2>

<table>
  <thead><tr><th>Language</th><th>Document</th></tr></thead>
  <tbody>
    <tr><td>English</td><td><a href="./04_success_rates_en.pdf"><code>04_success_rates_en.pdf</code></a></td></tr>
    <tr><td>Español</td><td><a href="./04_tasas_exito_es.pdf"><code>04_tasas_exito_es.pdf</code></a></td></tr>
  </tbody>
</table>

<h2>Purpose</h2>

<p>
  Evaluate mission reliability as a categorical outcome without mixing failed
  runs with continuous trajectory summaries.
</p>

<h2>Outcome definition</h2>

<div align="center">
  <code>Success = completion of A → B → A within 180 seconds</code>
</div>

<h2>Methods and outputs</h2>

<table>
  <thead><tr><th>Procedure</th><th>Input</th><th>Output</th></tr></thead>
  <tbody>
    <tr><td>Success rate</td><td>Successful and valid runs</td><td>Method- and scenario-specific percentage.</td></tr>
    <tr><td>Fisher exact test</td><td>Scenario-specific 2 × 2 table</td><td>Exact pairwise probability value.</td></tr>
    <tr><td>Pearson chi-square</td><td>Global 9 × 2 table</td><td>Association statistic and probability value.</td></tr>
    <tr><td>Cramér’s V</td><td>Chi-square statistic and total count</td><td>Association strength.</td></tr>
  </tbody>
</table>

<h2>Key controls</h2>

<ul>
  <li>Preserve method-specific denominators.</li>
  <li>Calculate rates from integer counts before rounding.</li>
  <li>Use exact inference for sparse 2 × 2 tables.</li>
  <li>Keep scenario-specific and global conclusions separate.</li>
</ul>

<h2>Navigation</h2>

<p>
  <a href="../03_pairwise_comparisons/">← Document 03</a>
  &nbsp;·&nbsp;
  <a href="../README.md">Index</a>
  &nbsp;·&nbsp;
  <a href="../05_topsis_multicriteria/">Document 05 →</a>
</p>

