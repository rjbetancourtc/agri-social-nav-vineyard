# Results Directory Structure

The `results/` directory stores the experimental data generated during the
Unity–MATLAB simulation runs. It contains the consolidated master logs at the
root level and the individual telemetry files organized by navigation method
and human–robot interaction scenario.

The dataset supports the reproducible evaluation of socially aware navigation
strategies for agricultural mobile robots operating in narrow vineyard
corridors.

---

## Root files

| File | Description |
|---|---|
| `README.md` | General description of the results directory, folder organization, file conventions, and data-use recommendations. |
| `master_log.csv` | Original consolidated experimental log generated from the complete set of processed runs. |
| `master_log_clean.csv` | Cleaned master log used for statistical processing and publication-ready analysis. |
| `master_log_excel_es.csv` | Excel-compatible version using Spanish/European numeric formatting, normally with comma decimal separators and semicolon delimiters. |
| `master_log_excel_us.csv` | Excel-compatible version using US numeric formatting, normally with decimal points and comma delimiters. |
| `master_log_clean_excel_es.csv` | Cleaned Excel-compatible version using Spanish/European numeric formatting. |
| `master_log_clean_excel_us.csv` | Cleaned Excel-compatible version using US numeric formatting. |

### Recommended files

For MATLAB, Python, R, or automated statistical processing, use:

```text
master_log_clean.csv
```

For manual inspection in Microsoft Excel, use the file corresponding to the
regional configuration of the operating system:

```text
master_log_clean_excel_es.csv
master_log_clean_excel_us.csv
```

The original `master_log.csv` is retained for traceability and should not be
overwritten.

---

## Method folders

Experimental telemetry is organized by navigation method.

| Code | Navigation method | Description |
|---|---|---|
| `M0` | NavMesh Only | Pure global NavMesh navigation without a social interaction layer. |
| `M1` | Threshold Stop | Binary stop policy activated by a robot–human distance threshold. |
| `M2` | Hysteresis Supervisor | Binary STOP/GO supervisor with separate stopping and resumption thresholds. |
| `M3` | Continuous Proxemic Field | Continuous proxemic navigation based on a local human-influence field. |
| `M4` | Full Anisotropic Proxemic Navigation | Orientation-dependent anisotropic proxemic field with multi-human aggregation and non-zero escape velocity. |
| `B1` | Social Dynamic Window Approach | Sampling-based local planner with social-distance penalties. |
| `B2` | ORCA / RVO | Reciprocal collision-avoidance baseline based on velocity constraints. |
| `B3` | Social Force Model | Continuous force-based navigation using attractive and repulsive interactions. |
| `B4` | Control-Barrier-Function Planner | Safety-constrained local navigation using barrier-function conditions. |

---

## Scenario folders

Each method directory is subdivided into four experimental scenarios.

| Code | Scenario | Description |
|---|---|---|
| `E1` | Frontal Encounter | Direct frontal interaction between the robot and one human worker. |
| `E2` | Lateral Intrusion | A human agent enters or crosses the robot trajectory laterally. |
| `E3` | Social Following | The robot navigates relative to a moving human agent. |
| `E4` | Multi-Agent Congestion | Several stationary workers occupy a narrow vineyard corridor. |

---

## Expected directory tree

```text
results/
├── README.md
├── master_log.csv
├── master_log_clean.csv
├── master_log_excel_es.csv
├── master_log_excel_us.csv
├── master_log_clean_excel_es.csv
├── master_log_clean_excel_us.csv
│
├── M0/
│   ├── E1/
│   ├── E2/
│   ├── E3/
│   └── E4/
│
├── M1/
│   ├── E1/
│   ├── E2/
│   ├── E3/
│   └── E4/
│
├── M2/
│   ├── E1/
│   ├── E2/
│   ├── E3/
│   └── E4/
│
├── M3/
│   ├── E1/
│   ├── E2/
│   ├── E3/
│   └── E4/
│
├── M4/
│   ├── E1/
│   ├── E2/
│   ├── E3/
│   └── E4/
│
├── B1/
│   ├── E1/
│   ├── E2/
│   ├── E3/
│   └── E4/
│
├── B2/
│   ├── E1/
│   ├── E2/
│   ├── E3/
│   └── E4/
│
├── B3/
│   ├── E1/
│   ├── E2/
│   ├── E3/
│   └── E4/
│
└── B4/
    ├── E1/
    ├── E2/
    ├── E3/
    └── E4/
```

---

## Telemetry file naming convention

Individual telemetry files follow the general convention:

```text
{METHOD}_{SCENARIO}_run{NN}.csv
```

where:

- `{METHOD}` is one of `M0`, `M1`, `M2`, `M3`, `M4`, `B1`, `B2`, `B3`, or `B4`.
- `{SCENARIO}` is one of `E1`, `E2`, `E3`, or `E4`.
- `{NN}` is the run number written with two digits.

Examples:

```text
M0_E1_run01.csv
M4_E3_run10.csv
B2_E4_run07.csv
B4_E2_run03.csv
```

The filename uniquely identifies the navigation method, experimental scenario,
and independent repetition.

---

## Experimental dataset

The consolidated study contains:

| Item | Value |
|---|---:|
| Navigation methods | 9 |
| Experimental scenarios | 4 |
| Nominal method–scenario combinations | 36 |
| Telemetry sampling rate | 10 Hz |
| Valid runs in the consolidated analysis | 343 |

The number of files inside a method–scenario folder may differ from the nominal
number of planned repetitions when a run was rejected, timed out, interrupted,
or excluded during data-integrity verification.

The cleaned master log must therefore be treated as the authoritative index of
the runs used in the statistical analysis.

---

## Typical telemetry content

Depending on the navigation method and export version, an individual telemetry
file can contain variables such as:

| Variable | Meaning | Typical unit |
|---|---|---|
| `t` | Elapsed simulation time | s |
| `d_min` | Minimum distance to the nearest human agent | m |
| `v_robot` | Robot linear-speed magnitude | m/s |
| `a_robot` | Numerical acceleration magnitude | m/s² |
| `x_robot` | Robot Cartesian position along the x-axis | m |
| `y_robot` | Robot Cartesian position along the y-axis | m |
| `z_robot` | Robot Cartesian position along the z-axis | m |
| `mission_state` | Current mission or supervisor state | categorical |
| `method_id` | Navigation-method identifier | categorical |
| `scenario_id` | Experimental-scenario identifier | categorical |

The exact column names should be verified from the CSV header before processing.
The master log provides the consolidated scalar metrics calculated from each
accepted run.

---

## Consolidated performance metrics

The master logs may include social, safety, kinematic, efficiency, and
operational indicators such as:

- minimum and mean robot–human distance;
- fifth-percentile robot–human distance;
- time spent in intimate and personal proxemic zones;
- mission-completion time;
- trajectory length and trajectory efficiency;
- mean, maximum, and standard deviation of robot velocity;
- maximum and RMS numerical acceleration;
- accumulated stop time and number of stops;
- mission success, timeout, deadlock, or exclusion status.

---

## Data integrity rules

Before using the dataset:

1. Use `master_log_clean.csv` as the principal analysis table.
2. Verify that every selected row has a corresponding telemetry file.
3. Do not merge original and cleaned master logs into the same analysis.
4. Preserve the original filename because it encodes the experimental factors.
5. Treat failed, timed-out, deadlocked, and excluded runs according to the
   statistical protocol of the study.
6. Record any additional filtering operation in the analysis script or
   processing log.
7. Do not modify the original telemetry files directly; write transformed data
   to a separate output directory.


To reproduce an analysis, report at minimum:

- source master-log filename;
- method and scenario filters;
- run-inclusion and exclusion criteria;
- software and package versions;
- random seed for stochastic or Monte Carlo procedures;
- statistical test and multiple-comparison correction;
- effect-size estimator;
- date and version of the processed dataset.

---

## Authors

- Reinaldo Betancourt
- Ingrid Nicole Vásconez
- Viviana Moya
- William Chamorro
- Sandra Cano
- Marco Antonio Molina
- Juan Pablo Vásconez

---

## Repository context

**Project:** Socially Aware Navigation for Agricultural Mobile Robots in Narrow
Vineyard Corridors

**Platforms:** Unity, NavMesh, UDP telemetry, and MATLAB

**Research areas:** Agricultural robotics, human–robot interaction, social
navigation, digital twins, statistical analysis, and multicriteria decision
analysis.
