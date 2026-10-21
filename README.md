<!-- Agri-Social-Nav-Vineyard: identified scientific release, 20 October 2026 -->
<div align="center">
<p><strong>RESEARCH PROJECT</strong><br>Agricultural Robotics · Human–Robot Interaction · Social Navigation</p>
<h1>🌿 Agri-Social Navigation in Vineyard Corridors</h1>
<h3>Anisotropic Proxemic Fields for Socially Aware Agricultural Robot Navigation</h3>
<p>A <strong>Unity–Python</strong> simulation and numerical-evaluation framework for agricultural mobile robots sharing vineyard corridors with workers. Current campaign: EVAL_20261019_112257; minimum scientific repository.</p>
<p><strong>Authors / Autores:</strong><br>Reinaldo Betancourt · Ingrid Nicole Vásconez · Viviana Moya · William Chamorro · Sandra Cano · Marco Antonio Molina · Juan Pablo Vásconez</p>
<p>
<img alt="Repository" src="https://img.shields.io/badge/GitHub-agri--social--nav--vineyard-181717?logo=github">
<img alt="Unity" src="https://img.shields.io/badge/Unity-Simulation-000000?logo=unity">
<img alt="Python" src="https://img.shields.io/badge/Python-Numerical_Analysis-3776AB?logo=python">
<img alt="Methods" src="https://img.shields.io/badge/Navigation_Methods-9-2F855A">
<img alt="Attempts" src="https://img.shields.io/badge/Mission_Attempts-360-2563EB">
<img alt="Success" src="https://img.shields.io/badge/SUCCESS-266-2F855A">
<img alt="Timeout" src="https://img.shields.io/badge/TIMEOUT-94-BC5C22">
<img alt="Benchmark" src="https://img.shields.io/badge/Benchmark-180_Attempts-8B5CF6">
<img alt="Reports" src="https://img.shields.io/badge/PDF_Reports-English_%7C_Espa%C3%B1ol-17365D">
</p>
<p><a href="#technical-reports">PDF reports / Informes PDF</a> · <a href="#authors">Authors</a></p>
</div>
<hr>

<h2 id="repository-purpose">Repository Purpose / Propósito</h2>
<p>This repository supports the manuscript <em>Anisotropic Proxemic Fields vs. Social Navigation Baselines for Agricultural Robot Navigation in Vineyard Corridors: A Statistical Comparison with Effect Sizes</em>. It studies mission completion, observed human separation, efficiency and controller computation in simulation. Physical-robot safety and agricultural deployment require separate validation.</p>
<p>Los informes de lectura se presentan en <strong>PDF, en español e inglés</strong>, con los siete autores. Los datos y scripts permanecen disponibles para reproducción numérica. Esta entrega distingue la evaluación corregida de las campañas históricas; los resultados no se mezclan.</p>

<h2 id="technical-reports">Technical Reports / Informes técnicos</h2>
<p>Each report includes all study authors on its cover and in PDF metadata. Numerical results refer to <code>EVAL_20261019_112257</code>, with literature distinguished explicitly. CSV/JSON files are machine-readable inputs, not the reading reports.</p>
<table><thead><tr><th>No.</th><th>Topic / Tema</th><th>English</th><th>Español</th></tr></thead><tbody>
<tr><td>01</td><td>Descriptive Statistics of the Evaluated Campaign<br><small>Estadística descriptiva de la campaña evaluada</small></td><td><a href="./docs/01_descriptive_statistics/01_descriptive_statistics_en.pdf">English PDF</a></td><td><a href="./docs/01_descriptive_statistics/01_estadistica_descriptiva_es.pdf">PDF español</a></td></tr>
<tr><td>02</td><td>Method, Scenario and Interaction Analysis<br><small>Análisis de método, escenario e interacción</small></td><td><a href="./docs/02_omnibus_tests/02_omnibus_tests_en.pdf">English PDF</a></td><td><a href="./docs/02_omnibus_tests/02_pruebas_omnibus_es.pdf">PDF español</a></td></tr>
<tr><td>03</td><td>Scenario-Specific Comparisons and Effect Sizes<br><small>Comparaciones por escenario y tamaños de efecto</small></td><td><a href="./docs/03_pairwise_comparisons/03_pairwise_comparisons_en.pdf">English PDF</a></td><td><a href="./docs/03_pairwise_comparisons/03_comparaciones_entre_metodos_es.pdf">PDF español</a></td></tr>
<tr><td>04</td><td>Mission Outcomes and Human-Proximity Events<br><small>Resultados de misión y eventos de proximidad humana</small></td><td><a href="./docs/04_success_rate_tests/04_success_rates_en.pdf">English PDF</a></td><td><a href="./docs/04_success_rate_tests/04_tasas_exito_es.pdf">PDF español</a></td></tr>
<tr><td>05</td><td>TOPSIS and Full-Space Pareto Analysis<br><small>TOPSIS y análisis de Pareto en el espacio completo</small></td><td><a href="./docs/05_topsis_multicriteria/05_topsis_en.pdf">English PDF</a></td><td><a href="./docs/05_topsis_multicriteria/05_topsis_es.pdf">PDF español</a></td></tr>
<tr><td>06</td><td>Weight Sensitivity and Sampling Uncertainty<br><small>Sensibilidad de pesos e incertidumbre muestral</small></td><td><a href="./docs/06_monte_carlo_sensitivity/06_monte_carlo_en.pdf">English PDF</a></td><td><a href="./docs/06_monte_carlo_sensitivity/06_monte_carlo_es.pdf">PDF español</a></td></tr>
<tr><td>07</td><td>State of the Art and Technical References<br><small>Estado del arte y referencias técnicas</small></td><td><a href="./docs/07_State_of_the_Art_and_References/07_state_of_the_art_and_technical_references_en.pdf">English PDF</a></td><td><a href="./docs/07_State_of_the_Art_and_References/07_estado_del_arte_y_referencias_es.pdf">PDF español</a></td></tr>
<tr><td>08</td><td>Controller Specification, Measurements and Benchmark<br><small>Controladores, mediciones y rendimiento computacional</small></td><td><a href="./docs/08_revised_manuscript_2026/08_measurements_and_benchmark_en.pdf">English PDF</a></td><td><a href="./docs/08_revised_manuscript_2026/08_mediciones_y_benchmark_es.pdf">PDF español</a></td></tr>
<tr><td>09</td><td>Data Provenance and Numerical Reproducibility<br><small>Procedencia de datos y reproducibilidad numérica</small></td><td><a href="./docs/09_reproducibility/09_reproducibility_en.pdf">English PDF</a></td><td><a href="./docs/09_reproducibility/09_reproducibilidad_es.pdf">PDF español</a></td></tr>
</tbody></table>

<h2 id="experimental-identity">Experimental Identity</h2>
<table><tr><th align="left">Item</th><th align="left">Evaluated specification</th></tr>
<tr><td>Campaign</td><td><code>EVAL_20261019_112257</code></td></tr>
<tr><td>Design</td><td>9 methods × 4 scenarios × 10 attempts = 360 planned and executed attempts</td></tr>
<tr><td>Outcomes retained</td><td><strong>266 SUCCESS + 94 TIMEOUT = 360</strong>; TIMEOUT is an observed mission failure, not a pending run.</td></tr>
<tr><td>Mission</td><td>A → B → A; waypoint arrival radius 2.0 m; wall-clock timeout 180 s</td></tr>
<tr><td>Control period</td><td>0.10 s (10 Hz); rendering and pose sampling are separate</td></tr>
<tr><td>Separate computational benchmark</td><td>9 methods × E1/E4 × 10 repetitions = <strong>180 assignments</strong></td></tr>
<tr><td>M4 field</td><td>Nearest eligible detected human; longitudinal–lateral anisotropy with front–rear symmetry; zone-based target speeds</td></tr>
<tr><td>Inference</td><td>Independent working assumption; scenario-specific contrasts. Matched trajectories / pairing are not verified.</td></tr>
</table>

<h2 id="navigation-methods">Navigation Methods</h2>
<table><tr><th>ID</th><th>Evaluated strategy</th></tr>
<tr><td>M0</td><td>NavMesh only</td></tr><tr><td>M1</td><td>Distance-threshold stop supervisor</td></tr><tr><td>M2</td><td>Hysteresis stop supervisor</td></tr><tr><td>M3</td><td>Nearest-human isotropic proxemics with zone-based speed</td></tr><tr><td>M4</td><td>Nearest-human longitudinal–lateral anisotropic proxemics with zone-based speed</td></tr><tr><td>B1</td><td>Social Dynamic Window Approach, including an anisotropic social-cost term</td></tr><tr><td>B2</td><td>ORCA/RVO-inspired relative-velocity correction; not the reference half-plane linear program</td></tr><tr><td>B3</td><td>Social Force implementation</td></tr><tr><td>B4</td><td>Barrier-inspired sampled Social DWA; no formal continuous-time CBF safety guarantee</td></tr>
</table>

<h2 id="hri-scenarios">Human–Robot Interaction Scenarios</h2>
<table><tr><th>Scenario</th><th>Interaction</th><th>Humans</th></tr><tr><td>E1</td><td>Frontal encounter</td><td>2</td></tr><tr><td>E2</td><td>Lateral intrusion</td><td>2</td></tr><tr><td>E3</td><td>Following / longitudinal interaction</td><td>3</td></tr><tr><td>E4</td><td>Multi-human congestion</td><td>9, including four without patrol</td></tr></table>
<p>Operational human-centre thresholds: intimate 0.45 m, personal 1.20 m, social 3.60 m. These thresholds define observed simulation exposure; they are not certified surface clearances or validated worker-comfort limits.</p>

<h2 id="evaluation-pipeline">Evaluation Pipeline</h2>
<p><code>Unity simulation → UDP telemetry → Python receiver → adjudicated run endpoints → scenario-specific inference → three-criterion TOPSIS → full-space dominance</code></p>
<table><tr><th>Component</th><th>Current scope</th></tr>
<tr><td>Method × Scenario</td><td>Sum-coded independent HC3 model for exposure, with rank and residual diagnostics. Full completed-duration factorial unavailable because only 28 of 36 cells contain completers; singular binary-model omnibus terms are not confirmatory.</td></tr>
<tr><td>128 contrasts</td><td>M4 versus eight comparators, four scenarios, four endpoints; Welch estimates / Hedges g for continuous endpoints; Fisher tests / risk differences for binary endpoints; two Holm families of 64.</td></tr>
<tr><td>TOPSIS</td><td>Three run-level criteria: failure-aware mission utility, saturated clearance utility and personal-exposure fraction. Acceleration, jerk and stop counts are excluded.</td></tr>
<tr><td>Pareto</td><td>Complete three-criterion mean vectors and 9 × 9 dominance matrices. A two-dimensional plot alone does not establish full-space non-dominance.</td></tr>
<tr><td>Preference uncertainty</td><td>Four weight profiles; recorded 500 weight draws and 1000 within-cell bootstrap samples per scenario / pooled set; criterion-removal and scenario-preference sensitivity.</td></tr>
</table>
<p>The pooled equal-weight TOPSIS order begins <strong>B3, B4, M4</strong>. The pooled mean-vector Pareto set is <strong>M0, B3, B4</strong>. Rankings express stated preferences under the evaluated conditions; M4 is not claimed to be universally optimal or faster.</p>

<p>The analysis regenerates descriptive statistics, exact event intervals, 128 contrasts, Holm correction, factorial diagnostics, four TOPSIS profiles, dominance, criterion/scenario sensitivity, 500 weight vectors and 1000 within-cell bootstrap samples per ranking set. It compares the regenerated results with the numerical inputs used by the reports. </code>.</p>
<p><strong>Exact scope:</strong> tables and statistical/decision analysis from supplied adjudicated per-run metrics. Raw trajectories and telemetry are excluded, so this package cannot independently recalculate all measured navigation metrics or cycle-time percentiles. </a>.</p>
<p><a href="Vasconez.pdf">Vasconez.pdf</a> and <a href="referencias/">six original reference PDFs</a> were recovered unchanged from the preceding repository revision. Their original authorship is preserved. See the <a href="references/README.md">reference index</a> and <a href="references/RECOVERED_REFERENCES.json">recovery hashes</a>. Other cited publications remain identified in the bibliography. No public raw-corpus deposit is claimed.</p>
<h2 id="authors">Authors</h2>

<table>
  <tr>
    <th align="left">Author</th>
    <th align="left">Affiliation</th>
  </tr>
  <tr>
    <td><strong>Reinaldo Betancourt</strong></td>
    <td>Faculty of Engineering, Department of Circuits and Measurements, Universidad de Los Andes, Mérida, Venezuela</td>
  </tr>
  <tr>
    <td><strong>Ingrid Nicole Vásconez</strong></td>
    <td>Centro de Biotecnología Vegetal, Facultad de Ciencias de la Vida, Universidad Andres Bello, Santiago, Chile</td>
  </tr>
  <tr>
    <td><strong>Viviana Moya</strong></td>
    <td>Departamento de Automatización y Control Industrial, Escuela Politécnica Nacional, Quito, Ecuador</td>
  </tr>
  <tr>
    <td><strong>William Chamorro</strong></td>
    <td>Departamento de Automatización y Control Industrial, Escuela Politécnica Nacional, Quito, Ecuador</td>
  </tr>
  <tr>
    <td><strong>Sandra Cano</strong></td>
    <td>School of Informatics Engineering, Pontificia Universidad Católica de Valparaíso, Valparaíso, Chile</td>
  </tr>
  <tr>
    <td><strong>Marco Antonio Molina</strong></td>
    <td>Faculty of Engineering, Department of Circuits and Measurements, Universidad de Los Andes, Mérida, Venezuela</td>
  </tr>
  <tr>
    <td><strong>Juan Pablo Vásconez</strong></td>
    <td>Faculty of Engineering, Universidad Andres Bello, Santiago, Chile; ANID – Millennium Nucleus in Data Science for Plant Resilience</td>
  </tr>
</table>

<p>
  <strong>Corresponding author:</strong> Juan Pablo Vásconez
</p>

<hr>




