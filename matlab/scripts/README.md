# MATLAB Scripts

This folder contains the MATLAB scripts used for telemetry acquisition, experimental monitoring, data reconstruction, statistical analysis and post-processing of the Unity-based agricultural robot digital twin.

The MATLAB environment is used together with Unity to receive robot/human telemetry, visualize experiments, compute navigation metrics and generate the numerical basis for the statistical analysis of the study.

---

## Purpose

The scripts in this folder support the experimental workflow for the project:

**Hybrid Social Navigation for Agricultural Robots in Digital Twins**

Main functions:

- Receive telemetry from Unity.
- Monitor robot and human trajectories.
- Visualize experimental scenarios.
- Reconstruct master experimental logs.
- Compute navigation metrics.
- Support statistical post-processing.
- Prepare data for descriptive statistics, non-parametric tests, TOPSIS and Monte Carlo analysis.

---

## Folder Contents

| File | Description |
|---|---|
| `unity_telemetry_receive7.m` | MATLAB receiver for telemetry data sent from Unity. |
| `RobotExperimentDashboard.m` | Initial experimental dashboard for monitoring robot experiments. |
| `RobotExperimentDashboard_V3.m` | Version 3 of the experimental dashboard. |
| `RobotExperimentDashboard_V4.m` | Version 4 of the experimental dashboard. |
| `RobotExperimentDashboard_V5.m` | Version 5 of the experimental dashboard. |
| `RobotExperimentDashboard_V5_1.m` | Intermediate update of version 5. |
| `RobotExperimentDashboard_V5_2.m` | Updated version of the V5 dashboard. |
| `RobotExperimentDashboard_V6.m` | Version 6 of the experimental dashboard. |
| `RobotExperimentDashboard_V7.m` | Version 7 of the experimental dashboard. |
| `RobotExperimentDashboard_V7_1.m` | Updated version 7.1 of the dashboard. |
| `RobotExperimentDashboard_V7_2.m` | Updated version 7.2 of the dashboard. |
| `RobotExperimentDashboard_V7_3.m` | Updated version 7.3 of the dashboard. |
| `RobotExperimentDashboard_V7_4.m` | Updated version 7.4 of the dashboard. |
| `RobotExperimentDashboard_V7_5.m` | Latest dashboard version for experiment execution and analysis. |
| `GuiaExperimentos200.m` | Experiment guide or automation script for 200 planned simulations. |
| `GuiaExperimentos200_1.m` | Updated version of the 200-experiment guide. |
| `rebuild_master_log_v2.m` | Script for rebuilding the master experimental log. |
| `rebuild_master_log_clean.m` | Clean version of the master log reconstruction script. |
| `rebuild_master_log_clean_1.m` | Updated clean version of the master log reconstruction script. |
| `analisis_estadistico_Q1.m` | Statistical analysis script for the experimental dataset. |
| `GridLayout.m` | GUI layout helper or interface utility. |
| `ManagerFactoryProducer.m` | MATLAB GUI/application management helper file. |
| `getExtendedErrorCallback.m` | Error callback utility. |
| `onCleanup.m` | Cleanup utility used by MATLAB processes. |
| `PeerProperties.m` | GUI/application support file. |
| `toolboxdir.m` | Utility function related to MATLAB toolbox paths. |
| `mustBeNonempty.m` | Validation utility for non-empty input arguments. |

---

## Recommended Main File

The recommended main dashboard version is:

```text
RobotExperimentDashboard_V7_5.m
