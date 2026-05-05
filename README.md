# Hybrid Social Navigation for Agricultural Robots in Digital Twins

This repository contains the implementation and experimental framework for hybrid social navigation of agricultural robots in digital twins.

The system combines NavMesh-based global planning, proxemic social supervision, anisotropic human-aware fields, Unity simulation, MATLAB telemetry analysis, and multicriteria evaluation using TOPSIS.

## Overview

Agricultural robots operating in shared crop corridors must navigate safely around human workers while maintaining mission efficiency and smooth motion. This project evaluates hybrid global-local navigation strategies in a simulated vineyard digital twin.

The proposed framework compares five navigation methods:

| Method | Description |
|---|---|
| M0 | NavMesh-only global navigation |
| M1 | Threshold-based stop supervisor |
| M2 | Hysteresis-based STOP/GO supervisor |
| M3 | Isotropic proxemic field navigation |
| M4 | Full anisotropic proxemic navigation with social supervision |

## Main Features

- Unity-based agricultural digital twin
- NavMesh global path planning
- Human-aware proxemic supervision
- Isotropic and anisotropic social fields
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

## Repository Status

This repository is under active development and is associated with an academic research manuscript on socially aware agricultural robot navigation.

## Authors

- R.J. Betancourt
- J.P. Vasconez

## License

To be defined.
