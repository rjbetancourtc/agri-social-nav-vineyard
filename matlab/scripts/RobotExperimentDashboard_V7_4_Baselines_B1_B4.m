function RobotExperimentDashboard_V7_4_Baselines_B1_B4
% =========================================================================
% EXPERIMENTAL DASHBOARD V7.4 - SOCIAL AGRICULTURAL ROBOT
% =========================================================================
% V7.4 UPDATE:
%   - Adds external baseline methods B2, B3 and B4 for E1-E4 acquisition.
%   - B1 = Social DWA, B2 = ORCA/RVO, B3 = Social Force, B4 = CBF-Social-DWA.
%
% V7.3 UPDATE:
%   - Adds B1 - Social DWA as external baseline method for E1-E4 acquisition.
%
% V7.2 UPDATES:
%   - FIX T_personal: uses actual dt between samples (not constant median)
%   - SATURATION: zone times limited to TotalTime_s
%   - FALLBACK: detects completion by proximity to A if Unity does not send status=2
%   - TIMEOUT: warning if run exceeds 180s (possible issue)
%
% V7.1 improvements:
%   - Mission only ends with status=2 (return to A confirmed)
%
% V7 improvements:
%   - RE-SAVING LOCK
%   - INVERTED FLOW SUPPORT (Play in Unity first)
%
% Compatible with UnityToMatlabUDP_Lidar_SafeRayScan_BushHumanPoint.cs
% =========================================================================

clearvars -except ans; clc;

try, stop(timerfindall); delete(timerfindall); catch, end
try, delete(udpportfind);  catch, end

% =========================================================================
% CONFIGURATION
% =========================================================================
cfg.port = 55000;
cfg.baseFolder = 'results';

cfg.methodCodes = {'M0','M1','M2','M3','M4','B1','B2','B3','B4'};
cfg.methodNames = { ...
    'M0 - NavMesh Only', ...
    'M1 - Threshold Stop', ...
    'M2 - Hysteresis Supervisor', ...
    'M3 - Isotropic Proxemics', ...
    'M4 - Full Anisotropic Hysteresis', ...
    'B1 - Social DWA', ...
    'B2 - ORCA / RVO', ...
    'B3 - Social Force', ...
    'B4 - CBF-Social-DWA'};

cfg.scenarioCodes = {'E1','E2','E3','E4'};
cfg.scenarioNames = { ...
    'E1 - Frontal Encounter', ...
    'E2 - Lateral Intrusion', ...
    'E3 - Social Following', ...
    'E4 - Multi-Human Congestion'};

cfg.intimateZone = 0.45;
cfg.personalZone = 1.20;
cfg.socialZone   = 3.60;

cfg.trailDuration = 2.0;
cfg.mapMargin     = 1.5;
cfg.minMapSide    = 4.0;
cfg.gridDivisions = 8;
cfg.robotScale    = 0.035;
cfg.robotRingScale = 1.35;

% LiDAR-style top-view visualization parameters
% Supports extended Unity UDP packets with real plant/human/bush detections.
% Packet base fields are preserved: t,dist,vel,acc,px,py,pz,status,method,scenario.
% Optional extension from UnityToMatlabUDP_Lidar.cs adds heading, human points,
% plant/crop-row LiDAR-like point returns, bush/shrub returns, and generic object returns.
cfg.lidarEnabled = true;

% Robot-centered LiDAR map with FIXED SCALE.
% The axes follow the robot center, but the scale/window size never changes.
% Plant and human detections remain in absolute Unity world coordinates; MATLAB
% only changes the visible window.  Objects outside this fixed-size window are
% clipped naturally by the axes.
cfg.robotCenteredMapEnabled = true;
cfg.mapHalfWidth_m  = 10.0;            % X half-window around robot [m]; fixed scale
cfg.mapHalfDepth_m  = 10.0;            % Z half-window around robot [m]; fixed scale
cfg.fixedMapGridDivisions = 8;
cfg.accumulateStaticPlants = true;    % keep plant returns already observed
cfg.maxStoredPlantPoints = 12000;     % cap for accumulated plant static map
cfg.plantMergeResolution = 0.05;      % [m] quantization used to merge duplicated plant points
cfg.accumulateStaticBushes = true;    % keep bush/shrub returns already observed
cfg.maxStoredBushPoints = 12000;      % cap for accumulated bush static map
cfg.bushMergeResolution = 0.05;       % [m] quantization used to merge duplicated bush points
cfg.humanRingRadius = 0.55;           % human detection ring radius [m]
cfg.robotDisplayRadius = 0.18;        % robot marker radius in fixed world units [m]
cfg.robotHeadingLength = 1.1;         % heading ray length [m]

% LiDAR scan appearance.  The map still uses Unity world coordinates, but
% the visualization looks like a real planar LiDAR scan: fixed range,
% field-of-view cone, range rings, radial beams, and current returns.
cfg.lidarScanStyle = true;
cfg.lidarMaxRange_m = 14.0;              % sensor display range [m]; increased to see both vineyard sides
cfg.lidarFov_deg = 360;                 % full 2D scan, including left/right bushes [deg]
cfg.lidarRingStep_m = 1.0;              % range ring spacing [m]
cfg.lidarRayDecimation = 1;             % draw more vegetation rays for stronger bush/row visibility
cfg.lidarMaxRays = 220;                 % maximum beams drawn per frame
cfg.showGenericObjectReturns = true;      % draw all additional colliders sent by Unity
cfg.genericObjectRayDecimation = 1;       % draw more row/object rays because vineyard rows may arrive as objects
cfg.showAccumulatedMapAsFaint = true;   % old/static crop map remains faint
cfg.showRadarSpokes = true;             % draw a true 360-degree radar grid
cfg.radarSpokeStep_deg = 10;            % angular spacing for radar spokes
cfg.liveRadarSweepEnabled = true;       % draw a subtle sweep line using current time
cfg.showFull360ScanBeams = true;        % faint radial beams around the robot, even before a hit is returned
cfg.full360ScanBeamStep_deg = 10;       % beam spacing for the visual radar sweep grid
cfg.objectReturnsAsVegetation = true;   % useful when shrub/row colliders are assigned to objectMask in Unity
cfg.accumulateStaticObjectsAsRows = true;  % keep generic row/object returns as a faint corridor map
cfg.maxStoredObjectPoints = 16000;
cfg.objectMergeResolution = 0.05;
cfg.humanTrailDuration = 2.5;           % seconds; human paths fade like the robot trail

% Enhanced vegetation rendering in MATLAB. These parameters only affect display;
% Unity transmission and the saved telemetry/metrics are unchanged.
cfg.bushAccumPointSize = 18;
cfg.bushLivePointSize  = 32;
cfg.bushRayLineWidth   = 0.90;
cfg.rowObjectPointSize = 20;
cfg.rowObjectRayWidth  = 0.75;
cfg.plantLivePointSize = 18;
cfg.plantRayLineWidth  = 0.65;

cfg.timerPeriod   = 0.05;
cfg.plotPeriod    = 0.10;
cfg.stopThreshold = 0.05;
cfg.autoStopTimeout = 5.0;
cfg.runsPerCell = 10;

% V7.2: fallback parameters
cfg.missionTimeout = 180;
cfg.proximityThreshold = 1.5;
cfg.proximityMinTime = 60;

% =========================================================================
% STATE
% =========================================================================
S.u = [];
S.timer = [];
S.isRunning = false;

S.t = []; S.dist = []; S.vel = []; S.acc = [];
S.px = []; S.py = []; S.pz = [];
S.status = []; S.methodReceived = []; S.scenarioReceived = [];

% Latest Unity LiDAR-like detections. These are updated every UDP packet when
% using UnityToMatlabUDP_Lidar_BushScan.cs. Empty arrays trigger MATLAB fallback rendering.
S.headingX = NaN; S.headingZ = NaN;
S.lidarHumanX = []; S.lidarHumanZ = [];
S.lidarPlantX = []; S.lidarPlantZ = [];
S.lidarBushX = []; S.lidarBushZ = [];
S.lidarObjectX = []; S.lidarObjectZ = [];
S.humanTrailX = []; S.humanTrailZ = []; S.humanTrailT = [];
S.mapPlantX = []; S.mapPlantZ = [];
S.mapPlantKeys = strings(0,1);
S.mapObjectX = []; S.mapObjectZ = [];
S.mapObjectKeys = strings(0,1);
S.mapObjectX = []; S.mapObjectZ = [];
S.mapObjectKeys = strings(0,1);
S.mapBushX = []; S.mapBushZ = [];
S.mapBushKeys = strings(0,1);

S.missionStarted = false;
S.missionCompleted = false;
S.missionStartIdx = 0;
S.missionEndIdx = 0;
S.autoSaveTriggered = false;
S.runAlreadySaved = false;
S.reachedBLogged = false;
S.timeoutWarned = false;

S.packetCount = 0;
S.invalidCount = 0;
S.outOfOrderCount = 0;

S.lastPlotUpdate = tic;
S.lastPacketTime = tic;
S.acquisitionStart = tic;

H = struct();

fig = figure( ...
    'Name','Experimental Dashboard V7.4 - Social Agricultural Robot', ...
    'NumberTitle','off', 'Color','w', ...
    'Position',[60 50 1500 850], ...
    'CloseRequestFcn',@closeFigure);

panel = uipanel(fig, ...
    'Title','Experimental control V7.4 (B1-B4 baselines + auto-save + fallback)', ...
    'FontWeight','bold', 'FontSize',10, ...
    'Units','normalized', 'Position',[0.01 0.02 0.22 0.96]);

uicontrol(panel,'Style','text','String','Method:', ...
    'Units','normalized','Position',[0.05 0.94 0.40 0.025], ...
    'HorizontalAlignment','left','FontWeight','bold');
methodMenu = uicontrol(panel,'Style','popupmenu', ...
    'String',cfg.methodNames, 'Value',7, ...
    'Units','normalized','Position',[0.05 0.91 0.90 0.030]);

uicontrol(panel,'Style','text','String','Scenario:', ...
    'Units','normalized','Position',[0.05 0.875 0.40 0.025], ...
    'HorizontalAlignment','left','FontWeight','bold');
scenarioMenu = uicontrol(panel,'Style','popupmenu', ...
    'String',cfg.scenarioNames, 'Value',1, ...
    'Units','normalized','Position',[0.05 0.845 0.90 0.030]);

uicontrol(panel,'Style','text','String','Run ID (1..10):', ...
    'Units','normalized','Position',[0.05 0.81 0.50 0.025], ...
    'HorizontalAlignment','left','FontWeight','bold');
runEdit = uicontrol(panel,'Style','edit','String','1', ...
    'Units','normalized','Position',[0.55 0.81 0.40 0.030]);

uicontrol(panel,'Style','text','String','UDP Port:', ...
    'Units','normalized','Position',[0.05 0.775 0.50 0.025], ...
    'HorizontalAlignment','left','FontWeight','bold');
portEdit = uicontrol(panel,'Style','edit','String',num2str(cfg.port), ...
    'Units','normalized','Position',[0.55 0.775 0.40 0.030]);

uicontrol(panel,'Style','text','String','Trail [s]:', ...
    'Units','normalized','Position',[0.05 0.74 0.50 0.025], ...
    'HorizontalAlignment','left','FontWeight','bold');
trailEdit = uicontrol(panel,'Style','edit', ...
    'String',num2str(cfg.trailDuration), ...
    'Units','normalized','Position',[0.55 0.74 0.40 0.030]);

uicontrol(panel,'Style','text','String','Auto-stop [s]:', ...
    'Units','normalized','Position',[0.05 0.705 0.50 0.025], ...
    'HorizontalAlignment','left','FontWeight','bold');
timeoutEdit = uicontrol(panel,'Style','edit', ...
    'String',num2str(cfg.autoStopTimeout), ...
    'Units','normalized','Position',[0.55 0.705 0.40 0.030]);

autoSaveCheck = uicontrol(panel,'Style','checkbox', ...
    'String','Auto-save when mission completion is detected', ...
    'Value',1, ...
    'Units','normalized','Position',[0.05 0.675 0.90 0.025], ...
    'BackgroundColor',[0.95 0.95 0.95]);

missionStatusBox = uicontrol(panel,'Style','text', ...
    'String','Mission: NO DATA', ...
    'Units','normalized','Position',[0.05 0.643 0.90 0.028], ...
    'HorizontalAlignment','center', 'FontName','Consolas', ...
    'FontWeight','bold', 'FontSize',9, ...
    'BackgroundColor',[0.85 0.85 0.85]);

startBtn = uicontrol(panel,'Style','pushbutton', ...
    'String','START ACQUISITION', 'FontWeight','bold', ...
    'BackgroundColor',[0.7 1.0 0.7], ...
    'Units','normalized','Position',[0.05 0.595 0.90 0.040], ...
    'Callback',@startAcquisition);

stopBtn = uicontrol(panel,'Style','pushbutton', ...
    'String','STOP', 'FontWeight','bold', 'Enable','off', ...
    'BackgroundColor',[1.0 0.7 0.7], ...
    'Units','normalized','Position',[0.05 0.553 0.90 0.038], ...
    'Callback',@stopAcquisition);

saveBtn = uicontrol(panel,'Style','pushbutton', ...
    'String','SAVE DATA AND METRICS', 'FontWeight','bold', ...
    'BackgroundColor',[0.7 0.85 1.0], ...
    'Units','normalized','Position',[0.05 0.511 0.90 0.038], ...
    'Callback',@saveCurrentRun);

discardBtn = uicontrol(panel,'Style','pushbutton', ...
    'String','DISCARD AND RESTART', 'FontWeight','bold', ...
    'BackgroundColor',[1.0 0.65 0.4], ...
    'Units','normalized','Position',[0.05 0.469 0.90 0.038], ...
    'Callback',@discardAndRestart);

testBtn = uicontrol(panel,'Style','pushbutton', ...
    'String','TEST UDP', ...
    'BackgroundColor',[1.0 1.0 0.7], ...
    'Units','normalized','Position',[0.05 0.427 0.90 0.035], ...
    'Callback',@testUDP);

captureBtn = uicontrol(panel,'Style','pushbutton', ...
    'String','CAPTURE PNG', ...
    'Units','normalized','Position',[0.05 0.388 0.90 0.035], ...
    'Callback',@captureDashboard);

clearBtn = uicontrol(panel,'Style','pushbutton', ...
    'String','CLEAR RUN', ...
    'Units','normalized','Position',[0.05 0.349 0.90 0.035], ...
    'Callback',@clearCurrentData);

nextBtn = uicontrol(panel,'Style','pushbutton', ...
    'String','NEXT RUN', ...
    'Units','normalized','Position',[0.05 0.310 0.90 0.035], ...
    'Callback',@nextRun);

progressBtn = uicontrol(panel,'Style','pushbutton', ...
    'String','VIEW MATRIX PROGRESS', ...
    'Units','normalized','Position',[0.05 0.271 0.90 0.035], ...
    'Callback',@showProgress);

infoBox = uicontrol(panel,'Style','text', ...
    'String','Waiting to start...', ...
    'Units','normalized','Position',[0.05 0.236 0.90 0.030], ...
    'HorizontalAlignment','center', 'FontName','Consolas', ...
    'BackgroundColor',[0.95 0.95 0.95], 'FontSize',8);

statusBox = uicontrol(panel,'Style','listbox', ...
    'String',{'System ready V7.4.'}, ...
    'Units','normalized','Position',[0.05 0.04 0.90 0.190], ...
    'FontName','Consolas', 'FontSize',8);

axDist = subplot('Position',[0.27 0.58 0.33 0.36]);
axVel  = subplot('Position',[0.65 0.58 0.33 0.36]);
axAcc  = subplot('Position',[0.27 0.10 0.33 0.36]);
% Map panel uses the same normalized size as distance, velocity and acceleration plots.
axMap  = subplot('Position',[0.65 0.10 0.33 0.36]);

initAxes();

    function initAxes()
        cla(axDist); hold(axDist,'on');
        H.distLine = plot(axDist,NaN,NaN,'r-','LineWidth',1.6);
        yline(axDist,cfg.intimateZone,'--','Intimate', ...
            'LabelHorizontalAlignment','left');
        yline(axDist,cfg.personalZone,'--','Personal', ...
            'LabelHorizontalAlignment','left');
        yline(axDist,cfg.socialZone,'--','Social', ...
            'LabelHorizontalAlignment','left');
        grid(axDist,'on'); box(axDist,'on');
        title(axDist,'Robot-human distance');
        xlabel(axDist,'Time [s]'); ylabel(axDist,'Distance [m]');
        ylim(axDist,[0 5]);

        cla(axVel); hold(axVel,'on');
        H.velLine = plot(axVel,NaN,NaN,'b-','LineWidth',1.6);
        grid(axVel,'on'); box(axVel,'on');
        title(axVel,'Velocity'); xlabel(axVel,'Time [s]');
        ylabel(axVel,'Velocity [m/s]');
        ylim(axVel,[0 2]);

        cla(axAcc); hold(axAcc,'on');
        H.accLine = plot(axAcc,NaN,NaN,'k-','LineWidth',1.3);
        grid(axAcc,'on'); box(axAcc,'on');
        title(axAcc,'Acceleration'); xlabel(axAcc,'Time [s]');
        ylabel(axAcc,'Acceleration [m/s^2]');
        ylim(axAcc,[-2 5]);

        cla(axMap); hold(axMap,'on'); axis(axMap,'equal');
        grid(axMap,'on'); box(axMap,'on');
        title(axMap,'Robot position');
        xlabel(axMap,'X [m]'); ylabel(axMap,'Z [m]');
    end

    function startAcquisition(~,~)
        clearCurrentData();

        cfg.port = str2double(get(portEdit,'String'));
        cfg.trailDuration = str2double(get(trailEdit,'String'));
        cfg.autoStopTimeout = str2double(get(timeoutEdit,'String'));

        if isnan(cfg.port) || cfg.port <= 0 || cfg.port > 65535
            addStatus('ERROR: invalid UDP port.');
            return;
        end

        try
            if ~isempty(S.u), S.u = []; end
            delete(udpportfind);
        catch
        end

        try
            S.u = udpport("datagram","LocalPort",cfg.port);
        catch ME
            addStatus(['ERROR opening UDP: ', ME.message]);
            return;
        end

        S.isRunning = true;
        S.acquisitionStart = tic;
        S.lastPacketTime = tic;
        S.lastPlotUpdate = tic;

        set(startBtn,'Enable','off');
        set(stopBtn,'Enable','on');

        S.timer = timer( ...
            'ExecutionMode','fixedSpacing', ...
            'Period', cfg.timerPeriod, ...
            'TimerFcn', @timerCallback, ...
            'ErrorFcn', @timerError, ...
            'BusyMode','drop');

        start(S.timer);

        addStatus(sprintf('UDP opened on port %d.', cfg.port));
        addStatus('Acquisition started (V7.4 with B1-B4 baselines + fallback).');
    end

    function timerCallback(~,~)
        if ~S.isRunning || ~ishandle(fig), return; end

        try
            while S.u.NumDatagramsAvailable > 0
                pkt = read(S.u, 1, "uint8");
                if isempty(pkt) || isempty(pkt.Data), continue; end

                dataStr = string(char(pkt.Data(:))');

                valuesAll = str2double(split(strtrim(dataStr), ','));
                valuesAll = valuesAll(:);

                if numel(valuesAll) >= 10 && all(isfinite(valuesAll(1:10)))
                    tNew = valuesAll(1);
                    if ~isempty(S.t) && tNew < S.t(end)
                        S.outOfOrderCount = S.outOfOrderCount + 1;
                        continue;
                    end

                    S.t(end+1)        = valuesAll(1);
                    S.dist(end+1)     = valuesAll(2);
                    S.vel(end+1)      = valuesAll(3);
                    S.acc(end+1)      = valuesAll(4);
                    S.px(end+1)       = valuesAll(5);
                    S.py(end+1)       = valuesAll(6);
                    S.pz(end+1)       = valuesAll(7);
                    S.status(end+1)   = valuesAll(8);
                    S.methodReceived(end+1)   = valuesAll(9);
                    S.scenarioReceived(end+1) = valuesAll(10);

                    % Optional extended packet from UnityToMatlabUDP_Lidar.cs:
                    % [base10], headingX, headingZ, nHumans, hx,hz..., nPlants, px,pz..., nBushes, bx,bz..., nObjects, ox,oz...
                    S.headingX = NaN; S.headingZ = NaN;
                    S.lidarHumanX = []; S.lidarHumanZ = [];
                    S.lidarPlantX = []; S.lidarPlantZ = [];
                    S.lidarBushX = []; S.lidarBushZ = [];
                    S.lidarObjectX = []; S.lidarObjectZ = [];
                    if numel(valuesAll) >= 13
                        S.headingX = valuesAll(11);
                        S.headingZ = valuesAll(12);
                        idx = 13;
                        nHum = max(0, round(valuesAll(idx))); idx = idx + 1;
                        nHumPairs = min(nHum, floor((numel(valuesAll)-idx+1)/2));
                        if nHumPairs > 0
                            humVals = valuesAll(idx:idx+2*nHumPairs-1);
                            S.lidarHumanX = humVals(1:2:end)';
                            S.lidarHumanZ = humVals(2:2:end)';
                            idx = idx + 2*nHumPairs;
                        end
                        if numel(valuesAll) >= idx
                            nPlants = max(0, round(valuesAll(idx))); idx = idx + 1;
                            nPlantPairs = min(nPlants, floor((numel(valuesAll)-idx+1)/2));
                            if nPlantPairs > 0
                                plantVals = valuesAll(idx:idx+2*nPlantPairs-1);
                                S.lidarPlantX = plantVals(1:2:end)';
                                S.lidarPlantZ = plantVals(2:2:end)';
                                idx = idx + 2*nPlantPairs;
                            end
                        end
                        if numel(valuesAll) >= idx
                            nBushes = max(0, round(valuesAll(idx))); idx = idx + 1;
                            nBushPairs = min(nBushes, floor((numel(valuesAll)-idx+1)/2));
                            if nBushPairs > 0
                                bushVals = valuesAll(idx:idx+2*nBushPairs-1);
                                S.lidarBushX = bushVals(1:2:end)';
                                S.lidarBushZ = bushVals(2:2:end)';
                                idx = idx + 2*nBushPairs;
                            end
                        end
                        if numel(valuesAll) >= idx
                            nObjects = max(0, round(valuesAll(idx))); idx = idx + 1;
                            nObjectPairs = min(nObjects, floor((numel(valuesAll)-idx+1)/2));
                            if nObjectPairs > 0
                                objectVals = valuesAll(idx:idx+2*nObjectPairs-1);
                                S.lidarObjectX = objectVals(1:2:end)';
                                S.lidarObjectZ = objectVals(2:2:end)';
                            end
                        end
                    end

                    S.packetCount = S.packetCount + 1;
                    S.lastPacketTime = tic;

                    if ~S.missionStarted && valuesAll(3) > 0.05
                        S.missionStarted = true;
                        S.missionStartIdx = numel(S.t);
                        addStatus(sprintf( ...
                            '>>> MISSION STARTED at t=%.2fs (M%d, E%d)', ...
                            valuesAll(1), valuesAll(9), valuesAll(10)));
                    end

                    if ~S.missionCompleted && valuesAll(8) == 2
                        S.missionCompleted = true;
                        S.missionEndIdx = numel(S.t);
                        addStatus(sprintf( ...
                            '*** MISSION COMPLETED at t=%.2fs ***', ...
                            valuesAll(1)));
                    elseif valuesAll(8) == 1 && ~S.reachedBLogged
                        S.reachedBLogged = true;
                        addStatus(sprintf( ...
                            '=== Reached B at t=%.2fs, returning to A ===', ...
                            valuesAll(1)));
                    end

                    % V7.2: FALLBACK by proximity to A
                    if ~S.missionCompleted && S.reachedBLogged && ...
                       valuesAll(1) > cfg.proximityMinTime
                        if numel(S.t) > 5
                            distA_xz = sqrt( (valuesAll(5) - S.px(1))^2 + ...
                                             (valuesAll(7) - S.pz(1))^2 );
                            if distA_xz < cfg.proximityThreshold
                                S.missionCompleted = true;
                                S.missionEndIdx = numel(S.t);
                                addStatus(sprintf( ...
                                    '*** MISSION COMPLETED (proximity fallback) at t=%.2fs ***', ...
                                    valuesAll(1)));
                                addStatus(sprintf( ...
                                    'Distance to start: %.2fm', distA_xz));
                            end
                        end
                    end

                    % V7.2: 180s TIMEOUT
                    if ~S.missionCompleted && valuesAll(1) > cfg.missionTimeout && ...
                       ~S.timeoutWarned
                        S.timeoutWarned = true;
                        addStatus('!!! WARNING: run exceeds 180s.');
                        addStatus('!!! Robot stuck or threshold too small.');
                        addStatus('!!! Press DISCARD AND RESTART if so.');
                    end

                else
                    S.invalidCount = S.invalidCount + 1;
                end
            end

            if S.missionCompleted && ~S.autoSaveTriggered
                if get(autoSaveCheck,'Value') == 1
                    S.autoSaveTriggered = true;
                    addStatus('Auto-save triggered: saving...');
                    pause(0.3);
                    autoSaveCurrentRun();
                end
            end

            timeSinceStart = toc(S.acquisitionStart);
            timeSinceLastPkt = toc(S.lastPacketTime);

            if S.packetCount == 0
                statusStr = sprintf( ...
                    'WAITING FOR UNITY... %.1fs (port %d)', ...
                    timeSinceStart, cfg.port);
                set(infoBox,'String', statusStr, ...
                    'BackgroundColor',[1.0 0.85 0.85]);
            else
                statusStr = sprintf( ...
                    'OK: %d pkt | inv:%d | last:%.1fs', ...
                    S.packetCount, S.invalidCount, timeSinceLastPkt);
                if timeSinceLastPkt > 2.0
                    set(infoBox,'String', statusStr, ...
                        'BackgroundColor',[1.0 1.0 0.7]);
                else
                    set(infoBox,'String', statusStr, ...
                        'BackgroundColor',[0.85 1.0 0.85]);
                end
            end

            updateMissionStatusIndicator();

            if ~isempty(S.t) && toc(S.lastPlotUpdate) >= cfg.plotPeriod
                updatePlots();
                S.lastPlotUpdate = tic;
            end

            if S.packetCount > 0 && timeSinceLastPkt > cfg.autoStopTimeout
                addStatus(sprintf('Auto-stop: %.1fs without packets.', ...
                    cfg.autoStopTimeout));
                stopAcquisition();
            end

        catch ME
            fprintf(2,'Error en timerCallback: %s\n', ME.message);
        end
    end

    function timerError(~, evt)
        addStatus(['Timer error: ', evt.Data.messageID]);
    end

    function stopAcquisition(~,~)
        S.isRunning = false;

        try
            if ~isempty(S.timer) && isvalid(S.timer)
                stop(S.timer);
                delete(S.timer);
                S.timer = [];
            end
        catch
        end

        set(startBtn,'Enable','on');
        set(stopBtn,'Enable','off');

        addStatus(sprintf('Acquisition stopped. %d valid pkts.', ...
            S.packetCount));

        if ~isempty(S.t)
            updatePlots();
            M = computeMetrics();
            if ~isempty(M)
                addStatus(sprintf('Tmission=%.1fs | d*=%.2fm | Nstop=%d', ...
                    M.TotalTime_s, M.MinDistance_m, M.NumberOfStops));
            end
        end
    end

    function testUDP(~,~)
        if ~S.isRunning
            addStatus('TEST: Start acquisition first.');
            return;
        end

        try
            sender = udpport;
            testTime = toc(S.acquisitionStart);
            msg = sprintf('%.3f,1.50,0.60,0.10,0.0,0.0,0.0', testTime);
            write(sender, msg, "string", "127.0.0.1", cfg.port);
            delete(sender);
            addStatus(['TEST: packet sent -> ', msg]);
        catch ME
            addStatus(['UDP test ERROR: ', ME.message]);
        end
    end

    function updatePlots()
        if isempty(S.t), return; end

        try
            set(H.distLine,'XData',S.t,'YData',S.dist);
            ylim(axDist,[0, max(5, max(S.dist)+0.5)]);
            xlim(axDist,[S.t(1), max(S.t(end), S.t(1)+1)]);

            set(H.velLine,'XData',S.t,'YData',S.vel);
            ylim(axVel,[0, max(2, max(S.vel)+0.2)]);
            xlim(axVel,[S.t(1), max(S.t(end), S.t(1)+1)]);

            set(H.accLine,'XData',S.t,'YData',S.acc);
            ylim(axAcc,[min(-2, min(S.acc)-0.5), max(5, max(S.acc)+0.5)]);
            xlim(axAcc,[S.t(1), max(S.t(end), S.t(1)+1)]);

            cla(axMap);
            hold(axMap,'on');
            grid(axMap,'on'); box(axMap,'on'); axis(axMap,'equal');
            title(axMap,'Robot position');
            xlabel(axMap,'X [m]'); ylabel(axMap,'Z [m]');

            xr = S.px(end);
            zr = S.pz(end);

            % Heading comes from Unity when available; otherwise it is
            % estimated from the recent robot trajectory.  This affects only
            % the robot heading ray, NOT the plant map.
            if isfield(S,'headingX') && isfinite(S.headingX) && isfinite(S.headingZ) && hypot(S.headingX,S.headingZ) > 1e-6
                heading = [S.headingX S.headingZ] ./ hypot(S.headingX,S.headingZ);
            elseif numel(S.px) >= 4
                i0 = max(1, numel(S.px)-12);
                hx = S.px(end) - S.px(i0);
                hz = S.pz(end) - S.pz(i0);
                hn = hypot(hx,hz);
                if hn > 1e-6, heading = [hx hz] ./ hn; else, heading = [0 1]; end
            else
                heading = [0 1];
            end

            % Accumulate static crop/plant detections in WORLD coordinates.
            % Nothing is generated relative to the robot.  If a point is no
            % longer detected, it remains in the static map because the plant
            % did not physically move.
            if cfg.accumulateStaticPlants && ~isempty(S.lidarPlantX)
                qx = round(S.lidarPlantX(:)./cfg.plantMergeResolution).*cfg.plantMergeResolution;
                qz = round(S.lidarPlantZ(:)./cfg.plantMergeResolution).*cfg.plantMergeResolution;
                newKeys = string(qx) + ":" + string(qz);
                if isempty(S.mapPlantKeys)
                    keep = true(size(newKeys));
                else
                    keep = ~ismember(newKeys, S.mapPlantKeys);
                end
                if any(keep)
                    tmpPX = S.lidarPlantX(:);
                    tmpPZ = S.lidarPlantZ(:);
                    S.mapPlantX = [S.mapPlantX; tmpPX(keep)]; %#ok<AGROW>
                    S.mapPlantZ = [S.mapPlantZ; tmpPZ(keep)]; %#ok<AGROW>
                    S.mapPlantKeys = [S.mapPlantKeys; newKeys(keep)]; %#ok<AGROW>
                end
                if numel(S.mapPlantX) > cfg.maxStoredPlantPoints
                    excess = numel(S.mapPlantX) - cfg.maxStoredPlantPoints;
                    S.mapPlantX(1:excess) = [];
                    S.mapPlantZ(1:excess) = [];
                    S.mapPlantKeys(1:excess) = [];
                end
            end

            % Accumulate static bush/shrub detections in WORLD coordinates.
            % These are separate from crop rows, so MATLAB can draw shrubs as a
            % denser LiDAR layer without moving or scaling them with the robot.
            if cfg.accumulateStaticBushes && ~isempty(S.lidarBushX)
                qx = round(S.lidarBushX(:)./cfg.bushMergeResolution).*cfg.bushMergeResolution;
                qz = round(S.lidarBushZ(:)./cfg.bushMergeResolution).*cfg.bushMergeResolution;
                newKeys = string(qx) + ":" + string(qz);
                if isempty(S.mapBushKeys)
                    keep = true(size(newKeys));
                else
                    keep = ~ismember(newKeys, S.mapBushKeys);
                end
                if any(keep)
                    tmpBX = S.lidarBushX(:);
                    tmpBZ = S.lidarBushZ(:);
                    S.mapBushX = [S.mapBushX; tmpBX(keep)]; %#ok<AGROW>
                    S.mapBushZ = [S.mapBushZ; tmpBZ(keep)]; %#ok<AGROW>
                    S.mapBushKeys = [S.mapBushKeys; newKeys(keep)]; %#ok<AGROW>
                end
                if numel(S.mapBushX) > cfg.maxStoredBushPoints
                    excess = numel(S.mapBushX) - cfg.maxStoredBushPoints;
                    S.mapBushX(1:excess) = [];
                    S.mapBushZ(1:excess) = [];
                    S.mapBushKeys(1:excess) = [];
                end
            end


            % Accumulate generic object returns as row/corridor points when Unity
            % has the crop rows assigned to objectMask.  This is the case in many
            % scenes where shrubs are not separated into Plant/Bush layers.
            if cfg.accumulateStaticObjectsAsRows && ~isempty(S.lidarObjectX)
                qx = round(S.lidarObjectX(:)./cfg.objectMergeResolution).*cfg.objectMergeResolution;
                qz = round(S.lidarObjectZ(:)./cfg.objectMergeResolution).*cfg.objectMergeResolution;
                newKeys = string(qx) + ":" + string(qz);
                if isempty(S.mapObjectKeys)
                    keep = true(size(newKeys));
                else
                    keep = ~ismember(newKeys, S.mapObjectKeys);
                end
                if any(keep)
                    tmpOX = S.lidarObjectX(:);
                    tmpOZ = S.lidarObjectZ(:);
                    S.mapObjectX = [S.mapObjectX; tmpOX(keep)]; %#ok<AGROW>
                    S.mapObjectZ = [S.mapObjectZ; tmpOZ(keep)]; %#ok<AGROW>
                    S.mapObjectKeys = [S.mapObjectKeys; newKeys(keep)]; %#ok<AGROW>
                end
                if numel(S.mapObjectX) > cfg.maxStoredObjectPoints
                    excess = numel(S.mapObjectX) - cfg.maxStoredObjectPoints;
                    S.mapObjectX(1:excess) = [];
                    S.mapObjectZ(1:excess) = [];
                    S.mapObjectKeys(1:excess) = [];
                end
            end

            if cfg.accumulateStaticPlants
                plantX = S.mapPlantX(:);
                plantZ = S.mapPlantZ(:);
            else
                plantX = S.lidarPlantX(:);
                plantZ = S.lidarPlantZ(:);
            end

            if cfg.accumulateStaticBushes
                bushX = S.mapBushX(:);
                bushZ = S.mapBushZ(:);
            else
                bushX = S.lidarBushX(:);
                bushZ = S.lidarBushZ(:);
            end

            if cfg.accumulateStaticObjectsAsRows
                objectMapX = S.mapObjectX(:);
                objectMapZ = S.mapObjectZ(:);
            else
                objectMapX = S.lidarObjectX(:);
                objectMapZ = S.lidarObjectZ(:);
            end

            humanX = S.lidarHumanX(:);
            humanZ = S.lidarHumanZ(:);

            % Robot-centered axes with constant physical scale. The plot
            % follows the robot, but it never auto-zooms, shrinks, or expands.
            % Crop rows/humans are still plotted in WORLD coordinates; only the
            % viewport is translated to keep the robot at the center.
            if cfg.robotCenteredMapEnabled
                xmin = xr - cfg.mapHalfWidth_m;
                xmax = xr + cfg.mapHalfWidth_m;
                zmin = zr - cfg.mapHalfDepth_m;
                zmax = zr + cfg.mapHalfDepth_m;
                xc = xr;
                zc = zr;
                L = max(xmax-xmin, zmax-zmin);
            else
                allX = [S.px(:); plantX; bushX; humanX; S.lidarObjectX(:)];
                allZ = [S.pz(:); plantZ; bushZ; humanZ; S.lidarObjectZ(:)];
                xmin_d = min(allX) - cfg.mapMargin; xmax_d = max(allX) + cfg.mapMargin;
                zmin_d = min(allZ) - cfg.mapMargin; zmax_d = max(allZ) + cfg.mapMargin;
                xc = (xmin_d + xmax_d)/2; zc = (zmin_d + zmax_d)/2;
                L  = max([xmax_d-xmin_d, zmax_d-zmin_d, cfg.minMapSide]);
                xmin = xc - L/2; xmax = xc + L/2; zmin = zc - L/2; zmax = zc + L/2;
            end

            xlim(axMap,[xmin xmax]); ylim(axMap,[zmin zmax]);

            % -----------------------------------------------------------------
            % LiDAR-like display layer
            % -----------------------------------------------------------------
            % The axes are robot-centered with fixed scale.  Points remain in
            % Unity world coordinates, but the display is clipped by the LiDAR
            % range/FOV to look like a real 2D planar scan.
            if cfg.lidarScanStyle
                set(axMap,'Color',[0.985 0.985 0.985]);
            end

            stepX = (xmax-xmin)/cfg.fixedMapGridDivisions;
            stepZ = (zmax-zmin)/cfg.fixedMapGridDivisions;
            for xg = xmin:stepX:xmax
                plot(axMap,[xg xg],[zmin zmax],':','Color',[0.86 0.86 0.86]);
            end
            for zg = zmin:stepZ:zmax
                plot(axMap,[xmin xmax],[zg zg],':','Color',[0.86 0.86 0.86]);
            end

            % Local LiDAR geometry in world coordinates.
            scanRange = min(cfg.lidarMaxRange_m, max(cfg.mapHalfWidth_m,cfg.mapHalfDepth_m));
            fovRad = deg2rad(cfg.lidarFov_deg);
            ang0 = atan2(heading(2),heading(1));
            fovAngles = linspace(ang0 - fovRad/2, ang0 + fovRad/2, 120);

            % Range rings centered on the robot. These are fixed in metric size.
            if cfg.lidarScanStyle
                ringTheta = linspace(0,2*pi,240);
                for rr = cfg.lidarRingStep_m:cfg.lidarRingStep_m:scanRange
                    plot(axMap, xr + rr*cos(ringTheta), zr + rr*sin(ringTheta), '-', ...
                        'Color',[0.82 0.82 0.82], 'LineWidth',0.55);
                    if rr < scanRange
                        text(axMap, xr + rr + 0.03, zr, sprintf('%gm',rr), ...
                            'FontSize',7, 'Color',[0.45 0.45 0.45], ...
                            'BackgroundColor',[0.985 0.985 0.985], 'Margin',1);
                    end
                end

                % 360-degree radar frame.  For a full scan, draw circular
                % boundary plus radial spokes instead of a one-sided sector.
                plot(axMap, xr + scanRange*cos(fovAngles), zr + scanRange*sin(fovAngles), '-', ...
                    'Color',[0.25 0.70 0.95], 'LineWidth',0.8);

                if cfg.showRadarSpokes
                    spokeAngles = deg2rad(0:cfg.radarSpokeStep_deg:345);
                    for aa = spokeAngles
                        plot(axMap, [xr xr+scanRange*cos(aa)], [zr zr+scanRange*sin(aa)], ':', ...
                            'Color',[0.80 0.84 0.88], 'LineWidth',0.45);
                    end
                end

                if cfg.showFull360ScanBeams
                    beamAngles = deg2rad(0:cfg.full360ScanBeamStep_deg:359);
                    for aa = beamAngles
                        plot(axMap, [xr xr+scanRange*cos(aa)], [zr zr+scanRange*sin(aa)], '-', ...
                            'Color',[0.90 0.94 0.98], 'LineWidth',0.25);
                    end
                end

                if cfg.liveRadarSweepEnabled && ~isempty(S.t)
                    sweepAngle = ang0 + 2*pi*mod(S.t(end),2.0)/2.0;
                    plot(axMap, [xr xr+scanRange*cos(sweepAngle)], [zr zr+scanRange*sin(sweepAngle)], '-', ...
                        'Color',[0.15 0.65 0.95], 'LineWidth',1.0);
                end

                if cfg.lidarFov_deg < 359.9
                    plot(axMap, [xr xr+scanRange*cos(fovAngles(1))], [zr zr+scanRange*sin(fovAngles(1))], '-', ...
                        'Color',[0.25 0.70 0.95], 'LineWidth',0.8);
                    plot(axMap, [xr xr+scanRange*cos(fovAngles(end))], [zr zr+scanRange*sin(fovAngles(end))], '-', ...
                        'Color',[0.25 0.70 0.95], 'LineWidth',0.8);
                end
            end

            % Current scan points from Unity: plants and humans in world coordinates.
            curPlantX = S.lidarPlantX(:); curPlantZ = S.lidarPlantZ(:);
            curBushX  = S.lidarBushX(:);  curBushZ  = S.lidarBushZ(:);
            curHumanX = S.lidarHumanX(:); curHumanZ = S.lidarHumanZ(:);
            curObjectX = S.lidarObjectX(:); curObjectZ = S.lidarObjectZ(:);

            % Human motion trail. Humans can be fixed or moving; MATLAB keeps
            % only a short time history so their path fades/disappears like the
            % robot trail instead of becoming a permanent map.
            if ~isempty(curHumanX)
                S.humanTrailX = [S.humanTrailX; curHumanX]; %#ok<AGROW>
                S.humanTrailZ = [S.humanTrailZ; curHumanZ]; %#ok<AGROW>
                S.humanTrailT = [S.humanTrailT; repmat(S.t(end),numel(curHumanX),1)]; %#ok<AGROW>
            end
            if ~isempty(S.humanTrailT)
                keepHumanTrail = S.humanTrailT >= (S.t(end) - cfg.humanTrailDuration);
                S.humanTrailX = S.humanTrailX(keepHumanTrail);
                S.humanTrailZ = S.humanTrailZ(keepHumanTrail);
                S.humanTrailT = S.humanTrailT(keepHumanTrail);
            end

            % Helper masks: visible by range and forward FOV.
            plantScanMask = false(size(curPlantX));
            if ~isempty(curPlantX)
                dxp = curPlantX - xr; dzp = curPlantZ - zr;
                rp = hypot(dxp,dzp);
                ap = atan2(dzp,dxp);
                dap = atan2(sin(ap-ang0), cos(ap-ang0));
                plantScanMask = rp <= scanRange & abs(dap) <= fovRad/2;
            end

            bushScanMask = false(size(curBushX));
            if ~isempty(curBushX)
                dxb = curBushX - xr; dzb = curBushZ - zr;
                rb = hypot(dxb,dzb);
                ab = atan2(dzb,dxb);
                dab = atan2(sin(ab-ang0), cos(ab-ang0));
                bushScanMask = rb <= scanRange & abs(dab) <= fovRad/2;
            end

            humanScanMask = false(size(curHumanX));
            if ~isempty(curHumanX)
                dxh = curHumanX - xr; dzh = curHumanZ - zr;
                rh = hypot(dxh,dzh);
                ah = atan2(dzh,dxh);
                dah = atan2(sin(ah-ang0), cos(ah-ang0));
                humanScanMask = rh <= scanRange & abs(dah) <= fovRad/2;
            end

            objectScanMask = false(size(curObjectX));
            if ~isempty(curObjectX)
                dxo = curObjectX - xr; dzo = curObjectZ - zr;
                ro = hypot(dxo,dzo);
                ao = atan2(dzo,dxo);
                dao = atan2(sin(ao-ang0), cos(ao-ang0));
                objectScanMask = ro <= scanRange & abs(dao) <= fovRad/2;
            end

            % Faint accumulated/static crop map. This gives context without
            % making old returns look like live LiDAR beams.
            if cfg.showAccumulatedMapAsFaint && ~isempty(plantX)
                inViewport = plantX >= xmin & plantX <= xmax & plantZ >= zmin & plantZ <= zmax;
                scatter(axMap,plantX(inViewport),plantZ(inViewport),10,[0.62 0.82 0.58],'.');
            end
            if cfg.showAccumulatedMapAsFaint && ~isempty(bushX)
                inViewportBush = bushX >= xmin & bushX <= xmax & bushZ >= zmin & bushZ <= zmax;
                scatter(axMap,bushX(inViewportBush),bushZ(inViewportBush),cfg.bushAccumPointSize,[0.12 0.55 0.10],'.');
            end
            if cfg.showAccumulatedMapAsFaint && cfg.accumulateStaticObjectsAsRows && ~isempty(objectMapX)
                inViewportObj = objectMapX >= xmin & objectMapX <= xmax & objectMapZ >= zmin & objectMapZ <= zmax;
                scatter(axMap,objectMapX(inViewportObj),objectMapZ(inViewportObj),cfg.rowObjectPointSize,[0.18 0.66 0.14],'.');
            end

            % LiDAR rays to the current plant returns.  Decimated for speed.
            scanPlantX = curPlantX(plantScanMask);
            scanPlantZ = curPlantZ(plantScanMask);
            nScanPlant = numel(scanPlantX);
            if nScanPlant > 0
                idx = 1:max(1,cfg.lidarRayDecimation):nScanPlant;
                if numel(idx) > cfg.lidarMaxRays
                    idx = round(linspace(1,nScanPlant,cfg.lidarMaxRays));
                end
                for kk = idx
                    plot(axMap,[xr scanPlantX(kk)],[zr scanPlantZ(kk)], '-', ...
                        'Color',[0.66 0.88 0.62], 'LineWidth',cfg.plantRayLineWidth);
                end
                scatter(axMap,scanPlantX,scanPlantZ,cfg.plantLivePointSize,[0.00 0.58 0.10],'.');
            end


            % Bush / shrub LiDAR returns.  These are drawn as a denser lateral
            % vegetation layer, useful for orchard/vineyard rows and low bushes.
            scanBushX = curBushX(bushScanMask);
            scanBushZ = curBushZ(bushScanMask);
            nScanBush = numel(scanBushX);
            if nScanBush > 0
                idxB = 1:max(1,cfg.lidarRayDecimation):nScanBush;
                if numel(idxB) > cfg.lidarMaxRays
                    idxB = round(linspace(1,nScanBush,cfg.lidarMaxRays));
                end
                for kk = idxB
                    plot(axMap,[xr scanBushX(kk)],[zr scanBushZ(kk)], '-', ...
                        'Color',[0.18 0.76 0.18], 'LineWidth',cfg.bushRayLineWidth);
                end
                scatter(axMap,scanBushX,scanBushZ,cfg.bushLivePointSize,[0.00 0.38 0.00],'.');
            end

            % Generic object / obstacle LiDAR returns. These represent every
            % collider sent by Unity through objectMask, so they can include
            % posts, trunks, tools, fences, boxes, vehicles, rocks, terrain
            % obstacles, or any other fixed/moving object around the robot.
            scanObjectX = curObjectX(objectScanMask);
            scanObjectZ = curObjectZ(objectScanMask);
            nScanObject = numel(scanObjectX);
            if cfg.showGenericObjectReturns && nScanObject > 0
                idxO = 1:max(1,cfg.genericObjectRayDecimation):nScanObject;
                if numel(idxO) > cfg.lidarMaxRays
                    idxO = round(linspace(1,nScanObject,cfg.lidarMaxRays));
                end
                if cfg.objectReturnsAsVegetation
                    rayColorObj = [0.25 0.80 0.22];
                    ptColorObj  = [0.00 0.42 0.00];
                else
                    rayColorObj = [0.62 0.70 0.82];
                    ptColorObj  = [0.20 0.32 0.52];
                end
                for kk = idxO
                    plot(axMap,[xr scanObjectX(kk)],[zr scanObjectZ(kk)], '-', ...
                        'Color',rayColorObj, 'LineWidth',cfg.rowObjectRayWidth);
                end
                scatter(axMap,scanObjectX,scanObjectZ,cfg.rowObjectPointSize,ptColorObj,'.');
            end

            % Human trails: faint and time-limited, like the recent robot path.
            if ~isempty(S.humanTrailX)
                inHumanTrail = S.humanTrailX >= xmin & S.humanTrailX <= xmax & ...
                               S.humanTrailZ >= zmin & S.humanTrailZ <= zmax;
                scatter(axMap,S.humanTrailX(inHumanTrail),S.humanTrailZ(inHumanTrail),16, ...
                    [1.0 0.62 0.45],'.');
            end

            % Human detections: one clean point per person.
            % Unity sends human centroids, so MATLAB does not draw clusters or rings.
            scanHumanX = curHumanX(humanScanMask);
            scanHumanZ = curHumanZ(humanScanMask);
            if ~isempty(scanHumanX)
                scatter(axMap,scanHumanX,scanHumanZ,46,[1.0 0.18 0.0],'filled', ...
                    'MarkerEdgeColor',[0.25 0.02 0.0], 'LineWidth',0.8);
            end

            % Full robot path and recent trail.  These move because the robot
            % moves through the fixed world map.
            plot(axMap, S.px, S.pz, '-', 'Color',[0.65 0.80 0.65], 'LineWidth',0.55);
            idxTrail = S.t >= (S.t(end) - cfg.trailDuration);
            if sum(idxTrail) > 1
                plot(axMap, S.px(idxTrail), S.pz(idxTrail), '-', 'Color',[0.0 0.55 0.0], 'LineWidth',1.4);
            end

            % Robot marker and heading beam. Fixed physical marker radius.
            R = cfg.robotDisplayRadius;
            R2 = cfg.robotRingScale * R;
            th = linspace(0,2*pi,160);

            plot(axMap, [xr, xr + cfg.robotHeadingLength*heading(1)], ...
                [zr, zr + cfg.robotHeadingLength*heading(2)], '-', ...
                'Color',[0.0 0.45 0.0], 'LineWidth',2.0);

            fill(axMap, xr+R*cos(th), zr+R*sin(th), ...
                [0.10 0.65 0.10], 'EdgeColor',[0 0.25 0], 'LineWidth',1.6);
            plot(axMap, xr, zr, 'k.', 'MarkerSize',18);
            plot(axMap, xr+R2*cos(th), zr+R2*sin(th), '--','Color',[0 0.45 0],'LineWidth',1.0);

            text(axMap, xc, zmin-0.10*L, ...
                sprintf('X=%.2fm   Z=%.2fm   |   d_{min}=%.2fm', xr, zr, min(S.dist)), ...
                'HorizontalAlignment','center', 'VerticalAlignment','top', ...
                'FontWeight','bold', 'BackgroundColor','w', ...
                'EdgeColor',[0.4 0.4 0.4], 'Margin',6, 'Clipping','off');

            text(axMap, xmin+0.03*(xmax-xmin), zmax-0.06*(zmax-zmin), ...
                sprintf('LiDAR 360 enhanced bushes | plants: %d | bushes: %d | humans: %d | row/objects: %d', numel(scanPlantX), numel(scanBushX), numel(scanHumanX), nScanObject), ...
                'FontSize',8, 'BackgroundColor','w', 'EdgeColor',[0.75 0.75 0.75], 'Margin',4);
        catch ME
            addStatus(['Map update warning: ', ME.message]);
        end
    end

% =========================================================================
% V7.2 METRICS (T_personal FIX)
% =========================================================================
    function M = computeMetrics()
        if numel(S.t) < 2, M = []; return; end

        % V7.2 FIX: use actual dt between samples (NOT constant median)
        dt_vec = diff(S.t);
        dt_med = median(dt_vec);
        if ~isfinite(dt_med) || dt_med <= 0, dt_med = 0.1; end

        % Cleanup of anomalous dt values
        bad_dt = dt_vec <= 0 | dt_vec > 2.0;
        dt_vec(bad_dt) = dt_med;

        % Zone times weighted by actual dt
        dist_arr = S.dist(:);
        vel_arr  = S.vel(:);
        dt_col   = dt_vec(:);

        in_social   = dist_arr(1:end-1) < cfg.socialZone;
        in_personal = dist_arr(1:end-1) < cfg.personalZone;
        in_intimate = dist_arr(1:end-1) < cfg.intimateZone;
        is_stopped  = vel_arr(1:end-1)  < cfg.stopThreshold;

        M.SocialTime_s   = sum(in_social   .* dt_col);
        M.PersonalTime_s = sum(in_personal .* dt_col);
        M.IntimateTime_s   = sum(in_intimate .* dt_col);
        M.StopTime_s     = sum(is_stopped  .* dt_col);

        M.TotalTime_s = S.t(end) - S.t(1);

        % Saturation: times CANNOT exceed duration
        M.SocialTime_s   = min(M.SocialTime_s,   M.TotalTime_s);
        M.PersonalTime_s = min(M.PersonalTime_s, M.TotalTime_s);
        M.IntimateTime_s   = min(M.IntimateTime_s,   M.TotalTime_s);
        M.StopTime_s     = min(M.StopTime_s,     M.TotalTime_s);

        % Trajectory
        dx = diff(S.px); dz = diff(S.pz);
        M.PathLength_m = sum(sqrt(dx.^2 + dz.^2));

        stopped_full = S.vel < cfg.stopThreshold;
        if numel(stopped_full) >= 2
            transitions = diff(double(stopped_full));
            M.NumberOfStops = sum(transitions == 1);
        else
            M.NumberOfStops = 0;
        end

        M.MinDistance_m     = min(S.dist);
        M.MeanDistance_m   = mean(S.dist);
        M.MedianDistance_m = median(S.dist);
        M.DistanceP05_m     = prctile(S.dist, 5);
        M.DistanceIQR_m     = iqr(S.dist);

        M.MeanVelocity_mps   = mean(S.vel);
        M.MedianVelocity_mps = median(S.vel);
        M.MaxVelocity_mps     = max(S.vel);
        M.VelocityStd_mps     = std(S.vel);

        M.MaxAcceleration_mps2 = max(abs(S.acc));
        M.AccRMS_mps2 = sqrt(mean(S.acc.^2));

        if numel(S.acc) > 1
            jerk_vec = diff(S.acc) ./ max(dt_col, 0.01);
            jerk_vec(~isfinite(jerk_vec)) = 0;
            M.JerkMax_mps3 = max(abs(jerk_vec));
            M.JerkRMS_mps3 = sqrt(mean(jerk_vec.^2));
        else
            M.JerkMax_mps3 = 0;
            M.JerkRMS_mps3 = 0;
        end

        if M.TotalTime_s > 0
            M.PathEfficiency = M.PathLength_m / M.TotalTime_s;
        else
            M.PathEfficiency = 0;
        end
    end

    function saveCurrentRun(~,~)
        if isempty(S.t)
            addStatus('No data to save.');
            return;
        end

        if S.runAlreadySaved
            addStatus('WARNING: This run has already been saved.');
            addStatus('Press CLEAR and start again.');
            return;
        end

        M = computeMetrics();
        if isempty(M)
            addStatus('Insufficient data for metrics.');
            return;
        end

        methodIdx    = get(methodMenu,'Value');
        scenarioIdx = get(scenarioMenu,'Value');
        method       = cfg.methodCodes{methodIdx};
        scenario    = cfg.scenarioCodes{scenarioIdx};

        runID = str2double(get(runEdit,'String'));
        if isnan(runID) || runID < 1, runID = 1; end

        outFolder = fullfile(cfg.baseFolder, method, scenario);
        if ~exist(outFolder,'dir'), mkdir(outFolder); end

        baseName = sprintf('%s_%s_run%02d', method, scenario, runID);
        csvFile     = fullfile(outFolder, [baseName '.csv']);
        matFile     = fullfile(outFolder, [baseName '.mat']);
        metricsFile = fullfile(outFolder, [baseName '_metrics.csv']);

        if exist(csvFile,'file')
            choice = questdlg( ...
                sprintf('%s already exists. Overwrite?', baseName), ...
                'Confirm','Yes','No','No');
            if ~strcmp(choice,'Yes')
                addStatus('Save canceled.');
                return;
            end
        end

        n = numel(S.t);
        telemetry = table( ...
            S.t(:), S.dist(:), S.vel(:), S.acc(:), ...
            S.px(:), S.py(:), S.pz(:), ...
            repmat(string(method),    n, 1), ...
            repmat(string(scenario), n, 1), ...
            repmat(runID,             n, 1), ...
            'VariableNames', {'Time_s','Distance_m','Velocity_mps', ...
                              'Acceleration_mps2','PosX_m','PosY_m','PosZ_m', ...
                              'Method','Scenario','RunID'});

        metrics = struct2table(M);
        metrics = addvars(metrics, ...
            string(method), string(scenario), runID, ...
            string(char(datetime('now','Format','yyyy-MM-dd HH:mm:ss'))), ...
            S.packetCount, S.invalidCount, S.outOfOrderCount, ...
            'NewVariableNames', ...
            {'Method','Scenario','RunID','Timestamp', ...
             'PacketsValid','PacketsInvalid','PacketsOutOfOrder'}, ...
            'Before', 1);

        writetable(telemetry, csvFile,     'Delimiter',',');
        writetable(metrics,   metricsFile, 'Delimiter',',');
        save(matFile, 'telemetry', 'metrics');

        updateMasterLog(method, scenario, runID, M);

        S.runAlreadySaved = true;

        addStatus(sprintf('Saved: %s', baseName));

        if runID < cfg.runsPerCell
            set(runEdit,'String',num2str(runID+1));
            addStatus(sprintf('Next run: %d', runID+1));
            addStatus('Clear and start again.');
        else
            addStatus(sprintf( ...
                'Combination %s_%s COMPLETED (10/10).', method, scenario));
        end
    end

    function updateMasterLog(method, scenario, runID, M)
        masterFile = fullfile(cfg.baseFolder, 'master_log.csv');
        if ~exist(cfg.baseFolder,'dir'), mkdir(cfg.baseFolder); end

        newRow = table( ...
            string(char(datetime('now','Format','yyyy-MM-dd HH:mm:ss'))), ...
            string(method), string(scenario), runID, ...
            M.TotalTime_s, M.PathLength_m, ...
            M.MinDistance_m, M.MeanDistance_m, M.MedianDistance_m, ...
            M.MeanVelocity_mps, M.MaxVelocity_mps, ...
            M.MaxAcceleration_mps2, M.JerkMax_mps3, ...
            M.SocialTime_s, M.PersonalTime_s, M.IntimateTime_s, ...
            M.StopTime_s, M.NumberOfStops, ...
            'VariableNames', {'Timestamp','Method','Scenario','RunID', ...
                'TotalTime_s','PathLength_m', ...
                'MinDistance_m','MeanDistance_m','MedianDistance_m', ...
                'MeanVelocity_mps','MaxVelocity_mps', ...
                'MaxAcceleration_mps2','MaxJerk_mps3', ...
                'SocialTime_s','PersonalTime_s','IntimateTime_s', ...
                'StopTime_s','NumberOfStops'});

        if exist(masterFile,'file')
            try
                T = readtable(masterFile,'Delimiter',',');
                T = [T; newRow];
                writetable(T, masterFile, 'Delimiter',',');
            catch
                writetable(newRow, masterFile, 'Delimiter',',');
            end
        else
            writetable(newRow, masterFile, 'Delimiter',',');
        end
    end

    function showProgress(~,~)
        masterFile = fullfile(cfg.baseFolder, 'master_log.csv');
        if ~exist(masterFile,'file')
            addStatus('No runs have been registered yet.');
            return;
        end
        try
            T = readtable(masterFile);
        catch
            addStatus('Error reading master_log.csv');
            return;
        end

        addStatus('=== MATRIX PROGRESS ===');
        totalDone = 0;
        for mi = 1:numel(cfg.methodCodes)
            for ei = 1:numel(cfg.scenarioCodes)
                m = cfg.methodCodes{mi};
                e = cfg.scenarioCodes{ei};
                mask = strcmp(T.Method, m) & strcmp(T.Scenario, e);
                runsDone = numel(unique(T.RunID(mask)));
                totalDone = totalDone + runsDone;
                if runsDone > 0
                    addStatus(sprintf('  %s_%s: %d/10', m, e, runsDone));
                end
            end
        end
        totalPlanned = numel(cfg.methodCodes) * numel(cfg.scenarioCodes) * cfg.runsPerCell;
        addStatus(sprintf('TOTAL: %d/%d (%.1f%%)', ...
            totalDone, totalPlanned, 100*totalDone/totalPlanned));
    end

    function captureDashboard(~,~)
        methodIdx    = get(methodMenu,'Value');
        scenarioIdx = get(scenarioMenu,'Value');
        method       = cfg.methodCodes{methodIdx};
        scenario    = cfg.scenarioCodes{scenarioIdx};
        runID = str2double(get(runEdit,'String'));
        if isnan(runID), runID = 1; end

        outFolder = fullfile(cfg.baseFolder, method, scenario);
        if ~exist(outFolder,'dir'), mkdir(outFolder); end

        timestamp = char(datetime('now','Format','yyyyMMdd_HHmmss'));
        baseName = sprintf('%s_%s_run%02d_dashboard_%s.png', ...
            method, scenario, runID, timestamp);
        pngFile = fullfile(outFolder, baseName);

        exportgraphics(fig, pngFile, 'Resolution', 300);
        addStatus(sprintf('PNG: %s', baseName));
    end

    function discardAndRestart(~,~)
        wasRunning = S.isRunning;
        S.isRunning = false;

        if ~isempty(S.timer) && isvalid(S.timer)
            try
                stop(S.timer);
                pause(0.1);
                delete(S.timer);
            catch
            end
            S.timer = [];
        end

        nPaq = S.packetCount;
        tDur = 0;
        if ~isempty(S.t)
            tDur = S.t(end) - S.t(1);
        end

        if nPaq > 0 || ~isempty(S.t) || wasRunning

            choice = questdlg( ...
                sprintf(['Discard current run?\n\n' ...
                'Packets: %d\nDuration: %.1f s'], ...
                nPaq, tDur), ...
                'Confirm discard', ...
                'YES, discard','NO, keep','NO, keep');

            if ~strcmp(choice,'YES, discard')
                addStatus('Discard canceled.');

                if wasRunning
                    try
                        S.isRunning = true;
                        S.timer = timer( ...
                            'ExecutionMode','fixedSpacing', ...
                            'Period', cfg.timerPeriod, ...
                            'TimerFcn', @timerCallback, ...
                            'ErrorFcn', @timerError, ...
                            'BusyMode','drop');
                        start(S.timer);
                        addStatus('Acquisition resumed.');
                    catch ME
                        addStatus(['Could not resume: ', ME.message]);
                        set(startBtn,'Enable','on');
                        set(stopBtn,'Enable','off');
                    end
                end
                return;
            end
        end

        try
            if ~isempty(S.u) && isvalid(S.u)
                while S.u.NumDatagramsAvailable > 0
                    read(S.u, 1, "uint8");
                end
            end
        catch
        end

        try
            if ~isempty(S.u)
                S.u = [];
            end
            delete(udpportfind);
        catch
        end

        S.t = []; S.dist = []; S.vel = []; S.acc = [];
        S.px = []; S.py = []; S.pz = [];
        S.status = []; S.methodReceived = []; S.scenarioReceived = [];

% Latest Unity LiDAR-like detections. These are updated every UDP packet when
% using UnityToMatlabUDP_Lidar_BushScan.cs. Empty arrays trigger MATLAB fallback rendering.
S.headingX = NaN; S.headingZ = NaN;
S.lidarHumanX = []; S.lidarHumanZ = [];
S.lidarPlantX = []; S.lidarPlantZ = [];
S.lidarBushX = []; S.lidarBushZ = [];
S.lidarObjectX = []; S.lidarObjectZ = [];
S.humanTrailX = []; S.humanTrailZ = []; S.humanTrailT = [];
S.mapPlantX = []; S.mapPlantZ = [];
S.mapPlantKeys = strings(0,1);
S.mapObjectX = []; S.mapObjectZ = [];
S.mapObjectKeys = strings(0,1);
S.mapBushX = []; S.mapBushZ = [];
S.mapBushKeys = strings(0,1);
        S.packetCount = 0;
        S.invalidCount = 0;
        S.outOfOrderCount = 0;
        S.lastPlotUpdate = tic;
        S.lastPacketTime = tic;
        S.acquisitionStart = tic;

        S.missionStarted = false;
        S.missionCompleted = false;
        S.missionStartIdx = 0;
        S.missionEndIdx = 0;
        S.autoSaveTriggered = false;
        S.runAlreadySaved = false;
        S.reachedBLogged = false;
        S.timeoutWarned = false;

        set(startBtn,'Enable','on');
        set(stopBtn,'Enable','off');

        try, initAxes(); catch, end

        set(infoBox,'String', ...
            'Run discarded. Ready to retry.', ...
            'BackgroundColor',[1.0 0.85 0.85]);

        try
            set(missionStatusBox,'String','Mission: NO DATA', ...
                'BackgroundColor',[0.85 0.85 0.85]);
        catch
        end

        runID = str2double(get(runEdit,'String'));
        if isnan(runID), runID = 1; end

        addStatus('=== RUN DISCARDED ===');
        addStatus(sprintf('Run ID remains: %d', runID));
        addStatus('Press START to retry.');
    end

    function clearCurrentData(~,~)
        S.t = []; S.dist = []; S.vel = []; S.acc = [];
        S.px = []; S.py = []; S.pz = [];
        S.status = []; S.methodReceived = []; S.scenarioReceived = [];

% Latest Unity LiDAR-like detections. These are updated every UDP packet when
% using UnityToMatlabUDP_Lidar_BushScan.cs. Empty arrays trigger MATLAB fallback rendering.
S.headingX = NaN; S.headingZ = NaN;
S.lidarHumanX = []; S.lidarHumanZ = [];
S.lidarPlantX = []; S.lidarPlantZ = [];
S.lidarBushX = []; S.lidarBushZ = [];
S.lidarObjectX = []; S.lidarObjectZ = [];
S.humanTrailX = []; S.humanTrailZ = []; S.humanTrailT = [];
S.mapPlantX = []; S.mapPlantZ = [];
S.mapPlantKeys = strings(0,1);
S.mapObjectX = []; S.mapObjectZ = [];
S.mapObjectKeys = strings(0,1);
S.mapBushX = []; S.mapBushZ = [];
S.mapBushKeys = strings(0,1);
        S.packetCount = 0; S.invalidCount = 0; S.outOfOrderCount = 0;
        S.lastPlotUpdate = tic;
        S.lastPacketTime = tic;
        S.acquisitionStart = tic;

        S.missionStarted = false;
        S.missionCompleted = false;
        S.missionStartIdx = 0;
        S.missionEndIdx = 0;
        S.autoSaveTriggered = false;
        S.runAlreadySaved = false;
        S.reachedBLogged = false;
        S.timeoutWarned = false;

        set(infoBox,'String','Waiting to start...', ...
            'BackgroundColor',[0.95 0.95 0.95]);
        set(missionStatusBox,'String','Mission: NO DATA', ...
            'BackgroundColor',[0.85 0.85 0.85]);
        initAxes();
        addStatus('Run cleared. Ready for a new run.');
    end

    function updateMissionStatusIndicator()
        if S.missionCompleted
            txt = 'Mission: COMPLETED';
            color = [0.7 1.0 0.7];
        elseif S.missionStarted
            txt = sprintf('Mission: NAVIGATING (%.1fs)', ...
                S.t(end) - S.t(S.missionStartIdx));
            color = [0.7 0.85 1.0];
        elseif S.packetCount > 0
            txt = 'Mission: WAITING FOR START...';
            color = [1.0 1.0 0.7];
        else
            txt = 'Mission: NO DATA';
            color = [0.85 0.85 0.85];
        end

        set(missionStatusBox,'String',txt,'BackgroundColor',color);
    end

    function autoSaveCurrentRun()
        S.isRunning = false;

        try
            if ~isempty(S.timer) && isvalid(S.timer)
                stop(S.timer);
                pause(0.1);
                delete(S.timer);
                S.timer = [];
            end
        catch
        end

        set(startBtn,'Enable','on');
        set(stopBtn,'Enable','off');

        saveCurrentRun();

        addStatus('Press Stop in Unity, then START for the next one.');
    end

    function nextRun(~,~)
        runID = str2double(get(runEdit,'String'));
        if isnan(runID), runID = 1; end
        runID = runID + 1;

        if runID > cfg.runsPerCell
            choice = questdlg( ...
                sprintf('Run %d exceeds 10. Continue?', runID), ...
                'Exceeds','Yes','No','No');
            if ~strcmp(choice,'Yes'), return; end
        end

        set(runEdit,'String',num2str(runID));
        clearCurrentData();
        addStatus(sprintf('Prepared for run %d.', runID));
    end

    function addStatus(msg)
        try
            old = get(statusBox,'String');
            if ischar(old), old = {old}; end
            timestamp = char(datetime('now','Format','HH:mm:ss'));
            newMsg = ['[' timestamp '] ' msg];
            set(statusBox,'String',[{newMsg}; old(:)]);
        catch
        end
    end

    function out = ternaryText(cond, a, b)
        if cond, out = a; else, out = b; end
    end

    function closeFigure(~,~)
        S.isRunning = false;

        try
            if ~isempty(S.timer) && isvalid(S.timer)
                stop(S.timer); delete(S.timer);
            end
        catch
        end

        try
            stop(timerfindall); delete(timerfindall);
        catch
        end

        try
            if ~isempty(S.u), S.u = []; end
            delete(udpportfind);
        catch
        end

        delete(fig);
    end

end
