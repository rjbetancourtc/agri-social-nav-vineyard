<div align="center">

<h1>
Anisotropic Proxemic Fields for Social Navigation<br>
in Agricultural Vineyard Corridors
</h1>

<p>
<strong>Simulation Framework · Navigation Algorithms · Experimental Dataset · Statistical Analysis</strong>
</p>

<p>
Reinaldo Betancourt<sup>1</sup> ·
Ingrid Nicole Vásconez<sup>2</sup> ·
Viviana Moya<sup>3</sup> ·
William Chamorro<sup>3</sup> ·
Sandra Cano<sup>4</sup> ·
Juan Pablo Vásconez<sup>1,5</sup>
</p>

<p>
<sub>
<sup>1</sup> Faculty of Engineering, Universidad de Los Andes, Mérida, Venezuela <br>
<sup>2</sup> Centro de Biotecnología Vegetal, Universidad Andrés Bello, Chile<br>
<sup>3</sup> Escuela Politécnica Nacional, Ecuador<br>
<sup>4</sup> Pontificia Universidad Católica de Valparaíso, Chile<br>
<sup>5</sup> ANID Millennium Nucleus in Data Science for Plant Resilience — PhytoLearning
</sub>
</p>

<p>
<strong>Unity</strong> ·
<strong>NavMesh</strong> ·
<strong>MATLAB</strong> ·
<strong>UDP Telemetry</strong> ·
<strong>Social Navigation</strong> ·
<strong>Human–Robot Interaction</strong>
</p>

</div>

<hr>

<h2>Repository Overview</h2>

<p>
This repository contains the complete simulation, experimentation, data-processing, and statistical-analysis framework developed for the study of socially aware navigation of agricultural mobile robots operating in narrow vineyard corridors shared with human workers.
</p>

<p>
The project investigates how autonomous agricultural robots can move efficiently through geometrically constrained environments while maintaining socially acceptable human–robot distances, avoiding unsafe close-contact interactions, reducing unnecessary stops, preventing operational deadlocks, and preserving mission completion performance.
</p>

<p>
The experimental platform integrates a vineyard environment developed in <strong>Unity</strong>, global path planning based on <strong>NavMesh</strong>, multiple local social-navigation strategies, real-time telemetry through <strong>UDP communication</strong>, and post-processing, visualization, statistical inference, effect-size analysis, and multicriteria evaluation in <strong>MATLAB</strong>.
</p>

<p>
The repository provides the implementation and experimental evaluation of nine navigation strategies, including classical navigation, binary safety supervisors, isotropic and anisotropic proxemic fields, and representative methods from the main families of social and local robot navigation:
</p>

<p align="center">
<strong>
M0 NavMesh ·
M1 Threshold Stop ·
M2 Hysteresis Supervisor ·
M3 Isotropic Proxemics ·
M4 Anisotropic Proxemic Navigation
</strong>
</p>

<p align="center">
<strong>
Social DWA ·
ORCA/RVO ·
Social Force Model ·
Control Barrier Functions
</strong>
</p>

<p>
The proposed <strong>M4 navigation strategy</strong> uses an orientation-dependent anisotropic proxemic field, multi-human social influence aggregation, continuous local avoidance, and a non-zero escape velocity outside the intimate interaction region. This architecture is designed to preserve robot mobility in the presence of stationary workers, particularly in narrow agricultural corridors where conventional binary STOP/GO supervisors may become permanently blocked.
</p>

<hr>

<h2>Experimental Framework</h2>

<p>
All navigation methods are evaluated under a common experimental protocol using an <strong>A → B → A</strong> navigation mission in a simulated vineyard environment. The complete experimental corpus contains <strong>343 valid runs</strong> across <strong>9 navigation methods</strong> and <strong>4 representative human–robot interaction scenarios</strong>.
</p>

<p>
<strong>E1 — Frontal Encounter:</strong>
evaluation of robot response during a direct frontal interaction with a human worker.
</p>

<p>
<strong>E2 — Lateral Intrusion:</strong>
evaluation of the navigation response when a human agent enters or crosses the robot trajectory laterally.
</p>

<p>
<strong>E3 — Social Following:</strong>
evaluation of socially appropriate navigation behavior relative to a moving human agent.
</p>

<p>
<strong>E4 — Multi-Agent Congestion:</strong>
evaluation of navigation reliability, deadlock resistance, path generation, and operational continuity in a constrained corridor occupied by multiple stationary workers.
</p>

<hr>

<h2>System Architecture</h2>

<p>
The experimental platform follows a hybrid global–local navigation architecture. Unity NavMesh provides the global reference direction, while the active local navigation method continuously evaluates the spatial relationship between the robot and nearby human agents.
</p>

<p>
The complete processing chain is:
</p>

<div align="center">

<p>
<strong>
Vineyard Environment
→ NavMesh Global Planning
→ Human Detection
→ Social Navigation Policy
→ Robot Motion
→ UDP Telemetry
→ MATLAB Processing
→ Statistical Analysis
</strong>
</p>

</div>

<p>
The telemetry system records robot–human distance, robot velocity, numerical acceleration, robot position, mission state, navigation method, and experimental scenario. These signals are used to reconstruct trajectories and compute safety, social, kinematic, and operational performance metrics.
</p>

<hr>

<h2>Proxemic Navigation Model</h2>

<p>
Human–robot interaction is represented through four distance-based proxemic regions:
</p>

<p align="center">
<strong>
Intimate: d &lt; 0.45 m
&nbsp;&nbsp;·&nbsp;&nbsp;
Personal: 0.45–1.20 m
&nbsp;&nbsp;·&nbsp;&nbsp;
Social: 1.20–3.60 m
&nbsp;&nbsp;·&nbsp;&nbsp;
Public: d ≥ 3.60 m
</strong>
</p>

<p>
The proposed M4 controller extends conventional distance-only interaction models by including the orientation of each person. Consequently, frontal and lateral approaches produce different social influence distributions. Multiple human influences are aggregated to generate a continuous local avoidance direction while preserving the global mission objective generated by NavMesh.
</p>

<p>
A key characteristic of the controller is the preservation of a small non-zero escape velocity outside the intimate region. This mechanism allows the robot to continue generating avoidance motion around stationary workers instead of waiting indefinitely for them to move.
</p>

<hr>

<h2>Evaluation Metrics</h2>

<p>
The repository includes tools for the computation and analysis of:
</p>

<p>
minimum human–robot distance · mean human–robot distance · personal-zone occupancy · intimate-zone occupancy · mission time · trajectory length · mean velocity · velocity standard deviation · maximum velocity · numerical acceleration · accumulated stop time · number of stops · mission success rate
</p>

<p>
The statistical analysis framework includes <strong>Welch's t-test</strong>, <strong>Hedges' g</strong>, <strong>Mann–Whitney U</strong>, <strong>Cliff's δ</strong>, <strong>Benjamini–Hochberg correction</strong>, <strong>Fisher's exact test</strong>, <strong>χ² analysis</strong>, <strong>Cramér's V</strong>, <strong>TOPSIS multicriteria evaluation</strong>, and <strong>Monte Carlo weight-sensitivity analysis</strong>.
</p>

<hr>

<h2>Research Objective</h2>

<p>
The purpose of this repository is not to present a single universally optimal navigation algorithm. Instead, it provides a reproducible framework for studying the engineering trade-off between:
</p>

<p align="center">
<strong>
Human Safety · Social Compliance · Trajectory Efficiency · Motion Smoothness · Throughput · Operational Reliability
</strong>
</p>

<p>
The experimental results show that different navigation families occupy complementary regions of this performance space. ORCA emphasizes velocity smoothness and throughput, CBF-based navigation emphasizes separation distance, the Social Force Model provides an intermediate continuous-response solution, Social DWA emphasizes social-cost-aware local planning, and the M4 anisotropic proxemic strategy provides an interpretable balance between orientation-aware social behavior, path efficiency, and operational continuity.
</p>

<hr>

<h2>Repository Contents</h2>

<p>
<strong>unity/</strong> — Vineyard simulation environment, robot controllers, human agents, navigation strategies, NavMesh integration, and UDP telemetry.
</p>

<p>
<strong>matlab/</strong> — Data acquisition, signal processing, visualization, metric extraction, statistical analysis, effect-size calculation, and multicriteria evaluation.
</p>

<p>
<strong>results/</strong> — Experimental datasets, processed metrics, tables, figures, and statistical outputs.
</p>

<p>
<strong>docs/</strong> — Technical documentation, diagrams, supplementary explanations, and supporting material.
</p>

<p>
<strong>referencias/</strong> — Scientific references and complementary research documentation.
</p>

<hr>

<h2>Associated Research</h2>

<p>
<strong>
Anisotropic Proxemic Fields vs. Social Navigation Baselines for Agricultural Robot Navigation in Vineyard Corridors: A Statistical Comparison with Effect Sizes
</strong>
</p>

<p>
This repository accompanies the research framework developed for the comparative study of anisotropic proxemic fields and representative social-navigation baselines in constrained agricultural environments.
</p>

<hr>

<h2>Authors</h2>

<p>
<strong>Reinaldo Betancourt</strong><br>
Mechanical Engineering · Robotics · Simulation · Human–Robot Interaction
</p>

<p>
<strong>Ingrid Nicole Vásconez</strong><br>
Centro de Biotecnología Vegetal, Universidad Andrés Bello
</p>

<p>
<strong>Viviana Moya</strong><br>
Departamento de Automatización y Control Industrial, Escuela Politécnica Nacional
</p>

<p>
<strong>William Chamorro</strong><br>
Departamento de Automatización y Control Industrial, Escuela Politécnica Nacional
</p>

<p>
<strong>Sandra Cano</strong><br>
School of Informatics Engineering, Pontificia Universidad Católica de Valparaíso
</p>

<p>
<strong>Juan Pablo Vásconez</strong><br>
Faculty of Engineering, Universidad Andrés Bello · PhytoLearning
</p>

<hr>

<div align="center">

<p>
<strong>
Agricultural Robotics · Social Navigation · Proxemics · Autonomous Mobile Robots · HRI · Unity · MATLAB
</strong>
</p>

<p>
<sub>
Research repository for reproducible experimentation in socially aware agricultural robot navigation.
</sub>
</p>

</div>
