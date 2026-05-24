# Hybrid Social Navigation for Agricultural Robots in Vineyard Environments with Proxemic Fields and Social DWA Baseline

This repository contains the implementation and experimental framework for hybrid social navigation of agricultural robots in vineyard environments.

The system combines NavMesh-based global planning, proxemic social supervision, isotropic and anisotropic human-aware fields, a Social DWA external baseline, Unity simulation, MATLAB telemetry analysis, and multicriteria evaluation using TOPSIS.

## Overview

Agricultural robots operating in shared crop corridors must navigate safely around human workers while maintaining mission efficiency and smooth motion. This project evaluates hybrid global-local navigation strategies in a simulated vineyard environment.

The proposed framework compares six navigation methods:

| Method | Description |
|---|---|
| M0 | NavMesh-only global navigation |
| M1 | Threshold-based stop supervisor |
| M2 | Hysteresis-based STOP/GO supervisor |
| M3 | Isotropic proxemic field navigation |
| M4 | Full anisotropic proxemic navigation with social supervision |
| B1 | Social DWA baseline for local human-aware trajectory planning |

## Main Features

- Unity-based agricultural simulation environment
- Simulated vineyard environment
- NavMesh global path planning
- Human-aware proxemic supervision
- Isotropic and anisotropic social fields
- Social DWA local planning baseline
- Multi-human interaction scenarios
- UDP telemetry from Unity to MATLAB
- Statistical analysis of navigation performance
- Multicriteria ranking using TOPSIS

## Experimental Scenarios

The framework evaluates robot navigation under four representative agricultural human-robot interaction scenarios:

1. Frontal encounter
2. Lateral intrusion
3. Social following
4. Multi-human congestion

## Metrics

The experimental analysis includes:

- Minimum robot-human distance
- Time inside personal and intimate zones
- Mission completion time
- Number of stops
- Velocity variability
- Peak acceleration
- Mission success rate
- TOPSIS multicriteria score

## Social DWA Baseline

In addition to the proposed NavMesh-based social navigation methods, this repository includes **B1**, an external baseline based on **Social DWA** (*Social Dynamic Window Approach*).

Social DWA extends the classical Dynamic Window Approach by evaluating candidate linear and angular velocity commands according to geometric, kinematic, obstacle-avoidance, and social criteria. In this framework, B1 samples admissible velocity pairs \((v,\omega)\), simulates short-horizon local trajectories, and selects the command that best balances progress toward the local NavMesh waypoint, obstacle clearance, velocity efficiency, and avoidance of human proxemic zones.

This baseline allows the proposed methods M3 and M4 to be compared against a widely used local trajectory-planning approach adapted for human-aware agricultural navigation.

## Repository Status

This repository is under active development and is associated with an academic research manuscript on socially aware agricultural robot navigation in simulated vineyard environments.

## Authors

- R.J. Betancourt
- J.P. Vasconez

## License

To be defined.
