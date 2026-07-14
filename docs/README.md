# Supporting Documents and References

This directory contains the seven numbered technical documents that support the
statistical, multicriteria, and bibliographic analysis of the original
Unity–MATLAB agricultural robot navigation study.

The official document series consists **only of Documents 01 through 07**.

No additional paper or manuscript is included in this directory.

---

## Directory Scope

```text
references/
├── README.md
├── 01_Estadistica_Descriptiva.pdf
├── 02_Kruskal_Wallis.pdf
├── 03_MannWhitney_Holm_Cliff.pdf
├── 04_Fisher_ChiCuadrado.pdf
├── 05_TOPSIS_Multicriterio.pdf
├── 06_MonteCarlo_Sensibilidad.pdf
└── 07_Estado_del_Arte_y_Referencias.pdf
```

---

## Official Numbered Documents

| No. | File | Title and purpose |
|---:|---|---|
| 01 | `01_Estadistica_Descriptiva.pdf` | **Exhaustive Descriptive Statistics.** Documents the descriptive analysis of the experimental runs, including measures of central tendency, dispersion, percentiles, proxemic-zone times, mission time, trajectory length, velocity, acceleration, stops, and trajectory efficiency. |
| 02 | `02_Kruskal_Wallis.pdf` | **Kruskal–Wallis Omnibus Test.** Presents the theoretical basis, rank construction, test statistic, critical-value comparison, and consolidated results for the scalar navigation metrics. |
| 03 | `03_MannWhitney_Holm_Cliff.pdf` | **Pairwise Mann–Whitney U Comparisons.** Includes raw and adjusted p-values, Holm–Bonferroni correction, Cliff’s delta effect size, and focused comparisons involving M4. |
| 04 | `04_Fisher_ChiCuadrado.pdf` | **Success-Rate Analysis.** Documents Fisher’s exact test, the global chi-square test of independence, and Cramér’s V for method success and failure counts. |
| 05 | `05_TOPSIS_Multicriterio.pdf` | **TOPSIS Multicriteria Analysis.** Derives the complete TOPSIS procedure, criterion weighting, ideal solutions, closeness coefficients, and rankings with and without failure penalties. |
| 06 | `06_MonteCarlo_Sensibilidad.pdf` | **Monte Carlo Weight-Sensitivity Analysis.** Evaluates the robustness of the TOPSIS ranking under random perturbations of the multicriteria weights. |
| 07 | `07_Estado_del_Arte_y_Referencias.pdf` | **State of the Art and Complete References.** Reviews proxemics, classical and reactive planning, social navigation, agricultural robotics, functional safety, digital twins, Sim2Real, non-parametric statistics, and multicriteria decision analysis. |

---

## Document Series

The numbered collection follows this analytical sequence:

```text
01  Descriptive characterization
        ↓
02  Omnibus comparison among methods
        ↓
03  Pairwise post-hoc comparisons and effect sizes
        ↓
04  Analysis of categorical mission success
        ↓
05  Multicriteria ranking
        ↓
06  Ranking robustness and sensitivity
        ↓
07  Theoretical framework, state of the art, and references
```

Together, Documents 01–07 provide the calculation traceability and theoretical
support for the original experimental analysis.

---

## Original Analysis Context

The numbered Documents 01–07 correspond to the original analysis phase based on:

```text
184 experimental runs
5 navigation methods
4 experimental scenarios
15 scalar metrics
```

The five original methods are:

| Code | Method |
|---|---|
| `M0` | NavMesh Only |
| `M1` | Threshold Stop |
| `M2` | Hysteresis Supervisor |
| `M3` | Continuous proxemic navigation |
| `M4` | Full anisotropic proxemic navigation |

The four experimental scenarios are:

| Code | Scenario |
|---|---|
| `E1` | Frontal encounter |
| `E2` | Lateral intrusion |
| `E3` | Social following |
| `E4` | Multi-human congestion |

Later repository branches may include additional external baselines. Those
additions do not change the numbering or purpose of the seven supporting
documents in this directory.

---

## Recommended Reading Order

For complete technical traceability, read the documents in numerical order:

1. Start with `01_Estadistica_Descriptiva.pdf`.
2. Continue with the omnibus comparison in `02_Kruskal_Wallis.pdf`.
3. Review pairwise differences and effect sizes in
   `03_MannWhitney_Holm_Cliff.pdf`.
4. Examine success and failure proportions in
   `04_Fisher_ChiCuadrado.pdf`.
5. Review the engineering ranking in `05_TOPSIS_Multicriterio.pdf`.
6. Verify ranking robustness in `06_MonteCarlo_Sensibilidad.pdf`.
7. Consult the theoretical framework and bibliography in
   `07_Estado_del_Arte_y_Referencias.pdf`.

---

## Relationship Between the Documents

### Documents 01–04: Statistical evidence

These documents establish the quantitative basis of the study:

```text
continuous metrics
    → descriptive statistics
    → omnibus testing
    → pairwise post-hoc testing
    → effect sizes

categorical outcomes
    → success/failure tables
    → Fisher exact tests
    → chi-square test
    → Cramér’s V
```

### Documents 05–06: Engineering decision analysis

These documents transform the statistical indicators into a multicriteria
engineering assessment:

```text
performance matrix
    → criterion normalization
    → criterion weighting
    → TOPSIS ranking
    → Monte Carlo sensitivity analysis
```

### Document 07: Scientific foundation

Document 07 provides the theoretical and bibliographic support for:

- human proxemics;
- social-navigation models;
- NavMesh and classical planning;
- DWA, TEB, ORCA, and reactive methods;
- anisotropic Gaussian fields;
- agricultural robot safety;
- digital twins and Sim2Real;
- non-parametric inference;
- TOPSIS and sensitivity analysis.

---

## Naming Convention

The numbered files should preserve the following format:

```text
NN_Descriptive_Title.pdf
```

where:

- `NN` is a two-digit number from `01` to `07`;
- the number defines the reading order;
- underscores separate words;
- no additional numbered files should be added unless the document series is
  formally revised.

Recommended names:

```text
01_Estadistica_Descriptiva.pdf
02_Kruskal_Wallis.pdf
03_MannWhitney_Holm_Cliff.pdf
04_Fisher_ChiCuadrado.pdf
05_TOPSIS_Multicriterio.pdf
06_MonteCarlo_Sensibilidad.pdf
07_Estado_del_Arte_y_Referencias.pdf
```




## Authors

- Reinaldo Betancourt
- Ingrid Nicole Vásconez
- Viviana Moya
- William Chamorro
- Sandra Cano
- Marco Antonio Molina
- Juan Pablo Vásconez

