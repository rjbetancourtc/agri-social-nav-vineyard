# Unity Scripts

This folder contains the C# scripts used by the Unity agricultural digital twin
for global navigation, socially aware local control, human-agent simulation,
trajectory visualization, and telemetry exchange with MATLAB.

The main experimental controller used to execute the navigation trials is:

```text
RobotSocialNavController.cs
```

This controller implements the complete nine-method comparison used in the
experimental campaign:

```text
M0, M1, M2, M3, M4, B1, B2, B3, B4
```

---

## Project Role

The Unity layer performs the real-time simulation of the vineyard environment,
the agricultural mobile robot, the human agents, and the navigation policies.

Its main responsibilities are:

- Generate the global route through Unity NavMesh.
- Detect nearby human agents.
- Apply the selected social-navigation method.
- Execute the common `A → B → A` patrol mission.
- Maintain robot motion inside the baked NavMesh.
- Expose method, scenario, motion, and mission-state information to the
  telemetry system.
- Support synchronized telemetry acquisition and analysis in MATLAB.

The navigation architecture is hybrid:

```text
NavMesh global route
        ↓
RobotSocialNavController
        ↓
Selected local social-navigation method
        ↓
Robot motion
        ↓
Unity telemetry component
        ↓
MATLAB experimental dashboard
```

---

## Recommended Main Controller

```text
RobotSocialNavController.cs
```

`RobotSocialNavController` is the principal controller used to run the
experiments. It requires a Unity `NavMeshAgent` component and provides a common
interface for all navigation methods.

The controller performs:

- patrol switching between `Point_A_R` and `Point_B_R`;
- human detection using a configurable `LayerMask`;
- nearest-human tracking;
- numerical estimation of human velocity;
- global-route extraction from NavMesh;
- local waypoint generation;
- proxemic-zone evaluation;
- method-specific velocity and steering control;
- debug visualization with Gizmos;
- support for the nine experimental method identifiers.

---

## Experimental Methods

The `ExperimentMode` enumeration defines the numerical method identifiers sent
to the experimental pipeline.

| Numeric ID | Code | Controller mode | Description |
|---:|---|---|---|
| 0 | `M0` | `M0_NavMeshOnly` | Pure NavMesh global navigation without social modulation. |
| 1 | `M1` | `M1_ThresholdStop` | Stops when the nearest human enters the personal zone. |
| 2 | `M2` | `M2_HysteresisSupervisor` | STOP/GO supervisor using personal-zone stopping and social-zone resumption. |
| 3 | `M3` | `M3_IsotropicProxemics` | Continuous isotropic repulsive field combined with the global NavMesh direction. |
| 4 | `M4` | `M4_FullAnisotropicHysteresis` | Orientation-dependent anisotropic proxemic field with continuous local avoidance and speed modulation. |
| 5 | `B1` | `B1_SocialDWA` | External Social Dynamic Window Approach controlled through `SocialDWAController`. |
| 6 | `B2` | `B2_ORCA_RVO` | Internal ORCA/RVO-inspired reciprocal velocity-obstacle baseline. |
| 7 | `B3` | `B3_SocialForce` | Internal Social Force Model baseline. |
| 8 | `B4` | `B4_CBF_SocialDWA` | Dynamic-window sampling with a Control Barrier Function safety filter. |

The numeric order must remain consistent with the MATLAB dashboard and the
telemetry method identifiers.

---

## Navigation-Method Details

### M0 — NavMesh Only

Uses the Unity `NavMeshAgent` directly:

```text
global target → NavMesh path → robot motion
```

No human-distance constraint or social behavior is applied.

### M1 — Threshold Stop

The robot follows the NavMesh route until the nearest human is inside the
personal zone:

```text
d ≤ personalZone  → STOP
d > personalZone  → GO
```

This binary policy can become blocked when a worker remains stationary in a
narrow corridor.

### M2 — Hysteresis Supervisor

Uses different thresholds for stopping and resuming:

```text
STOP when d ≤ personalZone
GO   when d ≥ socialZone
```

The hysteresis reduces rapid STOP/GO switching but can still deadlock in
persistent congestion.

### M3 — Isotropic Proxemics

Combines the global route direction with a radial avoidance vector:

```text
u = normalize(u_global + u_isotropic)
```

The repulsive magnitude increases when the robot enters the personal or
intimate zone.

### M4 — Full Anisotropic Proxemic Navigation

Uses human position and heading to construct an orientation-dependent
anisotropic field.

The field is evaluated from:

```text
distance to the human
relative bearing
human heading
sigmaX
sigmaY
avoidance gain
```

The final direction is:

```text
u_final = normalize(u_global + u_anisotropic)
```

The robot speed is then modulated according to the nearest-human distance.

### B1 — Social DWA

B1 delegates the local motion command to an attached:

```text
SocialDWAController
```

`RobotSocialNavController` remains responsible for:

- selecting the current patrol target;
- switching between A and B;
- detecting target overshoot;
- forwarding the current goal;
- detecting nearby `SocialDWAHuman` components;
- disabling direct NavMeshAgent position and rotation updates.

### B2 — ORCA / RVO

B2 computes a preferred velocity from the NavMesh route and modifies it when a
velocity-obstacle risk is detected.

The implementation includes:

- human-radius and safety-margin terms;
- neighbor-distance filtering;
- time-to-collision prediction;
- closest-approach estimation;
- lateral passing-side selection;
- emergency repulsion;
- estimated human velocity.

This implementation is ORCA/RVO-inspired and is integrated directly into the
common controller.

### B3 — Social Force Model

B3 computes a relaxation force toward the desired velocity and adds repulsive
forces from nearby humans:

```text
F = F_goal + Σ F_human
```

The force increases inside the personal and intimate regions and is converted
into a target velocity executed through NavMesh.

### B4 — CBF-Social-DWA

B4 performs local dynamic-window sampling over:

```text
linear velocity v
angular velocity omega
```

Each candidate is evaluated using:

- goal heading;
- distance to the local waypoint;
- NavMesh feasibility;
- human clearance;
- velocity preference;
- social-zone penalties.

A Control Barrier Function filter then modifies the nominal candidate to
preserve a configurable safe distance.

B4 uses manual unicycle integration and therefore temporarily disables direct
NavMeshAgent position and rotation updates.

---

## Social Modes

The controller also exposes a higher-level `SocialMode`:

| Mode | Function |
|---|---|
| `Patrol` | Follow the A–B patrol route. |
| `AvoidOnly` | Combine the global route with social avoidance. |
| `ApproachNearestHuman` | Generate a social reference point relative to the nearest human. |
| `FollowNearestHuman` | Track a human-relative reference point. |
| `Auto` | Use the global patrol behavior with the selected experimental controller. |

For the standard experimental campaign, `Auto` or `Patrol` should normally be
used together with the required `ExperimentMode`.

---

## Proxemic Parameters

Default proxemic thresholds:

| Zone | Parameter | Default |
|---|---|---:|
| Intimate | `intimateZone` | `0.45 m` |
| Personal | `personalZone` | `1.20 m` |
| Social | `socialZone` | `3.60 m` |

Default robot speeds:

| Parameter | Default | Meaning |
|---|---:|---|
| `nominalSpeed` | `1.00 m/s` | Normal patrol speed. |
| `slowSpeed` | `0.45 m/s` | Reduced speed near humans. |
| `stopSpeed` | `0.00 m/s` | Complete stop command. |

These values should be kept consistent with the MATLAB analysis and the
experimental protocol.

---

## Required Unity Components

The robot GameObject must include:

```text
NavMeshAgent
RobotSocialNavController
```

Depending on the selected method, it may also require:

```text
SocialDWAController       required for B1
telemetry component       required for Unity–MATLAB acquisition
RobotTelemetryPanel       optional runtime monitoring
TrajectoryTrail           optional path visualization
```

Human GameObjects should include:

```text
Collider
HumanWalker               when scripted walking is required
HumanKinematics           for heading/kinematic information
SocialDWAHuman            required for B1 detection
```

The human colliders must belong to the layer selected by `humanMask`.

---

## Inspector Configuration

### RobotSocialNavController

Assign the following references in the Unity Inspector:

| Field | Required configuration |
|---|---|
| `Mode` | Usually `Auto` for the experimental campaign. |
| `Experiment Mode` | Select the required method M0–M4 or B1–B4. |
| `Point A R` | Transform representing patrol endpoint A. |
| `Point B R` | Transform representing patrol endpoint B. |
| `Human Mask` | LayerMask containing all human colliders. |
| `Detection Radius` | Radius used to search for human agents. |
| `Switch Distance` | Distance used to change the patrol endpoint. |
| `Nominal Speed` | Reference robot speed. |
| `Intimate Zone` | Critical close-contact threshold. |
| `Personal Zone` | Active social-control threshold. |
| `Social Zone` | Monitoring and resumption threshold. |

### B1 configuration

For B1:

1. Add `SocialDWAController` to the robot.
2. Add `SocialDWAHuman` to every relevant human agent.
3. Assign `Point_A_R` and `Point_B_R`.
4. Confirm that the human layer is included in `humanMask`.
5. Select `B1_SocialDWA`.

The main controller automatically enables and disables
`SocialDWAController` according to the selected experimental mode.

### B4 configuration

For B4, review:

```text
b4PredictionTime
b4SimulationStep
b4VelocitySamples
b4OmegaSamples
b4OmegaMax
b4CbfSafeDistance
b4CbfGamma
b4CbfSteerGain
b4CbfInfluenceDistance
manualMaxLinearAcceleration
manualMaxAngularAcceleration
manualNavMeshSnapRadius
```

B4 uses manual motion, but every new position is projected back onto the
NavMesh whenever possible.

---

## Scene Preparation

Before running experiments:

1. Create and bake the vineyard NavMesh.
2. Place the robot on a valid NavMesh polygon.
3. Create `Point_A_R` and `Point_B_R`.
4. Assign both patrol-point transforms to the controller.
5. Place all human colliders on the configured human layer.
6. Attach `HumanKinematics` where human heading is required.
7. Configure the Unity telemetry component.
8. Confirm that the Unity method and scenario IDs match MATLAB.
9. Save the scene before starting the experimental batch.

The controller reports a warning when the robot does not start exactly on the
NavMesh.

---

## A–B–A Mission Logic

The default patrol target is `Point_B_R`.

When the robot enters the configured `switchDistance`, the target changes:

```text
A → B
B → A
```

For B1, the controller also detects target overshoot. This prevents a fast
manual controller from missing the switching radius and continuing beyond the
goal.

The mission-status component used for telemetry should report:

```text
0  mission in progress toward B
1  point B reached; returning to A
2  mission completed at A
```

The telemetry script, not `RobotSocialNavController`, is responsible for
sending these status values to MATLAB.

---

## Human Detection and Kinematics

Humans are detected using:

```csharp
Physics.OverlapSphereNonAlloc(...)
```

The detector:

- uses a fixed buffer of 64 colliders;
- ignores trigger colliders;
- removes repeated references to the same root transform;
- identifies the nearest human;
- optionally uses `Collider.ClosestPoint`;
- estimates human velocity from frame-to-frame position changes;
- accesses `HumanKinematics` when available.

The public properties:

```csharp
NearestHuman
NearestHumanDistance
```

can be used by telemetry or visualization components.

---

## Global and Local Navigation

The controller extracts a local waypoint from the global NavMesh route.

The process is:

```text
global patrol target
        ↓
NavMesh.CalculatePath
        ↓
look-ahead point along path corners
        ↓
NavMesh.SamplePosition
        ↓
local social controller
```

This avoids replacing global path planning with purely reactive motion.

M0–M4, B2, and B3 use the `NavMeshAgent` for movement.

B1 and B4 are manual-motion modes:

```text
B1 → SocialDWAController
B4 → internal unicycle controller
```

When switching back from a manual mode, the agent is warped to the current
position and normal NavMesh updates are restored.

---

## Debug Visualization

When `drawDebug` is enabled, the controller draws:

- human-detection radius;
- intimate, personal, and social zones;
- global target vector;
- avoidance vector;
- final motion direction;
- line to the nearest human;
- method-specific velocity or force vectors.

These Gizmos are intended for development and validation and do not modify the
navigation result.

---

## Main Scripts

| Script | Purpose |
|---|---|
| `RobotSocialNavController.cs` | **Primary nine-method navigation controller used for the experimental runs.** |
| `RobotNavPatrol.cs` | Basic NavMesh patrol/navigation behavior. |
| `UnityToMatlabTCP.cs` | Legacy or alternative telemetry communication component. |
| `UnityToMatlabUDP_Lidar_SafeRayScan_BushHumanPoint.cs` | UDP telemetry and extended LiDAR-style data source used with the current MATLAB dashboard, when present. |
| `SocialDWAController.cs` | Local Dynamic Window Approach controller required by B1. |
| `SocialDWAHuman.cs` | Human representation used by the B1 Social DWA controller. |
| `RobotTelemetryPanel.cs` | Runtime telemetry and experiment-monitoring interface. |
| `HumanWalker.cs` | Scripted human walking behavior. |
| `HumanKinematics.cs` | Human heading and kinematic-state estimation. |
| `SocialZoneDiana.cs` | Proxemic-zone visualization or computation. |
| `TrajectoryTrail.cs` | Robot or human trajectory visualization. |

Only scripts actually present in the repository should remain in the final
table. Remove rows corresponding to legacy files that are no longer included.

---

## Recommended Experimental Workflow

1. Open the vineyard scene.
2. Confirm that the NavMesh is baked.
3. Select the required `ExperimentMode`.
4. Select the corresponding method in the MATLAB dashboard.
5. Select the same scenario ID in Unity and MATLAB.
6. Set the run ID in MATLAB.
7. Start MATLAB acquisition.
8. Start or reset the Unity simulation.
9. Monitor robot motion and telemetry.
10. Confirm completion of the full `A → B → A` mission.
11. Save the telemetry and metrics.
12. Reset the scene before the next independent run.

---

## Compatibility with MATLAB

The recommended MATLAB acquisition file is:

```text
RobotExperimentDashboard_V7_4_Baselines_B1_B4.m
```

The method identifiers must be synchronized:

| Unity mode | MATLAB code |
|---|---|
| `M0_NavMeshOnly` | `M0` |
| `M1_ThresholdStop` | `M1` |
| `M2_HysteresisSupervisor` | `M2` |
| `M3_IsotropicProxemics` | `M3` |
| `M4_FullAnisotropicHysteresis` | `M4` |
| `B1_SocialDWA` | `B1` |
| `B2_ORCA_RVO` | `B2` |
| `B3_SocialForce` | `B3` |
| `B4_CBF_SocialDWA` | `B4` |

A mismatch between the Unity mode and MATLAB selection can place a valid
telemetry file in the wrong experimental cell. Always verify both interfaces
before starting a run.

---

## Data-Integrity Recommendations

- Use one unique method–scenario–run combination for every experiment.
- Reset the scene before every independent repetition.
- Keep all physical and controller parameters fixed within a comparison batch.
- Record any modified parameter set.
- Confirm that the robot begins on the NavMesh.
- Confirm that the correct human layer is active.
- Do not change `ExperimentMode` during a run.
- Validate mission completion before accepting the experiment.
- Preserve the exact controller version used for each dataset release.
- Keep backup and temporary scripts outside the tracked production directory.

---

## Known Implementation Considerations

- B1 requires external `SocialDWAController` and `SocialDWAHuman` components.
- B1 and B4 disable normal NavMeshAgent position updates while active.
- B2 is an ORCA/RVO-inspired implementation, not a direct import of the
  official RVO2 library.
- B4 uses sampled DWA candidates followed by a CBF-inspired velocity and
  steering filter.
- Human velocity is numerically estimated from Unity frame updates.
- The controller computes avoidance from detected humans; undetected or
  incorrectly layered humans do not affect navigation.
- The controller does not transmit telemetry by itself. A separate telemetry
  script must read the controller and robot state and send them to MATLAB.

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

