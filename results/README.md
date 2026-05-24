## Results Directory Structure

The `results/` directory stores all experimental data generated during the simulation runs. It contains the global master logs at the root level and the individual telemetry files organized by navigation method and scenario.

### Root files

| File | Description |
|---|---|
| `README.md` | General description of the results directory. |
| `master_log.csv` | Original consolidated experimental log. |
| `master_log_clean.csv` | Cleaned master log used for statistical processing. |
| `master_log_excel_es.csv` | Excel-compatible version using Spanish/European numeric formatting. |
| `master_log_excel_us.csv` | Excel-compatible version using US numeric formatting. |
| `master_log_clean_excel_es.csv` | Cleaned Excel-compatible version using Spanish/European numeric formatting. |
| `master_log_clean_excel_us.csv` | Cleaned Excel-compatible version using US numeric formatting. |

### Method folders

Each navigation method has its own folder:

| Folder | Method |
|---|---|
| `M0/` | NavMesh-only global navigation. |
| `M1/` | Threshold-based stop supervisor. |
| `M2/` | Hysteresis-based STOP/GO supervisor. |
| `M3/` | Isotropic proxemic field navigation. |
| `M4/` | Full anisotropic proxemic navigation with social supervision. |
| `B1/` | Social DWA baseline. |

### Scenario folders

Inside each method folder, the data are divided into four experimental scenarios:

| Folder | Scenario |
|---|---|
| `E1/` | Frontal encounter. |
| `E2/` | Lateral intrusion. |
| `E3/` | Social following. |
| `E4/` | Multi-human congestion. |

### Organization logic

The directory follows this structure:

```text
results/
├── master log files
├── M0/
│   └── E1/ E2/ E3/ E4/
├── M1/
│   └── E1/ E2/ E3/ E4/
├── M2/
│   └── E1/ E2/ E3/ E4/
├── M3/
│   └── E1/ E2/ E3/ E4/
├── M4/
│   └── E1/ E2/ E3/ E4/
└── B1/
    └── E1/ E2/ E3/ E4/
