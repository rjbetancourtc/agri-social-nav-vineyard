<!-- ============================================================
     PROJECT INTRODUCTION
     Anisotropic Proxemic Fields for Agricultural Robot Navigation
     ============================================================ -->

<section id="project-introduction" class="project-intro">

    <!-- HERO -->
    <header class="intro-header">

        <p class="project-tags">
            Unity · NavMesh · MATLAB · UDP Telemetry · Social Navigation ·
            Human–Robot Interaction · Agricultural Robotics
        </p>

        <h1>
            Socially Aware Navigation for Agricultural Mobile Robots
            in Narrow Vineyard Corridors
        </h1>

        <p class="project-subtitle">
            A reproducible Unity–MATLAB experimental framework for the
            simulation, implementation, telemetry acquisition, statistical
            evaluation, and multicriteria comparison of socially aware
            navigation strategies for agricultural mobile robots operating
            in human-populated vineyard environments.
        </p>

    </header>


    <!-- REPOSITORY OVERVIEW -->
    <section class="intro-block">

        <h2>Repository Overview</h2>

        <p>
            This repository contains the complete simulation,
            experimentation, telemetry, data-processing, visualization,
            statistical-analysis, and multicriteria-evaluation framework
            developed for the study of socially aware navigation of
            agricultural mobile robots operating in narrow vineyard
            corridors shared with human workers.
        </p>

        <p>
            The research investigates how autonomous agricultural robots
            can navigate efficiently through geometrically constrained
            environments while maintaining socially acceptable
            human–robot separation distances, reducing unsafe close-contact
            interactions, preventing operational deadlocks, limiting
            unnecessary stops, and preserving mission-completion reliability.
        </p>

        <p>
            The experimental platform integrates a vineyard environment
            developed in <strong>Unity</strong>, global path generation
            through <strong>NavMesh</strong>, multiple local
            social-navigation strategies, real-time
            <strong>UDP telemetry</strong>, and a complete
            <strong>MATLAB</strong> pipeline for signal processing,
            trajectory reconstruction, statistical inference,
            effect-size estimation, visualization, and multicriteria
            decision analysis.
        </p>

    </section>


    <!-- RESEARCH FRAMEWORK -->
    <section class="intro-block">

        <h2>Research Framework</h2>

        <p>
            Nine navigation strategies are implemented and evaluated under
            a common experimental architecture. The comparison includes
            incremental in-house navigation strategies and representative
            algorithms from the principal families of social and local
            robot navigation.
        </p>

        <div class="method-groups">

            <div class="method-group">
                <h3>In-House Navigation Strategies</h3>

                <ul>
                    <li><strong>M0:</strong> NavMesh Only</li>
                    <li><strong>M1:</strong> Threshold Stop</li>
                    <li><strong>M2:</strong> Hysteresis Supervisor</li>
                    <li><strong>M3:</strong> Isotropic Proxemic Navigation</li>
                    <li>
                        <strong>M4:</strong>
                        Full Anisotropic Proxemic Navigation
                    </li>
                </ul>
            </div>

            <div class="method-group">
                <h3>External Navigation Baselines</h3>

                <ul>
                    <li>
                        <strong>B1:</strong>
                        Social Dynamic Window Approach
                    </li>

                    <li>
                        <strong>B2:</strong>
                        ORCA / RVO Reciprocal Collision Avoidance
                    </li>

                    <li>
                        <strong>B3:</strong>
                        Social Force Model
                    </li>

                    <li>
                        <strong>B4:</strong>
                        Control-Barrier-Function Social DWA
                    </li>
                </ul>
            </div>

        </div>

    </section>


    <!-- PROPOSED METHOD -->
    <section class="intro-block highlight-block">

        <h2>Proposed M4 Navigation Strategy</h2>

        <p>
            The proposed M4 controller combines an
            <strong>orientation-dependent anisotropic proxemic field</strong>,
            multi-human influence aggregation, continuous local avoidance,
            velocity modulation, and a non-zero minimum escape velocity
            outside the intimate interaction region.
        </p>

        <p>
            Unlike binary STOP/GO safety supervisors, the controller does
            not require a nearby stationary worker to move before the robot
            can continue navigating. Instead, the robot preserves a small
            avoidance velocity that allows it to generate lateral escape
            motion and recover progress while maintaining the global mission
            direction provided by NavMesh.
        </p>

        <p>
            The anisotropic formulation explicitly considers human
            orientation. Consequently, frontal and lateral approaches
            generate different social-influence distributions, providing
            an interpretable mechanism for orientation-aware
            human–robot interaction.
        </p>

    </section>


    <!-- EXPERIMENTAL FRAMEWORK -->
    <section class="intro-block">

        <h2>Experimental Framework</h2>

        <p>
            All navigation strategies are evaluated using a common
            <strong>A → B → A</strong> navigation mission in the same
            simulated vineyard environment.
        </p>

        <p>
            The complete experimental corpus contains:
        </p>

        <div class="experiment-stats">

            <article class="stat-card">
                <span class="stat-value">343</span>
                <span class="stat-label">Valid Experimental Runs</span>
            </article>

            <article class="stat-card">
                <span class="stat-value">9</span>
                <span class="stat-label">Navigation Methods</span>
            </article>

            <article class="stat-card">
                <span class="stat-value">4</span>
                <span class="stat-label">HRI Scenarios</span>
            </article>

            <article class="stat-card">
                <span class="stat-value">10 Hz</span>
                <span class="stat-label">Telemetry Sampling Rate</span>
            </article>

        </div>

    </section>


    <!-- SCENARIOS -->
    <section class="intro-block">

        <h2>Human–Robot Interaction Scenarios</h2>

        <div class="scenario-grid">

            <article class="scenario-card">
                <h3>E1 — Frontal Encounter</h3>

                <p>
                    Evaluates robot response during a direct frontal
                    interaction with a human worker.
                </p>
            </article>


            <article class="scenario-card">
                <h3>E2 — Lateral Intrusion</h3>

                <p>
                    Evaluates the navigation response when a human agent
                    enters or crosses the robot trajectory laterally.
                </p>
            </article>


            <article class="scenario-card">
                <h3>E3 — Social Following</h3>

                <p>
                    Evaluates socially appropriate robot behavior relative
                    to a moving human agent.
                </p>
            </article>


            <article class="scenario-card">
                <h3>E4 — Multi-Agent Congestion</h3>

                <p>
                    Evaluates navigation reliability, path generation,
                    deadlock resistance, and operational continuity in a
                    narrow corridor occupied by multiple stationary workers.
                </p>
            </article>

        </div>

    </section>


    <!-- SYSTEM ARCHITECTURE -->
    <section class="intro-block">

        <h2>System Architecture</h2>

        <p>
            The platform follows a hybrid global–local navigation
            architecture. Unity NavMesh generates the global reference
            direction, while the active local navigation strategy
            continuously evaluates the spatial relationship between the
            robot and nearby human agents.
        </p>

        <div class="architecture-flow">

            <span>Vineyard Environment</span>
            <span class="arrow">→</span>

            <span>NavMesh Global Planning</span>
            <span class="arrow">→</span>

            <span>Human Detection</span>
            <span class="arrow">→</span>

            <span>Social Navigation Policy</span>
            <span class="arrow">→</span>

            <span>Robot Motion</span>
            <span class="arrow">→</span>

            <span>UDP Telemetry</span>
            <span class="arrow">→</span>

            <span>MATLAB Processing</span>
            <span class="arrow">→</span>

            <span>Statistical Analysis</span>

        </div>

    </section>


    <!-- PROXEMICS -->
    <section class="intro-block">

        <h2>Proxemic Interaction Model</h2>

        <p>
            Human–robot interaction is represented using four
            distance-dependent proxemic regions:
        </p>

        <div class="proxemic-zones">

            <div class="zone">
                <strong>Intimate</strong>
                <span>d &lt; 0.45 m</span>
            </div>

            <div class="zone">
                <strong>Personal</strong>
                <span>0.45 m ≤ d &lt; 1.20 m</span>
            </div>

            <div class="zone">
                <strong>Social</strong>
                <span>1.20 m ≤ d &lt; 3.60 m</span>
            </div>

            <div class="zone">
                <strong>Public</strong>
                <span>d ≥ 3.60 m</span>
            </div>

        </div>

        <p>
            M4 extends conventional distance-only interaction models by
            incorporating the orientation of each human agent and by
            aggregating multiple human influences into a continuous
            local avoidance command.
        </p>

    </section>


    <!-- TELEMETRY -->
    <section class="intro-block">

        <h2>Telemetry and Data Processing</h2>

        <p>
            During each experimental run, the Unity simulation transmits
            real-time telemetry to MATLAB through UDP communication.
            The recorded signals include:
        </p>

        <ul class="metric-list">
            <li>Robot–human minimum distance</li>
            <li>Robot velocity</li>
            <li>Numerical acceleration</li>
            <li>Robot Cartesian position</li>
            <li>Mission state</li>
            <li>Navigation method identifier</li>
            <li>Experimental scenario identifier</li>
        </ul>

        <p>
            These signals are used to reconstruct robot trajectories and
            calculate social, safety, kinematic, efficiency, and operational
            performance indicators.
        </p>

    </section>


    <!-- METRICS -->
    <section class="intro-block">

        <h2>Evaluation Metrics</h2>

        <p>
            The repository includes tools for computing and analyzing:
        </p>

        <p class="metrics-inline">
            Minimum human–robot distance · Mean human–robot distance ·
            Personal-zone occupancy · Intimate-zone occupancy ·
            Mission time · Trajectory length · Mean velocity ·
            Velocity standard deviation · Maximum velocity ·
            Numerical acceleration · Accumulated stop time ·
            Number of stops · Mission success rate
        </p>

    </section>


    <!-- STATISTICS -->
    <section class="intro-block">

        <h2>Statistical and Multicriteria Analysis</h2>

        <p>
            The analysis pipeline combines parametric, non-parametric,
            categorical, effect-size, and multicriteria methods.
        </p>

        <p class="metrics-inline">
            Welch's t-test · Welch ANOVA · Hedges' g ·
            Mann–Whitney U · Cliff's δ ·
            Benjamini–Hochberg correction · Fisher's exact test ·
            χ² analysis · Cramér's V · TOPSIS ·
            Monte Carlo weight-sensitivity analysis
        </p>

    </section>


    <!-- KEY FINDINGS -->
    <section class="intro-block highlight-block">

        <h2>Principal Engineering Findings</h2>

        <p>
            The experiments demonstrate that binary threshold-based
            supervisors can become operationally blocked in the presence
            of stationary workers, while continuous-action strategies
            preserve substantially higher navigation continuity.
        </p>

        <p>
            The evaluated navigation families occupy complementary
            regions of the social–throughput performance space:
        </p>

        <ul>
            <li>
                <strong>ORCA/RVO</strong> emphasizes mission speed and
                velocity smoothness, but its reciprocity assumption becomes
                problematic when interacting with non-reactive workers.
            </li>

            <li>
                <strong>CBF-based navigation</strong> emphasizes separation
                distance through hard safety constraints, but this
                conservatism may reduce liveness in constrained scenarios.
            </li>

            <li>
                <strong>Social Force Model</strong> provides a continuous
                intermediate response with balanced navigation behavior.
            </li>

            <li>
                <strong>Social DWA</strong> emphasizes socially aware
                local velocity selection and reduced proxemic occupancy,
                with an associated throughput cost.
            </li>

            <li>
                <strong>M4</strong> provides an interpretable balance between
                orientation-aware social behavior, trajectory efficiency,
                low stop frequency, and operational continuity.
            </li>
        </ul>

    </section>


    <!-- RESEARCH OBJECTIVE -->
    <section class="intro-block">

        <h2>Research Objective</h2>

        <p>
            The purpose of this repository is not to claim the existence
            of a universally optimal social-navigation algorithm.
            Instead, the objective is to provide a reproducible
            experimental framework for studying the engineering trade-off
            between:
        </p>

        <div class="research-objectives">

            <span>Human Safety</span>
            <span>Social Compliance</span>
            <span>Trajectory Efficiency</span>
            <span>Motion Smoothness</span>
            <span>Throughput</span>
            <span>Operational Reliability</span>

        </div>

        <p>
            The framework enables direct and statistically grounded
            comparison of navigation algorithms under identical
            environmental geometry, mission definition, human–robot
            interaction scenarios, telemetry acquisition, and evaluation
            criteria.
        </p>

    </section>


    <!-- REPOSITORY STRUCTURE -->
    <section class="intro-block">

        <h2>Repository Structure</h2>

        <pre><code>unity/
├── Vineyard simulation environment
├── Robot navigation controllers
├── Human agent controllers
├── NavMesh integration
├── Social navigation methods
└── UDP telemetry

matlab/
├── UDP data acquisition
├── Signal processing
├── Trajectory reconstruction
├── Metric extraction
├── Statistical inference
├── Effect-size analysis
├── Visualization
└── TOPSIS and Monte Carlo sensitivity

results/
├── Experimental datasets
├── Processed metrics
├── Statistical tables
├── Figures
└── Multicriteria results

docs/
├── Technical documentation
├── Architecture diagrams
├── Experimental protocol
└── Supplementary material

referencias/
├── Scientific references
└── Complementary research documentation</code></pre>

    </section>


    <!-- ASSOCIATED RESEARCH -->
    <section class="intro-block">

        <h2>Associated Research</h2>

        <p>
            <strong>
                Anisotropic Proxemic Fields vs. Social Navigation Baselines
                for Agricultural Robot Navigation in Vineyard Corridors:
                A Statistical Comparison with Effect Sizes
            </strong>
        </p>

        <p>
            This repository accompanies the simulation, experimental,
            telemetry, statistical, and multicriteria framework developed
            for the comparative study of anisotropic proxemic navigation
            and representative social-navigation baselines in constrained
            agricultural environments.
        </p>

    </section>


    <!-- KEYWORDS -->
    <footer class="intro-footer">

        <p>
            Agricultural Robotics · Social Navigation · Proxemics ·
            Autonomous Mobile Robots · Human–Robot Interaction ·
            Unity · NavMesh · MATLAB · UDP Telemetry ·
            ORCA · Social Force Model · DWA ·
            Control Barrier Functions
        </p>

    </footer>

</section>
