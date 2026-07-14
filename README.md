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
    <img alt="Runs"
         src="https://img.shields.io/badge/Valid_Runs-343-2563EB">
    <img alt="Scenarios"
         src="https://img.shields.io/badge/HRI_Scenarios-4-7C3AED">
    <img alt="Telemetry"
         src="https://img.shields.io/badge/UDP_Telemetry-10_Hz-0F766E">
  </p>

  <p>Purpose · Experimental identity · Methods · Architecture · Repository structure · Manuscript</p>

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
  The central engineering problem is to preserve an effective compromise among
  <strong>human–robot separation</strong>, <strong>collision avoidance</strong>,
  <strong>deadlock resistance</strong>, <strong>trajectory efficiency</strong>,
  <strong>kinematic smoothness</strong>, and
  <strong>mission-completion reliability</strong>.
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
    <td>MATLAB for telemetry processing, statistics, visualization, and TOPSIS</td>
  </tr>
  <tr>
    <td><strong>Mission</strong></td>
    <td>Common <code>A → B → A</code> navigation task</td>
  </tr>
  <tr>
    <td><strong>Experimental corpus</strong></td>
    <td>343 valid runs</td>
  </tr>
  <tr>
    <td><strong>Navigation methods</strong></td>
    <td>9 strategies: M0–M4 and B1–B4</td>
  </tr>
  <tr>
    <td><strong>HRI scenarios</strong></td>
    <td>4 vineyard interaction scenarios</td>
  </tr>
  <tr>
    <td><strong>Telemetry rate</strong></td>
    <td>10 Hz through UDP communication</td>
  </tr>
  <tr>
    <td><strong>Primary contribution</strong></td>
    <td>
      Orientation-dependent anisotropic proxemic navigation with continuous
      avoidance and non-zero escape velocity
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
    <td>Continuous proxemic-field navigation</td>
  </tr>
  <tr>
    <td><strong>M4</strong></td>
    <td>Full anisotropic proxemic navigation with velocity modulation</td>
  </tr>
  <tr>
    <td rowspan="4"><strong>External baselines</strong></td>
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
    <td>Control-Barrier-Function social planner</td>
  </tr>
</table>

<h2 id="system-architecture">System Architecture</h2>

<div align="center">
  <p>
    <code>
      Vineyard Digital Twin
      → NavMesh Global Planner
      → Human Detection
      → Social Navigation Policy
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
    <td>Active avoidance and velocity modulation</td>
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
    <td>Several stationary workers constrain the available corridor space</td>
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
    <td>Telemetry processing, statistics, visualization, and ranking scripts</td>
  </tr>
  <tr>
    <td><code>results/</code></td>
    <td>Experimental outputs, processed data, tables, and figures</td>
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

<h2>Evaluation Pipeline</h2>

<p>
  The analysis includes descriptive statistics, parametric and non-parametric
  inference, effect-size estimation, multiple-comparison correction,
  categorical success analysis, trajectory reconstruction, and multicriteria
  decision analysis.
</p>

<p>
  <strong>Representative methods:</strong>
  Welch's t-test · Hedges' g · Mann–Whitney U · Cliff's δ ·
  Benjamini–Hochberg correction · Fisher's exact test · χ² ·
  Cramér's V · TOPSIS · Monte Carlo weight-sensitivity analysis
</p>

<details>
  <summary><strong>Core engineering contribution</strong></summary>

  <p>
    The M4 strategy combines an orientation-dependent anisotropic proxemic
    field, multi-human influence aggregation, continuous local avoidance,
    velocity modulation, and a non-zero minimum escape velocity.
  </p>

  <p>
    This formulation is designed to preserve robot mobility in constrained
    scenarios where binary STOP/GO supervisors can become operationally
    blocked by stationary workers.
  </p>
</details>

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

</div>
