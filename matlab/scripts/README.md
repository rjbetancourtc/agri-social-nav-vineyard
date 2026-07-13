# MATLAB Scripts

This folder contains the MATLAB scripts used for real-time telemetry acquisition,
experimental supervision, LiDAR-style visualization, metric extraction, dataset
generation, master-log reconstruction, statistical analysis, and post-processing
of the Unity-based agricultural robot digital twin.

The MATLAB environment operates as the experimental instrumentation and analysis
layer of the project:

**Hybrid Social Navigation for Agricultural Robots in Digital Twins**

Unity executes the vineyard simulation and navigation controllers, while MATLAB
receives UDP telemetry, monitors the mission, reconstructs trajectories, computes
per-run metrics, saves the experimental records, and prepares the numerical data
used in the statistical and multicriteria analyses.

---

## Purpose

The scripts in this directory support the complete experimental workflow:

- Receive real-time telemetry from Unity through UDP.
- Select the navigation method, HRI scenario, and run identifier.
- Monitor robot–human distance, robot velocity, and acceleration.
- Display the robot trajectory and a robot-centered 360° LiDAR-style map.
- Visualize human, plant, bush, crop-row, and generic-object detections.
- Detect mission start, arrival at point B, and return to point A.
- Apply automatic mission-completion fallback logic.
- Detect possible timeouts, communication loss, and out-of-order packets.
- Compute social, kinematic, trajectory, and operational metrics.
- Save telemetry, metrics, MATLAB data, and dashboard screenshots.
- Update the consolidated `master_log.csv`.
- Monitor completion of the full method–scenario experimental matrix.
- Support subsequent statistical analysis, TOPSIS, and Monte Carlo studies.

---

## Recommended Main File

The current recommended dashboard is:

```text
RobotExperimentDashboard_V7_4_Baselines_B1_B4.m
```

Run it from MATLAB with:

```matlab
RobotExperimentDashboard_V7_4_Baselines_B1_B4
```

This version integrates the complete set of in-house methods and external
baselines:

```text
M0, M1, M2, M3, M4, B1, B2, B3, B4
```

It supersedes the earlier dashboard versions for acquisition of the expanded
nine-method experimental matrix.

---

## Main Dashboard: V7.4 Baselines B1–B4

`RobotExperimentDashboard_V7_4_Baselines_B1_B4.m` is the primary graphical
application for experiment execution and telemetry recording.

### Main additions in V7.4

- Adds the external baselines `B2`, `B3`, and `B4`.
- Retains `B1`, introduced in V7.3.
- Supports all four scenarios `E1`–`E4`.
- Preserves the V7.2 correction for proxemic-zone time integration.
- Uses the actual time interval between consecutive samples.
- Saturates zone-occupancy times so they cannot exceed mission duration.
- Detects mission completion from Unity status code `2`.
- Includes a fallback completion detector based on return proximity to point A.
- Issues a warning when a run exceeds the configured mission timeout.
- Prevents accidental re-saving of the same run.
- Supports the inverted workflow in which Unity may already be running.
- Includes real-time LiDAR-style visualization and accumulated static mapping.
- Generates telemetry, metrics, MAT files, PNG captures, and a master log.

The dashboard is designed to work with:

```text
UnityToMatlabUDP_Lidar_SafeRayScan_BushHumanPoint.cs
```

---

## Navigation Methods

| Code | Method | Description |
|---|---|---|
| `M0` | NavMesh Only | Pure Unity NavMesh global navigation without a social local policy. |
| `M1` | Threshold Stop | Binary stopping policy activated by a robot–human distance threshold. |
| `M2` | Hysteresis Supervisor | STOP/GO supervisor with separate stopping and resumption conditions. |
| `M3` | Isotropic Proxemics | Continuous isotropic proxemic-field navigation. |
| `M4` | Full Anisotropic Hysteresis | Orientation-dependent anisotropic proxemic navigation with continuous velocity modulation. |
| `B1` | Social DWA | Social Dynamic Window Approach baseline. |
| `B2` | ORCA / RVO | Reciprocal velocity-obstacle collision-avoidance baseline. |
| `B3` | Social Force | Social Force Model baseline. |
| `B4` | CBF-Social-DWA | Control-Barrier-Function-constrained Social DWA baseline. |

---

## Experimental Scenarios

| Code | Scenario | Experimental objective |
|---|---|---|
| `E1` | Frontal Encounter | Evaluate the robot response during a direct frontal encounter with a worker. |
| `E2` | Lateral Intrusion | Evaluate the response when a human crosses or enters the robot trajectory laterally. |
| `E3` | Social Following | Evaluate navigation relative to a moving human agent. |
| `E4` | Multi-Human Congestion | Evaluate deadlock resistance and operational continuity in a narrow corridor occupied by several workers. |

The nominal experiment matrix contains:

```text
9 methods × 4 scenarios × 10 runs = 360 planned runs
```

---

## Dashboard Interface

The graphical dashboard provides controls for:

| Control | Function |
|---|---|
| `Method` | Selects one of the nine navigation methods. |
| `Scenario` | Selects one of the four HRI scenarios. |
| `Run ID` | Identifies the independent repetition, normally from 1 to 10. |
| `UDP Port` | Sets the local MATLAB UDP listening port. |
| `Trail [s]` | Sets the visible duration of the recent robot trajectory. |
| `Auto-stop [s]` | Stops acquisition after a period without incoming packets. |
| `Auto-save` | Saves the run automatically after mission completion is detected. |
| `START ACQUISITION` | Opens the UDP receiver and starts telemetry acquisition. |
| `STOP` | Stops acquisition without discarding the current data. |
| `SAVE DATA AND METRICS` | Saves telemetry, calculated metrics, and MATLAB data. |
| `DISCARD AND RESTART` | Discards the current run while retaining the run identifier. |
| `TEST UDP` | Performs a local UDP diagnostic. |
| `CAPTURE PNG` | Saves a 300 dpi image of the dashboard. |
| `CLEAR RUN` | Clears the current in-memory experiment data. |
| `NEXT RUN` | Advances the run sequence. |
| `VIEW MATRIX PROGRESS` | Reads the master log and reports completed method–scenario cells. |

---

## Real-Time Visualizations

The interface displays four synchronized panels:

1. **Robot–human distance**

   Shows the minimum detected robot–human distance and the proxemic thresholds:

   ```text
   Intimate zone: 0.45 m
   Personal zone: 1.20 m
   Social zone:   3.60 m
   ```

2. **Robot velocity**

   Displays the instantaneous robot linear velocity.

3. **Robot acceleration**

   Displays the numerical acceleration received from Unity.

4. **Robot-centered map**

   Displays the robot trajectory, heading, detected humans, crop rows, plants,
   bushes, and generic objects in Unity world coordinates.

The map uses a fixed metric scale centered on the robot and provides:

- 360° LiDAR-style range rings.
- Radial spokes and full-scan beams.
- Live scan returns.
- Short-duration human and robot trails.
- Accumulated static plant, bush, and row/object maps.
- Current robot position and minimum human distance.
- A visual radar sweep.

The LiDAR-style display affects visualization only. It does not modify the
telemetry or the calculated metrics.

---

## UDP Configuration

The default local UDP port is:

```text
55000
```

The base packet received from Unity contains ten comma-separated numerical
fields:

```text
t, dist, vel, acc, px, py, pz, status, method, scenario
```

| Position | Variable | Description |
|---:|---|---|
| 1 | `t` | Simulation time in seconds. |
| 2 | `dist` | Minimum robot–human distance in metres. |
| 3 | `vel` | Robot linear velocity in metres per second. |
| 4 | `acc` | Robot acceleration in metres per second squared. |
| 5 | `px` | Robot world-coordinate x position. |
| 6 | `py` | Robot world-coordinate y position. |
| 7 | `pz` | Robot world-coordinate z position. |
| 8 | `status` | Mission-state code. |
| 9 | `method` | Numerical method identifier. |
| 10 | `scenario` | Numerical scenario identifier. |

The extended packet can append:

```text
headingX, headingZ,
nHumans,  hx1, hz1, ...,
nPlants,  px1, pz1, ...,
nBushes,  bx1, bz1, ...,
nObjects, ox1, oz1, ...
```

Packets with fewer than the required base fields or with invalid numerical
values are rejected and counted as invalid packets. Packets whose timestamp is
older than the last accepted sample are counted as out-of-order packets.

---

## Mission-State Logic

The dashboard uses the following operational logic:

- Mission start is detected when robot velocity exceeds `0.05 m/s`.
- Arrival at B is logged when Unity reports mission status `1`.
- Mission completion is confirmed when Unity reports status `2`.
- A fallback completion condition is available after reaching B:
  - minimum elapsed time: `60 s`;
  - return distance to the starting point: less than `1.5 m`.
- A warning is issued when mission time exceeds `180 s`.
- Acquisition automatically stops after `5 s` without incoming packets by
  default.

These values can be changed in the configuration section of the main script.

---

## Calculated Metrics

For every accepted run, the dashboard computes metrics including:

| Metric | Description |
|---|---|
| `TotalTime_s` | Total recorded mission duration. |
| `PathLength_m` | Accumulated robot trajectory length in the x–z plane. |
| `MinDistance_m` | Minimum robot–human distance. |
| `MeanDistance_m` | Mean robot–human distance. |
| `MedianDistance_m` | Median robot–human distance. |
| `DistanceP05_m` | Fifth percentile of robot–human distance. |
| `DistanceIQR_m` | Interquartile range of robot–human distance. |
| `SocialTime_s` | Time below the social-zone threshold. |
| `PersonalTime_s` | Time below the personal-zone threshold. |
| `IntimateTime_s` | Time below the intimate-zone threshold. |
| `StopTime_s` | Time with robot velocity below the stop threshold. |
| `NumberOfStops` | Number of transitions into the stopped state. |
| `MeanVelocity_mps` | Mean robot velocity. |
| `MedianVelocity_mps` | Median robot velocity. |
| `MaxVelocity_mps` | Maximum robot velocity. |
| `VelocityStd_mps` | Standard deviation of robot velocity. |
| `MaxAcceleration_mps2` | Maximum absolute acceleration. |
| `AccRMS_mps2` | RMS acceleration. |
| `JerkMax_mps3` | Maximum absolute numerical jerk. |
| `JerkRMS_mps3` | RMS numerical jerk. |
| `PathEfficiency` | Current script output defined from path length and total time. |

Proxemic-zone and stop times are integrated using the actual interval between
consecutive telemetry samples rather than assuming a constant sample period.

---

## Generated Files

For each run, the dashboard creates a method–scenario directory:

```text
results/{METHOD}/{SCENARIO}/
```

Example:

```text
results/B2/E4/
```

The base filename follows:

```text
{METHOD}_{SCENARIO}_run{NN}
```

Example:

```text
B2_E4_run07
```

The following files are generated:

| File | Contents |
|---|---|
| `{METHOD}_{SCENARIO}_run{NN}.csv` | Sample-by-sample telemetry. |
| `{METHOD}_{SCENARIO}_run{NN}_metrics.csv` | Scalar metrics and acquisition metadata. |
| `{METHOD}_{SCENARIO}_run{NN}.mat` | MATLAB tables containing telemetry and metrics. |
| `{METHOD}_{SCENARIO}_run{NN}_dashboard_{TIMESTAMP}.png` | Optional 300 dpi dashboard capture. |
| `results/master_log.csv` | Consolidated table containing one row per saved run. |

The telemetry CSV includes:

```text
Time_s
Distance_m
Velocity_mps
Acceleration_mps2
PosX_m
PosY_m
PosZ_m
Method
Scenario
RunID
```

---

## Master Log

Every saved run is appended to:

```text
results/master_log.csv
```

The master log includes the method, scenario, run ID, timestamp, mission time,
trajectory length, distance statistics, velocity statistics, acceleration,
jerk, proxemic-zone occupancy, stop time, and number of stops.

The `VIEW MATRIX PROGRESS` function reads this file and counts the unique run
identifiers registered for each method–scenario combination.

---

## Recommended Execution Workflow

### 1. Set the repository root as the MATLAB working directory

```matlab
cd("path/to/agri-social-nav-vineyard")
addpath("matlab/scripts")
```

This is recommended because the dashboard uses the relative output directory:

```matlab
cfg.baseFolder = 'results';
```

### 2. Start the dashboard

```matlab
RobotExperimentDashboard_V7_4_Baselines_B1_B4
```

### 3. Configure the experiment

Select:

```text
Method
Scenario
Run ID
UDP port
```

Verify that the method and scenario selected in MATLAB correspond to the
controller configuration active in Unity.

### 4. Start acquisition

Press:

```text
START ACQUISITION
```

The dashboard supports the conventional workflow and the inverted workflow in
which Unity is already running.

### 5. Start or continue the Unity simulation

Confirm that packets are arriving and that the status panel reports valid
telemetry.

### 6. Monitor the mission

Check:

- robot–human distance;
- velocity and acceleration;
- robot trajectory;
- LiDAR-style detections;
- mission state;
- valid, invalid, and out-of-order packet counts.

### 7. Save the run

When mission completion is detected, the dashboard can save automatically.
Manual saving is also available through:

```text
SAVE DATA AND METRICS
```

### 8. Verify the output

Confirm that the telemetry, metrics, MAT file, and master-log entry were created
inside the correct method–scenario directory.

### 9. Advance to the next repetition

Use:

```text
NEXT RUN
```

or allow the dashboard to increment the run identifier after saving.

---

## MATLAB Requirements

A recent MATLAB release supporting the following functions is required:

```text
udpport
timer
uicontrol
exportgraphics
readtable
writetable
prctile
iqr
```

The Statistics and Machine Learning Toolbox is recommended for the statistical
functions used during metric computation and subsequent analysis.

The UDP port selected in MATLAB must be available and must match the destination
port configured in Unity.

---

## Folder Contents

| File | Description |
|---|---|
| `unity_telemetry_receive7.m` | MATLAB receiver for telemetry transmitted from Unity. |
| `RobotExperimentDashboard.m` | Initial experimental dashboard. |
| `RobotExperimentDashboard_V3.m` | Version 3 of the dashboard. |
| `RobotExperimentDashboard_V4.m` | Version 4 of the dashboard. |
| `RobotExperimentDashboard_V5.m` | Version 5 of the dashboard. |
| `RobotExperimentDashboard_V5_1.m` | Intermediate V5 update. |
| `RobotExperimentDashboard_V5_2.m` | Updated V5 dashboard. |
| `RobotExperimentDashboard_V6.m` | Version 6 of the dashboard. |
| `RobotExperimentDashboard_V7.m` | Version 7 with re-saving protection and inverted-flow support. |
| `RobotExperimentDashboard_V7_1.m` | Version 7.1 with stricter mission-completion logic. |
| `RobotExperimentDashboard_V7_2.m` | Version 7.2 with corrected zone-time integration, fallback completion, and timeout warning. |
| `RobotExperimentDashboard_V7_3.m` | Version 7.3 with the B1 Social DWA baseline. |
| `RobotExperimentDashboard_V7_4.m` | Earlier V7.4 development version. |
| `RobotExperimentDashboard_V7_4_Baselines_B1_B4.m` | **Current recommended dashboard with M0–M4 and B1–B4 support.** |
| `RobotExperimentDashboard_V7_5.m` | Additional development branch retained for comparison, when present. |
| `GuiaExperimentos200.m` | Guide or automation script for the original 200-run experiment matrix. |
| `GuiaExperimentos200_1.m` | Updated version of the original experiment guide. |
| `rebuild_master_log_v2.m` | Rebuilds the consolidated experimental master log. |
| `rebuild_master_log_clean.m` | Clean master-log reconstruction script. |
| `rebuild_master_log_clean_1.m` | Updated clean reconstruction script. |
| `analisis_estadistico_Q1.m` | Statistical analysis of the experimental dataset. |
| `GridLayout.m` | GUI layout helper or interface-support file. |
| `ManagerFactoryProducer.m` | MATLAB GUI or application-management helper. |
| `getExtendedErrorCallback.m` | Extended error-callback utility. |
| `onCleanup.m` | Cleanup utility used by MATLAB processes. |
| `PeerProperties.m` | GUI or application-support file. |
| `toolboxdir.m` | Utility related to MATLAB toolbox paths. |
| `mustBeNonempty.m` | Input-validation utility. |

Earlier dashboard versions are retained for traceability and comparison. New
experimental acquisition should use the recommended V7.4 B1–B4 dashboard unless
a specific legacy protocol must be reproduced.

---

## Data Integrity Recommendations

- Keep the original telemetry files unchanged.
- Use a unique method, scenario, and run-ID combination for every experiment.
- Check the Unity and MATLAB method/scenario identifiers before acquisition.
- Do not overwrite a run unless the replacement has been explicitly validated.
- Inspect invalid and out-of-order packet counters after every run.
- Verify mission completion before accepting a run.
- Record timeouts, deadlocks, interrupted runs, and exclusions separately.
- Rebuild or clean the master log only from verified source files.
- Keep statistical analysis outputs separate from raw telemetry.
- Preserve the dashboard version used to acquire each experimental batch.

---

## Implementation Note

The local `TEST UDP` control is intended as a communication diagnostic. The main
receiver validates the complete ten-field base packet, so the most reliable
end-to-end test is a packet generated by the compatible Unity telemetry script.

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

## Project Context

**Project:** Socially aware navigation for agricultural mobile robots in narrow
vineyard corridors.

**Architecture:** Unity vineyard digital twin → NavMesh global planner → social
navigation controller → UDP telemetry → MATLAB dashboard → experimental dataset
→ statistical and multicriteria analysis.

**Research areas:** Agricultural robotics, autonomous navigation, human–robot
interaction, proxemics, digital twins, LiDAR-style visualization, statistical
inference, and multicriteria decision analysis.
