# Diccionario de datos

Autores: Reinaldo Betancourt · Ingrid Nicole Vásconez · Viviana Moya · William Chamorro · Sandra Cano · Marco Antonio Molina · Juan Pablo Vásconez

CSV: UTF-8, coma, cabecera; valores ausentes no se sustituyen por cero. JSON conserva parámetros y procedencia. No se incluyen MAT duplicados.

RUN_ENDPOINTS.csv: 360 filas, una por intento. Identity = EVAL_20261019_112257_M<método>_E<escenario>_R<repetición>_S<semilla>_A<intento>. CampaignID, Method (0–8), Scenario (1–4), RunID, Seed y AttemptID son identificadores. Métodos 0–4 = M0–M4; 5–8 = B1–B4. result: 2 SUCCESS, 3 TIMEOUT. TIMEOUT es terminal, no pendiente.

Duration_s: duración observada RUN–END, segundos. DurationCompleted_s: solo SUCCESS; TIMEOUT no se imputa. LengthXZ_m: longitud observada XZ, metros; una ruta parcial no equivale a una misión eficiente. MinGroundTruthXZ_m: distancia mínima entre centros, metros. Exposure045_s, Exposure12_s y Exposure36_s: exposición observada bajo 0,45/1,20/3,60 m, segundos. MissingGroundTruth_s y NoHuman_s: segundos sin distancia disponible o sin humanos. MaxFiltered_mps, MaxRealized_mps, MeanFilteredSample_mps y MeanFilteredTime_mps: velocidades en m/s; velocidad objetivo y realizada no son equivalentes. StopTime002_s, StopTime005_s, StopTime010_s: segundos de parada a umbrales de 0,02/0,05/0,10 m/s; se conservan los valores no disponibles.

Failure = result distinto de 2. IntimateEvent = mínimo bajo 0,45 m. MissionUtility = 0 para no éxito; para éxito max(0,1−Duration_s/180). SafetyUtility = clip((MinGroundTruthXZ_m−0,45)/(1,20−0,45),0,1). PersonalExposureFraction = Exposure12_s/(Duration_s−MissingGroundTruth_s). Son adimensionales y no representan garantías físicas ni preferencias universales.

packetGaps, GapCount y ReceiverPacketGaps: pérdidas/indicadores conservados de la cadena correspondiente. TimeInvalid, OriginalMetricTimeInvalid, ZeroDurationPoseIntervals y TechnicalMissing: calidad temporal/técnica. metricVersion, SourceMetricVersion, Version, protocol, receiverVersion y EndpointSource: versión/procedencia. samples y Samples: conteos originales de sus cadenas. terminalHash identifica el evento terminal. started conserva la marca original de inicio. Block identifica escenario/semilla y no demuestra emparejamiento. FailureIncludingTechnicalSensitivity es el indicador conservador definido por el código; no se borran sus denominadores.

QUALITY_PER_RUN.csv: 360 identidades con cobertura de secuencias del emisor, correspondencia de entradas recibidas, intervalos temporales nulos, decisiones de adjudicación y hashes de procedencia. OriginalStrictQualityFlag y OriginalStrictQualityReason conservan las 115 alertas originales. Los hashes del corpus excluido identifican evidencia, no son accesos públicos.

Benchmark: 180 asignaciones E1/E4, subconjunto de identidades de las 360 misiones, no otras 180 misiones que deban sumarse. Campos _ms_mean, _ms_p50, _ms_p95 y _ms_p99 en milisegundos. Detection, Kernel, SensorToCommandAPI y Observer son fronteras distintas. Cycles, WarmupCycles, GC*_collections y ComputeDeadlineMisses son conteos; EffectiveHz es Hz; Result es el resultado terminal. La tabla agrega medias por asignación, no un percentil global de ciclos. PROCESS_CPU_SUMMARY.csv: porcentajes de un núcleo/capacidad total y RSS en bytes; mide sesiones del receptor, no CPU por controlador.

CONTROLLER_CONFIGURATIONS.json: parámetros serializados por corrida; nombres y unidades deben interpretarse con el código congelado, no inferirse por magnitud. HUMAN_INITIAL_CONDITIONS.csv: Identity/Method/Scenario/Human identifican corrida y humano; x/z son metros, Speed es m/s, headingX/headingZ son componentes adimensionales de dirección, seed es la semilla humana y target identifica el destino. No se afirma emparejamiento entre métodos.

El archivo ENDPOINT_COLUMNS.csv enumera todos los campos, tipos y ausencias. La especificación efectiva está en config/analysis.json; el orden de contrastes y generador aleatorio se conserva para reproducir las realizaciones estocásticas originales.
