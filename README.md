<!-- ============================================================
     REPOSITORY IDENTIFICATION — GitHub README compatible
     Repository: agri-social-nav-vineyard
     ============================================================ -->

<div align="center">

  <p>
    <strong>OPEN RESEARCH REPOSITORY</strong><br>
    Agricultural Robotics · Human–Robot Interaction · Social Navigation
  </p>

  <h1>🌿 Agri-Social Navigation in Vineyard Corridors</h1>

  <h3>
    Anisotropic Proxemic Fields for Socially Aware Agricultural Robot Navigation
  </h3>

  <p>
    A reproducible <strong>Unity–MATLAB</strong> framework for simulating,
    implementing, instrumenting, and statistically evaluating socially aware
    navigation strategies for agricultural mobile robots operating in narrow
    vineyard corridors shared with human workers.
  </p>

  <p>
    <strong>Authors:</strong><br>
    Reinaldo Betancourt · Ingrid Nicole Vásconez · Viviana Moya ·
    William Chamorro · Sandra Cano · Marco Antonio Molina ·
    Juan Pablo Vásconez
  </p>

  <p>
    <img alt="Repository"
         src="https://img.shields.io/badge/GitHub-agri--social--nav--vineyard-181717?logo=github">
    <img alt="Unity"
         src="https://img.shields.io/badge/Unity-Digital_Twin-000000?logo=unity">
    <img alt="MATLAB"
         src="https://img.shields.io/badge/MATLAB-Data_Analysis-E16737">
    <img alt="Methods"
         src="https://img.shields.io/badge/Navigation_Methods-9-2F855A">
    <img alt="Navigation runs"
         src="https://img.shields.io/badge/Valid_Navigation_Runs-360-2563EB">
    <img alt="Scenarios"
         src="https://img.shields.io/badge/HRI_Scenarios-4-7C3AED">
    <img alt="Benchmark"
         src="https://img.shields.io/badge/Computational_Benchmark-54_Runs-8B5CF6">
    <img alt="Recorded telemetry"
         src="https://img.shields.io/badge/Recorded_Telemetry-4.387_Hz-0F766E">
  </p>

  <p>
    Purpose · Experimental identity · Methods · Architecture ·
    Repository structure · Evaluation · Manuscript
  </p>

</div>

<hr>

<h2 id="repository-purpose">Repository Purpose</h2>

<p>
  This repository contains the simulation environment, navigation controllers,
  telemetry tools, experimental data, analysis scripts, figures, and scientific
  documentation used to investigate socially aware navigation of agricultural
  mobile robots in human-populated vineyard environments.
</p>

<p>
  The engineering objective is to study the balance among
  <strong>human–robot separation</strong>, <strong>collision avoidance</strong>,
  <strong>trajectory efficiency</strong>, <strong>kinematic behavior</strong>,
  and <strong>mission completion</strong> under the simulated conditions.
  The reported experiments do not constitute validation on a physical robot.
</p>

<h2 id="experimental-identity">Experimental Identity</h2>

<table>
  <tr>
    <th align="left">Identifier</th>
    <th align="left">Description</th>
  </tr>
  <tr>
    <td><strong>Repository</strong></td>
    <td><code>agri-social-nav-vineyard</code></td>
  </tr>
  <tr>
    <td><strong>Research domain</strong></td>
    <td>Agricultural robotics, social navigation, HRI, and digital twins</td>
  </tr>
  <tr>
    <td><strong>Simulation platform</strong></td>
    <td>Unity with NavMesh global path planning</td>
  </tr>
  <tr>
    <td><strong>Analysis platform</strong></td>
    <td>MATLAB for telemetry processing, statistics, visualization, and multicriteria analysis</td>
  </tr>
  <tr>
    <td><strong>Mission</strong></td>
    <td>Common <code>A → B → A</code> navigation task</td>
  </tr>
  <tr>
    <td><strong>Navigation campaign</strong></td>
    <td>9 methods × 4 scenarios × 10 repetitions = 360 runs</td>
  </tr>
  <tr>
    <td><strong>Run accounting reported in the revised manuscript</strong></td>
    <td>360 planned, 360 executed, 360 valid, 0 excluded, 360 successful, 0 failed</td>
  </tr>
  <tr>
    <td><strong>Separate computational benchmark</strong></td>
    <td>9 methods × 2 scenarios × 3 repetitions = 54 runs</td>
  </tr>
  <tr>
    <td><strong>Telemetry timing</strong></td>
    <td>Nominal 0.10 s interval; approximately 4.387 Hz average recorded frequency</td>
  </tr>
  <tr>
    <td><strong>Primary contribution</strong></td>
    <td>
      Longitudinal–lateral anisotropic proxemic navigation with front–rear
      symmetry and continuous velocity regulation in the evaluated M4 controller
    </td>
  </tr>
</table>

<h2 id="navigation-methods">Navigation Methods</h2>

<table>
  <tr>
    <th align="left">Group</th>
    <th align="left">ID</th>
    <th align="left">Navigation strategy</th>
  </tr>
  <tr>
    <td rowspan="5"><strong>In-house methods</strong></td>
    <td><strong>M0</strong></td>
    <td>NavMesh only</td>
  </tr>
  <tr>
    <td><strong>M1</strong></td>
    <td>Distance-threshold stop supervisor</td>
  </tr>
  <tr>
    <td><strong>M2</strong></td>
    <td>Hysteresis-based supervisor</td>
  </tr>
  <tr>
    <td><strong>M3</strong></td>
    <td>Continuous isotropic proxemic-field navigation</td>
  </tr>
  <tr>
    <td><strong>M4</strong></td>
    <td>Longitudinal–lateral anisotropic proxemic navigation with velocity modulation</td>
  </tr>
  <tr>
    <td rowspan="4"><strong>Comparison baselines</strong></td>
    <td><strong>B1</strong></td>
    <td>Social Dynamic Window Approach</td>
  </tr>
  <tr>
    <td><strong>B2</strong></td>
    <td>ORCA / RVO reciprocal collision avoidance</td>
  </tr>
  <tr>
    <td><strong>B3</strong></td>
    <td>Social Force Model</td>
  </tr>
  <tr>
    <td><strong>B4</strong></td>
    <td>CBF-SocialDWA with a control-barrier-based safety layer</td>
  </tr>
</table>

<p>
  The cited literature motivates these algorithmic families. The numerical
  settings reported in the manuscript describe the evaluated implementations;
  the references should not be interpreted as prescribing every experimental
  parameter value.
</p>

<h2 id="system-architecture">System Architecture</h2>

<div align="center">
  <p>
    <code>
      Vineyard Digital Twin
      → NavMesh Global Planner
      → Human-State Information
      → Local Navigation Policy
      → Robot Motion
      → UDP Telemetry
      → MATLAB Processing
      → Statistical and Multicriteria Analysis
    </code>
  </p>
</div>

<h3>Proxemic model</h3>

<table>
  <tr>
    <th>Zone</th>
    <th>Robot–human distance</th>
    <th>Operational interpretation</th>
  </tr>
  <tr>
    <td><strong>Intimate</strong></td>
    <td><code>d &lt; 0.45 m</code></td>
    <td>Critical close-contact region</td>
  </tr>
  <tr>
    <td><strong>Personal</strong></td>
    <td><code>0.45 m ≤ d &lt; 1.20 m</code></td>
    <td>Active avoidance and velocity regulation</td>
  </tr>
  <tr>
    <td><strong>Social</strong></td>
    <td><code>1.20 m ≤ d &lt; 3.60 m</code></td>
    <td>Monitoring and anticipatory response</td>
  </tr>
  <tr>
    <td><strong>Public</strong></td>
    <td><code>d ≥ 3.60 m</code></td>
    <td>Nominal navigation</td>
  </tr>
</table>

<h2>Human–Robot Interaction Scenarios</h2>

<table>
  <tr>
    <th align="left">Scenario</th>
    <th align="left">Description</th>
  </tr>
  <tr>
    <td><strong>E1 — Frontal encounter</strong></td>
    <td>Direct robot–worker interaction inside a narrow corridor</td>
  </tr>
  <tr>
    <td><strong>E2 — Lateral intrusion</strong></td>
    <td>A human enters or crosses the robot trajectory laterally</td>
  </tr>
  <tr>
    <td><strong>E3 — Social following</strong></td>
    <td>The robot navigates relative to a moving human agent</td>
  </tr>
  <tr>
    <td><strong>E4 — Multi-agent congestion</strong></td>
    <td>Several workers constrain the available corridor space</td>
  </tr>
</table>

<h2 id="repository-structure">Repository Structure</h2>

<table>
  <tr>
    <th align="left">Path</th>
    <th align="left">Contents</th>
  </tr>
  <tr>
    <td><code>docs/</code></td>
    <td>Technical documentation, methodology, and supporting material</td>
  </tr>
  <tr>
    <td><code>unity/scripts/</code></td>
    <td>Unity C# controllers, navigation logic, and UDP instrumentation</td>
  </tr>
  <tr>
    <td><code>matlab/scripts/</code></td>
    <td>Telemetry processing, statistics, visualization, and analysis scripts</td>
  </tr>
  <tr>
    <td><code>results/Data2/Results/</code></td>
    <td>Final unified 360-run navigation dataset: run-level CSV, MAT, and metrics files</td>
  </tr>
  <tr>
    <td><code>results/Data2/ControllerBenchmark/</code></td>
    <td>Separate 54-run computational benchmark and aggregate summary</td>
  </tr>
  <tr>
    <td><code>results/</code></td>
    <td>Also contains earlier results retained for historical traceability</td>
  </tr>
  <tr>
    <td><code>referencias/</code></td>
    <td>Scientific references and supporting publications</td>
  </tr>
  <tr>
    <td><code>Vasconez.pdf</code></td>
    <td>Research manuscript associated with the repository</td>
  </tr>
</table>

<p>
  <strong>Dataset separation:</strong>
  The earlier root-level <code>results/master_log*.csv</code> files must not
  be merged with <code>results/Data2/Results/</code> when reproducing the
  revised manuscript. Historical counts, including 343 valid runs, do not
  describe the final 360-run campaign.
</p>

<h2 id="evaluation-pipeline">Evaluation Pipeline</h2>

<p>
  The revised manuscript uses a common run-level telemetry and metrics
  pipeline for all nine methods. Its principal analysis models
  <strong>Method</strong>, <strong>Scenario</strong>, and their interaction
  while retaining the paired experimental structure.
</p>

<table>
  <tr>
    <th align="left">Analysis component</th>
    <th align="left">Reported approach</th>
  </tr>
  <tr>
    <td><strong>Factorial inference</strong></td>
    <td>Repeated-measures Method × Scenario analysis with Greenhouse–Geisser correction where applicable</td>
  </tr>
  <tr>
    <td><strong>Sensitivity analysis</strong></td>
    <td>Linear mixed-effects analysis accounting for the paired run/seed structure</td>
  </tr>
  <tr>
    <td><strong>Scenario-specific contrasts</strong></td>
    <td>Paired differences, 95% confidence intervals, Cohen's d<sub>z</sub>, and Holm adjustment</td>
  </tr>
  <tr>
    <td><strong>Multicriteria comparison</strong></td>
    <td>Five-criterion TOPSIS with equal-weight reference and alternative weight profiles</td>
  </tr>
  <tr>
    <td><strong>Safety-distance criterion</strong></td>
    <td>Saturating utility rather than an unbounded reward for increasing separation</td>
  </tr>
  <tr>
    <td><strong>Pareto analysis</strong></td>
    <td>Full five-criterion vectors and a 9 × 9 dominance matrix</td>
  </tr>
  <tr>
    <td><strong>Computational profiling</strong></td>
    <td>Separate 54-run benchmark of latency, effective invocation frequency, and CPU measurements</td>
  </tr>
</table>

<p>
  The numerical maximum-acceleration diagnostic and the constant success
  outcome are not criteria in the primary TOPSIS analysis. A two-dimensional
  Pareto projection is used for visualization, not as proof of dominance in
  the full criterion space.
</p>

<details>
  <summary><strong>Core engineering contribution and scope</strong></summary>

  <p>
    The evaluated M4 strategy uses a longitudinal–lateral anisotropic
    proxemic field with front–rear symmetry, referenced to the nearest
    detected human, together with continuous velocity regulation.
    The current description does not claim a guaranteed nonzero minimum
    escape velocity or multi-human influence aggregation.
  </p>

  <p>
    The results characterize the evaluated Unity simulation scenarios.
    Physical deployment, worst-case real-time behavior, and safety with
    real workers require separate validation.
  </p>
</details>

<h2 id="data-availability">Data Availability and Versioning</h2>

<p>
  The final navigation dataset is available under
  <a href="./results/Data2/Results/"><code>results/Data2/Results/</code></a>.
  The separate computational benchmark is available under
  <a href="./results/Data2/ControllerBenchmark/"><code>results/Data2/ControllerBenchmark/</code></a>.
  The 54 benchmark runs are not part of the 360-run navigation sample.
</p>

<p>
  The revised manuscript also refers to Supplementary Table S1 for the
  complete scenario-specific statistical contrasts. That table must be
  checked and supplied as supplementary material; a reference to S1 in
  the manuscript does not mean that its file is already included here.
</p>

<h2>Authors</h2>

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
