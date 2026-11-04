(function () {
    'use strict';

    const PALETTE = [
        '#4cc2ff', '#7ef0c0', '#ffc857', '#ff8fab', '#b28dff', '#ff9f6e',
        '#66d9e8', '#a3e635', '#f472b6', '#60a5fa', '#fbbf24', '#34d399',
        '#c084fc', '#fb7185', '#22d3ee', '#facc15'
    ];

    const VIEW_COLORS = [
        '#4cc2ff', '#ffc857', '#7ef0c0', '#ff8fab', '#b28dff', '#ff9f6e',
        '#66d9e8', '#a3e635'
    ];

    const state = {
        data: null,
        times: [],
        series: [],
        stringParams: [],
        views: [],
        nextViewId: 1,
        xRange: { min: 0, max: 1 },
        dataMin: 0,
        dataMax: 1,
        seriesSearch: '',
        hoverTime: null,
        hoveredSeriesId: null,
        defaultYMode: 'normalized',
        sourceName: ''
    };

    const els = {};

    function init() {
        cacheElements();
        bindEvents();

        if (window.Chart) {
            Chart.defaults.color = '#8da0b8';
            Chart.defaults.borderColor = 'rgba(255, 255, 255, 0.08)';
            Chart.defaults.font.family = '"Segoe UI", "Noto Sans SC", "Microsoft YaHei", sans-serif';
        } else {
            setStatus('无法加载 Chart.js（需要联网访问 CDN）。数据仍可解析，但无法绘图。', 'error');
        }

        const params = new URLSearchParams(window.location.search);

        if (params.get('ymode') === 'shared') state.defaultYMode = 'shared';

        els.sampleButton.disabled = !window.AVIONICS_BUS_SAMPLE;
        els.resetZoomButton.disabled = true;

        if (params.get('sample') === '1') loadSample();
    }

    function cacheElements() {
        els.dropZone = document.getElementById('drop-zone');
        els.fileInput = document.getElementById('file-input');
        els.sampleButton = document.getElementById('sample-button');
        els.clearButton = document.getElementById('clear-button');
        els.pasteDetails = document.getElementById('paste-details');
        els.pasteInput = document.getElementById('paste-input');
        els.pasteButton = document.getElementById('paste-button');
        els.metadataSection = document.getElementById('metadata-section');
        els.metaFile = document.getElementById('meta-file');
        els.metaTime = document.getElementById('meta-time');
        els.metaMethod = document.getElementById('meta-method');
        els.metaRate = document.getElementById('meta-rate');
        els.metaCount = document.getElementById('meta-count');
        els.metaDuration = document.getElementById('meta-duration');
        els.metaWarnings = document.getElementById('meta-warnings');
        els.seriesSection = document.getElementById('series-section');
        els.seriesSearch = document.getElementById('series-search');
        els.seriesCount = document.getElementById('series-count');
        els.seriesList = document.getElementById('series-list');
        els.stringSection = document.getElementById('string-section');
        els.stringCount = document.getElementById('string-count');
        els.stringList = document.getElementById('string-list');
        els.addViewButton = document.getElementById('add-view-button');
        els.resetZoomButton = document.getElementById('reset-zoom-button');
        els.chartsContainer = document.getElementById('charts-container');
        els.chartsEmpty = document.getElementById('charts-empty');
        els.statusBar = document.getElementById('status-bar');
    }

    function bindEvents() {
        els.dropZone.addEventListener('click', function () {
            els.fileInput.click();
        });

        els.dropZone.addEventListener('keydown', function (event) {
            if (event.key === 'Enter' || event.key === ' ') {
                event.preventDefault();
                els.fileInput.click();
            }
        });

        els.fileInput.addEventListener('change', function () {
            if (els.fileInput.files && els.fileInput.files.length > 0) {
                handleFile(els.fileInput.files[0]);
            }
            els.fileInput.value = '';
        });

        window.addEventListener('dragover', function (event) {
            event.preventDefault();
            els.dropZone.classList.add('drag-over');
        });

        window.addEventListener('dragleave', function (event) {
            if (event.target === document.documentElement) els.dropZone.classList.remove('drag-over');
        });

        window.addEventListener('drop', function (event) {
            event.preventDefault();
            els.dropZone.classList.remove('drag-over');
            if (event.dataTransfer && event.dataTransfer.files && event.dataTransfer.files.length > 0) {
                handleFile(event.dataTransfer.files[0]);
            }
        });

        els.sampleButton.addEventListener('click', loadSample);
        els.clearButton.addEventListener('click', clearAll);

        els.pasteButton.addEventListener('click', function () {
            loadText(els.pasteInput.value, '粘贴内容');
        });

        els.addViewButton.addEventListener('click', function () {
            addView([], {});
        });

        els.resetZoomButton.addEventListener('click', function () {
            if (!state.data) return;
            resetXRange();
            setStatus('已重置缩放。', 'neutral');
        });

        els.seriesSearch.addEventListener('input', function () {
            state.seriesSearch = els.seriesSearch.value;
            renderSeriesList();
            renderStringList();
        });
    }

    function handleFile(file) {
        if (!file) return;

        if (file.size > 100 * 1024 * 1024) {
            setStatus('文件超过 100 MB，载入可能需要较长时间。', 'warning');
        } else {
            setStatus('正在读取 ' + file.name + ' …', 'neutral');
        }

        file.text().then(function (text) {
            loadText(text, file.name);
        }).catch(function (error) {
            setStatus('读取文件失败：' + error.message, 'error');
        });
    }

    function loadText(text, sourceName) {
        try {
            const parsed = parseJsonText(text);
            loadExport(parsed, sourceName || '未命名');
        } catch (error) {
            setStatus('载入失败：' + error.message, 'error');
        }
    }

    function parseJsonText(text) {
        const trimmed = String(text || '').trim();

        if (!trimmed) throw new Error('内容为空。');

        try {
            return JSON.parse(trimmed);
        } catch (originalError) {
            const first = trimmed.indexOf('{');
            const last = trimmed.lastIndexOf('}');

            if (first >= 0 && last > first) {
                const candidate = trimmed.slice(first, last + 1);

                try {
                    return JSON.parse(candidate);
                } catch (ignored) {
                    throw originalError;
                }
            }

            throw originalError;
        }
    }

    function loadSample() {
        if (!window.AVIONICS_BUS_SAMPLE) {
            setStatus('没有可用的内置示例。', 'error');
            return;
        }

        loadExport(window.AVIONICS_BUS_SAMPLE, '内置示例');
    }

    function loadExport(raw, sourceName) {
        let normalized;

        try {
            normalized = normalizeExport(raw, sourceName);
        } catch (error) {
            setStatus('载入失败：' + error.message, 'error');
            return;
        }

        state.data = normalized;
        state.times = normalized.times;
        state.series = normalized.series;
        state.stringParams = normalized.stringParams;
        state.sourceName = sourceName;
        state.hoveredSeriesId = null;
        state.seriesSearch = '';
        state.dataMin = normalized.times[0];
        state.dataMax = normalized.times[normalized.times.length - 1];

        if (state.dataMax <= state.dataMin) state.dataMax = state.dataMin + 1;

        state.xRange = { min: state.dataMin, max: state.dataMax };
        els.seriesSearch.value = '';

        destroyAllViews();

        renderMetadata(normalized, sourceName);
        renderSeriesList();
        renderStringList();

        els.metadataSection.classList.remove('hidden');
        els.seriesSection.classList.toggle('hidden', normalized.series.length === 0);
        els.stringSection.classList.toggle('hidden', normalized.stringParams.length === 0);
        els.clearButton.disabled = false;
        els.resetZoomButton.disabled = false;
        els.addViewButton.disabled = normalized.series.length === 0;

        const warningText = normalized.warnings.length > 0 ? '，' + normalized.warnings.length + ' 条提示' : '';

        if (normalized.series.length > 0) {
            els.chartsEmpty.classList.add('hidden');
            addView(normalized.series.map(function (series) { return series.id; }), {
                yMode: state.defaultYMode,
                silent: true
            });
            setStatus('已载入 ' + sourceName + '：' + normalized.series.length + ' 条曲线，' + normalized.stringParams.length + ' 个文本参数' + warningText + '。', normalized.warnings.length > 0 ? 'warning' : 'success');
        } else {
            els.chartsEmpty.classList.remove('hidden');
            els.chartsEmpty.querySelector('strong').textContent = '没有数值参数';
            els.chartsEmpty.querySelector('span').textContent = '文本参数可以在左侧查看。';
            setStatus('已载入 ' + sourceName + '：没有数值参数' + warningText + '。', 'warning');
        }
    }

    function normalizeExport(raw, sourceName) {
        if (!raw || typeof raw !== 'object' || Array.isArray(raw)) {
            throw new Error('JSON 根节点必须是对象。');
        }

        if (!Array.isArray(raw.times) || raw.times.length === 0) {
            throw new Error('缺少非空的 times 数组。');
        }

        const times = raw.times.map(function (value) {
            const number = Number(value);
            return Number.isFinite(number) ? number : null;
        });

        if (times.some(function (value) { return value === null; })) {
            throw new Error('times 中存在非数字值。');
        }

        const warnings = [];

        if (raw.schemaVersion !== 1) {
            warnings.push('schemaVersion 为 ' + raw.schemaVersion + '，当前查看器按版本 1 解析。');
        }

        for (let i = 1; i < times.length; i++) {
            if (times[i] < times[i - 1]) {
                warnings.push('times 不是单调递增的，图表可能显示异常。');
                break;
            }
        }

        if (!Array.isArray(raw.parameters)) {
            throw new Error('缺少 parameters 数组。');
        }

        const series = [];
        const stringParams = [];
        let colorIndex = 0;

        raw.parameters.forEach(function (param, paramIndex) {
            if (!param || typeof param !== 'object') {
                warnings.push('第 ' + (paramIndex + 1) + ' 个参数不是对象，已跳过。');
                return;
            }

            const type = String(param.type || '').toLowerCase();
            const key = param.key || (type + ':' + (param.id == null ? '?' : param.id));
            const name = param.name || key;
            const sourceValues = Array.isArray(param.values) ? param.values : [];

            if (sourceValues.length !== times.length) {
                warnings.push(name + ' 的 values 长度为 ' + sourceValues.length + '，与 times 长度 ' + times.length + ' 不一致，已按缺失补 null。');
            }

            const values = new Array(times.length);

            for (let i = 0; i < times.length; i++) {
                values[i] = i < sourceValues.length ? sourceValues[i] : null;
            }

            if (type === 'float' || type === 'int' || type === 'byte' || type === 'number') {
                series.push(makeSeries(key, name, type, values.map(toNumberOrNull), colorIndex++, 'number'));
            } else if (type === 'bool') {
                const booleans = values.map(function (value) {
                    if (value == null) return null;
                    if (typeof value === 'boolean') return value;
                    return Boolean(value);
                });

                const numbers = booleans.map(function (value) {
                    return value == null ? null : (value ? 1 : 0);
                });

                const item = makeSeries(key, name, 'bool', numbers, colorIndex++, 'bool');
                item.rawValues = booleans;
                series.push(item);
            } else if (type === 'vector3') {
                const components = ['x', 'y', 'z'];

                for (let component = 0; component < 3; component++) {
                    const numbers = values.map(function (value) {
                        if (!Array.isArray(value) || value.length <= component) return null;

                        return toNumberOrNull(value[component]);
                    });

                    series.push(makeSeries(key + ':' + components[component], name + '.' + components[component], 'vector3', numbers, colorIndex++, 'number'));
                }
            } else if (type === 'string') {
                stringParams.push({
                    key: key,
                    name: name,
                    values: values.map(function (value) {
                        return value == null ? null : String(value);
                    })
                });
            } else {
                warnings.push(name + ' 的类型 "' + type + '" 未知，按文本参数显示。');
                stringParams.push({
                    key: key,
                    name: name,
                    values: values.map(function (value) {
                        return value == null ? null : String(value);
                    })
                });
            }
        });

        if (series.length === 0 && stringParams.length === 0) {
            throw new Error('没有可显示的参数。');
        }

        series.forEach(computeStats);

        if (raw.overflowed === true) {
            warnings.push('录制时缓冲区已满，数据被提前截断。');
        }

        const duration = Number(raw.durationSeconds);

        return {
            schemaVersion: raw.schemaVersion,
            source: raw.source || 'AvionicsBusRecorder',
            recordedAtUtc: raw.recordedAtUtc || '',
            samplingMethod: raw.samplingMethod || '',
            sampleRateHz: Number(raw.sampleRateHz) || 0,
            sampleCount: Number(raw.sampleCount) || times.length,
            durationSeconds: Number.isFinite(duration) ? duration : times[times.length - 1],
            overflowed: raw.overflowed === true,
            times: times,
            series: series,
            stringParams: stringParams,
            warnings: warnings,
            sourceName: sourceName
        };
    }

    function makeSeries(key, label, type, values, colorIndex, kind) {
        return {
            id: key,
            key: key,
            label: label,
            type: type,
            kind: kind,
            color: colorAt(colorIndex),
            values: values,
            rawValues: null,
            stats: null
        };
    }

    function colorAt(index) {
        if (index < PALETTE.length) return PALETTE[index];

        const hue = (index * 137.508) % 360;

        return 'hsl(' + hue.toFixed(1) + ', 72%, 64%)';
    }

    function toNumberOrNull(value) {
        if (typeof value === 'number') return Number.isFinite(value) ? value : null;
        if (typeof value === 'string' && value.trim() !== '') {
            const number = Number(value);
            return Number.isFinite(number) ? number : null;
        }

        return null;
    }

    function computeStats(series) {
        let min = null;
        let max = null;
        let sum = 0;
        let count = 0;
        let last = null;
        let trueCount = 0;
        let falseCount = 0;

        for (let i = 0; i < series.values.length; i++) {
            const value = series.values[i];

            if (value != null) {
                if (min === null || value < min) min = value;
                if (max === null || value > max) max = value;
                sum += value;
                count++;
                last = value;
            }

            if (series.rawValues) {
                const raw = series.rawValues[i];
                if (raw === true) trueCount++;
                else if (raw === false) falseCount++;
            }
        }

        series.stats = {
            min: min,
            max: max,
            avg: count > 0 ? sum / count : null,
            last: last,
            count: count,
            trueCount: trueCount,
            falseCount: falseCount
        };
    }

    function renderMetadata(data, sourceName) {
        els.metaFile.textContent = sourceName || '—';
        els.metaFile.title = sourceName || '';
        els.metaTime.textContent = formatUtc(data.recordedAtUtc);
        els.metaMethod.textContent = data.samplingMethod || '—';
        els.metaRate.textContent = formatRate(data.sampleRateHz);
        els.metaCount.textContent = String(data.sampleCount);
        els.metaDuration.textContent = formatDuration(data.durationSeconds);

        els.metaWarnings.innerHTML = '';

        if (data.warnings.length > 0) {
            els.metaWarnings.classList.remove('hidden');

            data.warnings.forEach(function (warning) {
                const item = makeEl('div', 'warning-item', warning);
                els.metaWarnings.appendChild(item);
            });
        } else {
            els.metaWarnings.classList.add('hidden');
        }
    }

    function addView(initialSeriesIds, options) {
        if (!state.data || state.series.length === 0) return null;

        const view = createView(initialSeriesIds || [], options || {});
        state.views.push(view);
        renumberViews();
        els.chartsContainer.classList.toggle('single-view', state.views.length === 1);
        renderSeriesList();
        els.chartsEmpty.classList.add('hidden');

        if (!options.silent) {
            setStatus('已新增视图 ' + view.number + '。在左侧参数表点击数字 ' + view.number + ' 添加参数。', 'neutral');
        }

        view.article.scrollIntoView({ behavior: 'smooth', block: 'nearest' });

        return view;
    }

    function createView(initialSeriesIds, options) {
        const view = {
            id: 'view-' + state.nextViewId++,
            number: state.views.length + 1,
            color: VIEW_COLORS[state.views.length % VIEW_COLORS.length],
            seriesIds: new Set(initialSeriesIds),
            yMode: options.yMode === 'shared' ? 'shared' : 'normalized',
            chart: null,
            dragging: false,
            dragStartX: 0,
            dragStartMin: 0,
            dragStartMax: 0,
            article: null,
            titleEl: null,
            dotEl: null,
            countEl: null,
            canvas: null,
            emptyEl: null,
            bodyEl: null,
            crosshairEl: null,
            readoutEl: null,
            readoutTimeEl: null,
            readoutRowsEl: null,
            readoutEntries: [],
            readoutSignature: '',
            removeButton: null,
            yModeSelect: null
        };

        const article = makeEl('article', 'chart-view');
        article.dataset.viewId = view.id;

        const header = makeEl('div', 'view-header');
        const dot = makeEl('span', 'view-dot');
        const title = makeEl('span', 'view-title', '视图 ' + view.number);
        const count = makeEl('span', 'view-count', '0 条曲线');
        const yModeLabel = makeEl('label', 'view-y-mode');
        yModeLabel.appendChild(makeEl('span', null, 'Y 轴'));
        const yModeSelect = makeEl('select');
        const normalizedOption = makeEl('option', null, '归一化');
        normalizedOption.value = 'normalized';
        const sharedOption = makeEl('option', null, '共享轴');
        sharedOption.value = 'shared';
        yModeSelect.appendChild(normalizedOption);
        yModeSelect.appendChild(sharedOption);
        yModeSelect.value = view.yMode;
        yModeLabel.appendChild(yModeSelect);

        const clearButton = makeEl('button', null, '清空');
        clearButton.type = 'button';
        const removeButton = makeEl('button', null, '移除');
        removeButton.type = 'button';

        header.appendChild(dot);
        header.appendChild(title);
        header.appendChild(count);
        header.appendChild(yModeLabel);
        header.appendChild(clearButton);
        header.appendChild(removeButton);

        const body = makeEl('div', 'view-body');
        const canvas = makeEl('canvas', 'view-canvas');
        const empty = makeEl('div', 'view-empty', '');
        const crosshair = makeEl('div', 'view-crosshair hidden');
        const readout = makeEl('div', 'view-readout hidden');
        const readoutTime = makeEl('div', 'view-readout-time');
        const readoutRows = makeEl('div', 'view-readout-rows');
        readout.appendChild(readoutTime);
        readout.appendChild(readoutRows);
        body.appendChild(canvas);
        body.appendChild(empty);
        body.appendChild(crosshair);
        body.appendChild(readout);

        article.appendChild(header);
        article.appendChild(body);
        els.chartsContainer.appendChild(article);

        view.article = article;
        view.titleEl = title;
        view.dotEl = dot;
        view.countEl = count;
        view.canvas = canvas;
        view.emptyEl = empty;
        view.bodyEl = body;
        view.crosshairEl = crosshair;
        view.readoutEl = readout;
        view.readoutTimeEl = readoutTime;
        view.readoutRowsEl = readoutRows;
        view.removeButton = removeButton;
        view.yModeSelect = yModeSelect;

        yModeSelect.addEventListener('change', function () {
            view.yMode = yModeSelect.value === 'shared' ? 'shared' : 'normalized';
            renderView(view);
        });

        clearButton.addEventListener('click', function () {
            view.seriesIds.clear();
            updateViewHeader(view);
            renderView(view);
            renderSeriesList();
            setStatus('已清空视图 ' + view.number + '。', 'neutral');
        });

        removeButton.addEventListener('click', function () {
            removeView(view);
        });

        bindCanvasInteractions(view);
        updateViewHeader(view);
        renderView(view);

        return view;
    }

    function removeView(view) {
        const index = state.views.indexOf(view);

        if (index < 0) return;

        if (state.views.length <= 1) {
            setStatus('至少保留一个视图。', 'warning');
            return;
        }

        if (view.chart) {
            view.chart.destroy();
            view.chart = null;
        }

        view.article.remove();
        state.views.splice(index, 1);
        renumberViews();
        els.chartsContainer.classList.toggle('single-view', state.views.length === 1);
        renderSeriesList();
        setStatus('已移除视图 ' + (index + 1) + '。', 'neutral');
    }

    function renumberViews() {
        state.views.forEach(function (view, index) {
            view.number = index + 1;
            view.color = VIEW_COLORS[index % VIEW_COLORS.length];
            updateViewHeader(view);
        });
    }

    function updateViewHeader(view) {
        view.titleEl.textContent = '视图 ' + view.number;
        view.dotEl.style.backgroundColor = view.color;
        view.countEl.textContent = view.seriesIds.size + ' 条曲线';
        view.removeButton.disabled = state.views.length <= 1;
        view.yModeSelect.value = view.yMode;
        updateViewEmptyText(view);
    }

    function updateViewEmptyText(view) {
        let text = '';

        if (!state.data) text = '还没有数据';
        else if (state.series.length === 0) text = '没有数值参数';
        else if (!window.Chart) text = 'Chart.js 未加载，请检查网络后刷新';
        else if (view.seriesIds.size === 0) text = '在左侧参数表点击数字 ' + view.number + ' 添加参数';

        view.emptyEl.textContent = text;
        view.emptyEl.classList.toggle('hidden', text === '');
    }

    function destroyAllViews() {
        state.views.forEach(function (view) {
            if (view.chart) view.chart.destroy();
            if (view.article) view.article.remove();
        });

        state.views = [];
        state.nextViewId = 1;
        state.hoverTime = null;
        els.chartsContainer.classList.remove('single-view');
    }

    function renderView(view) {
        if (view.chart) {
            view.chart.destroy();
            view.chart = null;
        }

        view.dragging = false;
        view.canvas.classList.remove('dragging');
        view.crosshairEl.classList.add('hidden');
        view.readoutEl.classList.add('hidden');
        view.readoutEntries = [];
        view.readoutSignature = '';
        updateViewEmptyText(view);

        if (!state.data || !window.Chart) return;

        const selected = getViewSeries(view);

        if (selected.length === 0) return;

        view.emptyEl.classList.add('hidden');

        try {
            view.chart = new Chart(view.canvas.getContext('2d'), {
                type: 'line',
                data: { datasets: buildDatasets(view, selected) },
                options: buildChartOptions(view)
            });
        } catch (error) {
            view.chart = null;
            view.emptyEl.textContent = '图表创建失败：' + error.message;
            view.emptyEl.classList.remove('hidden');
            return;
        }

        applyHoverToView(view);

        if (state.hoverTime != null) updateHoverVisuals();
    }

    function getViewSeries(view) {
        return state.series.filter(function (series) {
            return view.seriesIds.has(series.id);
        });
    }

    function buildDatasets(view, selected) {
        const times = state.times;
        const normalized = view.yMode === 'normalized';

        return selected.map(function (series) {
            const stats = series.stats;
            const flat = stats.min != null && stats.max != null && stats.min === stats.max;
            const data = new Array(times.length);

            for (let i = 0; i < times.length; i++) {
                const raw = series.rawValues ? series.rawValues[i] : series.values[i];
                const numeric = series.values[i];
                let y = null;

                if (numeric != null) {
                    if (normalized) {
                        y = flat ? 0.5 : (numeric - stats.min) / (stats.max - stats.min);
                    } else {
                        y = numeric;
                    }
                }

                data[i] = { x: times[i], y: y, raw: raw };
            }

            return {
                label: series.label,
                data: data,
                borderColor: series.color,
                backgroundColor: series.color,
                borderWidth: 1.6,
                pointRadius: 0,
                pointHitRadius: 8,
                tension: 0,
                spanGaps: false,
                stepped: series.kind === 'bool',
                _seriesId: series.id,
                _kind: series.kind
            };
        });
    }

    function buildChartOptions(view) {
        const normalized = view.yMode === 'normalized';

        return {
            responsive: true,
            maintainAspectRatio: false,
            animation: false,
            normalized: true,
            parsing: false,
            interaction: { mode: 'index', intersect: false },
            layout: { padding: { left: 6, right: 12, top: 6, bottom: 0 } },
            scales: {
                x: {
                    type: 'linear',
                    min: state.xRange.min,
                    max: state.xRange.max,
                    title: { display: false },
                    ticks: {
                        callback: function (value) { return formatTime(value); },
                        maxRotation: 0,
                        autoSkipPadding: 16
                    },
                    grid: { color: 'rgba(255, 255, 255, 0.055)' }
                },
                y: {
                    type: 'linear',
                    beginAtZero: false,
                    grace: normalized ? 0 : '5%',
                    min: normalized ? 0 : undefined,
                    max: normalized ? 1 : undefined,
                    title: { display: false },
                    ticks: {
                        callback: function (value) {
                            return normalized ? Number(value).toFixed(2) : formatNumber(value);
                        }
                    },
                    grid: { color: 'rgba(255, 255, 255, 0.055)' }
                }
            },
            plugins: {
                legend: { display: false },
                decimation: {
                    enabled: state.times.length > 20000,
                    algorithm: 'lttb',
                    samples: 4000
                },
                tooltip: { enabled: false }
            }
        };
    }

    function updateAllChartsXRange() {
        state.views.forEach(function (view) {
            if (!view.chart) return;

            view.chart.options.scales.x.min = state.xRange.min;
            view.chart.options.scales.x.max = state.xRange.max;
            view.chart.update('none');
        });
    }

    function setXRange(min, max) {
        const fullMin = state.dataMin;
        const fullMax = state.dataMax;
        const fullSpan = fullMax - fullMin;

        if (!(fullSpan > 0)) return;

        let nextMin = Number(min);
        let nextMax = Number(max);

        if (!Number.isFinite(nextMin) || !Number.isFinite(nextMax)) return;

        const minSpan = Math.max(fullSpan / 20000, 1e-6);
        let span = nextMax - nextMin;

        if (span < minSpan) {
            const center = (nextMin + nextMax) / 2;
            nextMin = center - minSpan / 2;
            nextMax = center + minSpan / 2;
            span = minSpan;
        }

        if (span > fullSpan) {
            nextMin = fullMin;
            nextMax = fullMax;
        } else {
            if (nextMin < fullMin) {
                nextMax += fullMin - nextMin;
                nextMin = fullMin;
            }

            if (nextMax > fullMax) {
                nextMin -= nextMax - fullMax;
                nextMax = fullMax;
            }

            if (nextMin < fullMin) nextMin = fullMin;
            if (nextMax > fullMax) nextMax = fullMax;

            if (nextMax - nextMin < minSpan) {
                if (nextMin <= fullMin) {
                    nextMin = fullMin;
                    nextMax = Math.min(fullMax, fullMin + minSpan);
                } else {
                    nextMax = fullMax;
                    nextMin = Math.max(fullMin, fullMax - minSpan);
                }
            }
        }

        state.xRange = { min: nextMin, max: nextMax };
        updateAllChartsXRange();
    }

    function resetXRange() {
        if (!state.data) return;

        state.xRange = { min: state.dataMin, max: state.dataMax };
        updateAllChartsXRange();
    }

    function bindCanvasInteractions(view) {
        view.canvas.addEventListener('pointerdown', function (event) {
            if (!view.chart || event.button !== 0) return;

            clearHover();
            view.dragging = true;
            view.dragStartX = event.clientX;
            view.dragStartMin = state.xRange.min;
            view.dragStartMax = state.xRange.max;
            view.canvas.classList.add('dragging');

            try {
                view.canvas.setPointerCapture(event.pointerId);
            } catch (ignored) {
            }

            event.preventDefault();
        });

        view.canvas.addEventListener('pointermove', function (event) {
            if (!view.dragging || !view.chart || !view.chart.chartArea) return;

            const plotWidth = view.chart.chartArea.right - view.chart.chartArea.left;

            if (plotWidth <= 0) return;

            const span = view.dragStartMax - view.dragStartMin;
            const delta = (view.dragStartX - event.clientX) / plotWidth * span;

            setXRange(view.dragStartMin + delta, view.dragStartMax + delta);
            event.preventDefault();
        });

        view.canvas.addEventListener('pointermove', function (event) {
            if (view.dragging) return;

            updateHoverFromEvent(view, event);
        });

        function endPan(event) {
            if (!view.dragging) return;

            view.dragging = false;
            view.canvas.classList.remove('dragging');

            try {
                if (view.canvas.hasPointerCapture(event.pointerId)) {
                    view.canvas.releasePointerCapture(event.pointerId);
                }
            } catch (ignored) {
            }

            if (event.clientX != null) updateHoverFromEvent(view, event);
        }

        view.canvas.addEventListener('pointerup', endPan);
        view.canvas.addEventListener('pointercancel', endPan);
        view.canvas.addEventListener('lostpointercapture', function () {
            view.dragging = false;
            view.canvas.classList.remove('dragging');
        });

        view.bodyEl.addEventListener('pointerleave', function () {
            clearHover();
        });

        view.canvas.addEventListener('wheel', function (event) {
            if (!view.chart || event.deltaY === 0) return;

            event.preventDefault();

            const rect = view.canvas.getBoundingClientRect();
            const pixelX = event.clientX - rect.left;
            const value = view.chart.scales.x.getValueForPixel(pixelX);

            if (!Number.isFinite(value)) return;

            const factor = event.deltaY < 0 ? 0.8 : 1.25;
            const min = state.xRange.min;
            const max = state.xRange.max;

            setXRange(value - (value - min) * factor, value + (max - value) * factor);
            updateHoverFromEvent(view, event);
        }, { passive: false });

        view.canvas.addEventListener('dblclick', function (event) {
            resetXRange();
            updateHoverFromEvent(view, event);
        });
    }

    function updateHoverFromEvent(view, event) {
        if (!view.chart || !view.chart.chartArea || !state.data) {
            clearHover();
            return;
        }

        const rect = view.canvas.getBoundingClientRect();
        const pixelX = event.clientX - rect.left;
        const area = view.chart.chartArea;

        if (pixelX < area.left || pixelX > area.right) {
            clearHover();
            return;
        }

        const value = view.chart.scales.x.getValueForPixel(pixelX);

        if (!Number.isFinite(value)) {
            clearHover();
            return;
        }

        state.hoverTime = Math.max(state.xRange.min, Math.min(state.xRange.max, value));
        updateHoverVisuals();
    }

    function clearHover() {
        if (state.hoverTime == null) return;

        state.hoverTime = null;
        updateHoverVisuals();
    }

    function updateHoverVisuals() {
        const time = state.hoverTime;

        if (time == null || !state.data) {
            state.views.forEach(hideHoverForView);
            return;
        }

        const index = nearestSampleIndex(time);

        state.views.forEach(function (view) {
            if (!view.chart || !view.chart.chartArea || view.seriesIds.size === 0) {
                hideHoverForView(view);
                return;
            }

            const area = view.chart.chartArea;
            const pixel = view.chart.scales.x.getPixelForValue(time);

            if (!Number.isFinite(pixel) || pixel < area.left - 1 || pixel > area.right + 1) {
                hideHoverForView(view);
                return;
            }

            view.crosshairEl.style.left = pixel + 'px';
            view.crosshairEl.style.top = area.top + 'px';
            view.crosshairEl.style.height = Math.max(0, area.bottom - area.top) + 'px';
            view.crosshairEl.classList.remove('hidden');
            view.readoutEl.classList.remove('hidden');
            view.readoutTimeEl.textContent = 't = ' + formatTime(time);

            const selected = getViewSeries(view);
            const signature = selected.map(function (series) { return series.id; }).join('|');

            if (view.readoutSignature !== signature) {
                buildReadoutRows(view, selected);
                view.readoutSignature = signature;
            }

            view.readoutEntries.forEach(function (entry) {
                const raw = entry.series.rawValues ? entry.series.rawValues[index] : entry.series.values[index];
                entry.valueEl.textContent = formatValue(raw, entry.series.kind);
            });
        });
    }

    function hideHoverForView(view) {
        if (view.crosshairEl) view.crosshairEl.classList.add('hidden');
        if (view.readoutEl) view.readoutEl.classList.add('hidden');
    }

    function buildReadoutRows(view, selected) {
        view.readoutRowsEl.innerHTML = '';
        view.readoutEntries = [];

        selected.forEach(function (series) {
            const row = makeEl('div', 'view-readout-row');
            const swatch = makeEl('span', 'view-readout-swatch');
            swatch.style.backgroundColor = series.color;
            const name = makeEl('span', 'view-readout-name', series.label);
            const value = makeEl('span', 'view-readout-value', '—');

            row.appendChild(swatch);
            row.appendChild(name);
            row.appendChild(value);
            view.readoutRowsEl.appendChild(row);
            view.readoutEntries.push({ series: series, valueEl: value });
        });
    }

    function nearestSampleIndex(time) {
        const times = state.times;

        if (times.length === 0) return 0;
        if (time <= times[0]) return 0;
        if (time >= times[times.length - 1]) return times.length - 1;

        let low = 0;
        let high = times.length - 1;

        while (low <= high) {
            const mid = (low + high) >> 1;

            if (times[mid] === time) return mid;
            if (times[mid] < time) low = mid + 1;
            else high = mid - 1;
        }

        const right = Math.min(low, times.length - 1);
        const left = Math.max(high, 0);

        return (time - times[left] <= times[right] - time) ? left : right;
    }

    function toggleSeriesInView(series, view) {
        if (view.seriesIds.has(series.id)) {
            view.seriesIds.delete(series.id);
        } else {
            view.seriesIds.add(series.id);
        }

        updateViewHeader(view);
        renderView(view);
        renderSeriesList();
    }

    function renderSeriesList() {
        if (state.hoveredSeriesId != null) {
            state.hoveredSeriesId = null;
            applyHover();
        }

        els.seriesList.innerHTML = '';

        const total = state.series.length;
        const query = state.seriesSearch.trim().toLowerCase();
        const filtered = state.series.filter(function (series) {
            return !query || seriesMatches(series, query);
        });

        els.seriesCount.textContent = query ? filtered.length + ' / ' + total + ' 条' : total + ' 条';

        if (total === 0) {
            els.seriesList.appendChild(makeEl('div', 'series-empty', '没有数值参数。'));
            return;
        }

        if (filtered.length === 0) {
            els.seriesList.appendChild(makeEl('div', 'series-empty', '没有匹配的参数。'));
            return;
        }

        filtered.forEach(function (series) {
            const row = makeEl('div', 'series-row');
            row.dataset.seriesId = series.id;

            const swatch = makeEl('span', 'swatch');
            swatch.style.backgroundColor = series.color;

            const main = makeEl('div', 'series-main');
            const title = makeEl('div', 'series-title');
            const name = makeEl('span', 'series-name', series.label);
            name.title = series.label;
            const badge = makeEl('span', 'type-badge', series.type);
            const stats = makeEl('div', 'series-stats', formatSeriesStats(series));
            const chips = makeEl('div', 'view-chips');

            state.views.forEach(function (view) {
                const chip = makeEl('button', 'view-chip', String(view.number));
                chip.type = 'button';
                chip.dataset.viewNumber = String(view.number);
                chip.style.setProperty('--chip-color', view.color);
                chip.title = (view.seriesIds.has(series.id) ? '从视图 ' : '加入视图 ') + view.number;
                chip.setAttribute('aria-label', chip.title + '：' + series.label);
                chip.setAttribute('aria-pressed', view.seriesIds.has(series.id) ? 'true' : 'false');
                chip.classList.toggle('active', view.seriesIds.has(series.id));
                chip.addEventListener('click', function (event) {
                    event.stopPropagation();
                    toggleSeriesInView(series, view);
                });
                chips.appendChild(chip);
            });

            title.appendChild(name);
            title.appendChild(badge);
            main.appendChild(title);
            main.appendChild(stats);
            main.appendChild(chips);
            row.appendChild(swatch);
            row.appendChild(main);

            row.addEventListener('mouseenter', function () {
                state.hoveredSeriesId = series.id;
                applyHover();
            });

            row.addEventListener('mouseleave', function () {
                state.hoveredSeriesId = null;
                applyHover();
            });

            els.seriesList.appendChild(row);
        });
    }

    function seriesMatches(series, query) {
        const haystack = (series.label + ' ' + series.key + ' ' + series.type + ' ' + series.id).toLowerCase();

        return haystack.indexOf(query) >= 0;
    }

    function formatSeriesStats(series) {
        const stats = series.stats;

        if (!stats) return '';

        if (series.kind === 'bool') {
            return 'true ' + stats.trueCount + ' · false ' + stats.falseCount;
        }

        if (stats.count === 0) return '无有效数值';

        return 'min ' + formatNumber(stats.min) + ' · max ' + formatNumber(stats.max) + ' · avg ' + formatNumber(stats.avg);
    }

    function renderStringList() {
        els.stringList.innerHTML = '';

        const total = state.stringParams.length;
        const query = state.seriesSearch.trim().toLowerCase();
        const filtered = state.stringParams.filter(function (param) {
            if (!query) return true;

            return (param.name + ' ' + param.key).toLowerCase().indexOf(query) >= 0;
        });

        els.stringCount.textContent = query ? filtered.length + ' / ' + total + ' 个' : total + ' 个';

        if (total === 0) return;

        if (filtered.length === 0) {
            els.stringList.appendChild(makeEl('div', 'string-empty', '没有匹配的文本参数。'));
            return;
        }

        filtered.forEach(function (param) {
            const row = makeEl('div', 'string-row');
            const name = makeEl('div', 'string-name', param.name);
            const lastValue = lastNonNull(param.values);
            const last = makeEl('div', 'string-last', '最后值：' + (lastValue == null ? '—' : lastValue));

            row.appendChild(name);
            row.appendChild(last);

            const changes = buildChanges(param.values, state.times);

            if (changes.length > 0) {
                const details = makeEl('details');
                const summary = makeEl('summary', null, '变化记录 (' + changes.length + ')');
                const list = makeEl('div', 'change-list');

                changes.slice(0, 200).forEach(function (change) {
                    const item = makeEl('div', 'change-item');
                    const time = makeEl('span', 'change-time', formatTime(change.time));
                    const value = makeEl('span', null, change.value == null ? '—' : change.value);

                    item.appendChild(time);
                    item.appendChild(value);
                    list.appendChild(item);
                });

                if (changes.length > 200) {
                    list.appendChild(makeEl('div', 'change-item', '… 还有 ' + (changes.length - 200) + ' 条'));
                }

                details.appendChild(summary);
                details.appendChild(list);
                row.appendChild(details);
            }

            els.stringList.appendChild(row);
        });
    }

    function buildChanges(values, times) {
        const changes = [];
        let previous = undefined;

        for (let i = 0; i < values.length; i++) {
            if (i === 0 || values[i] !== previous) {
                changes.push({ time: times[i], value: values[i] });
            }

            previous = values[i];
        }

        return changes;
    }

    function lastNonNull(values) {
        for (let i = values.length - 1; i >= 0; i--) {
            if (values[i] != null) return values[i];
        }

        return null;
    }

    function applyHover() {
        state.views.forEach(applyHoverToView);
    }

    function applyHoverToView(view) {
        if (!view.chart) return;

        const hoveredInView = state.hoveredSeriesId != null && view.seriesIds.has(state.hoveredSeriesId);

        view.chart.data.datasets.forEach(function (dataset) {
            const hovered = dataset._seriesId === state.hoveredSeriesId;
            const dimmed = hoveredInView && !hovered;

            dataset.borderWidth = hovered ? 3 : 1.6;
            dataset.borderColor = dimmed ? withAlpha(dataset.backgroundColor, 0.25) : dataset.backgroundColor;
        });

        view.chart.update('none');
    }

    function withAlpha(color, alpha) {
        if (color.charAt(0) !== '#') return color;

        const hex = color.slice(1);

        if (hex.length !== 6) return color;

        const r = parseInt(hex.slice(0, 2), 16);
        const g = parseInt(hex.slice(2, 4), 16);
        const b = parseInt(hex.slice(4, 6), 16);

        return 'rgba(' + r + ', ' + g + ', ' + b + ', ' + alpha + ')';
    }

    function makeEl(tag, className, text) {
        const node = document.createElement(tag);

        if (className) node.className = className;
        if (text != null) node.textContent = text;

        return node;
    }

    function formatValue(value, kind) {
        if (value == null) return '—';
        if (kind === 'bool') return value ? 'true' : 'false';

        return formatNumber(value);
    }

    function formatNumber(value) {
        if (value == null || !Number.isFinite(Number(value))) return '—';

        const number = Number(value);
        const abs = Math.abs(number);

        if (abs !== 0 && (abs < 0.001 || abs >= 1000000)) {
            return number.toExponential(3);
        }

        return number.toLocaleString('zh-CN', { maximumFractionDigits: 4 });
    }

    function formatTime(seconds) {
        if (!Number.isFinite(Number(seconds))) return '—';

        const number = Number(seconds);

        if (number >= 60) {
            const minutes = Math.floor(number / 60);
            const remaining = number - minutes * 60;

            return minutes + ':' + remaining.toFixed(1).padStart(4, '0');
        }

        return number.toFixed(2) + ' s';
    }

    function formatDuration(seconds) {
        if (!Number.isFinite(Number(seconds))) return '—';

        const number = Number(seconds);

        if (number >= 60) {
            const minutes = Math.floor(number / 60);
            const remaining = number - minutes * 60;

            return minutes + ' 分 ' + remaining.toFixed(1) + ' 秒';
        }

        return number.toFixed(2) + ' 秒';
    }

    function formatRate(rate) {
        if (!Number.isFinite(Number(rate)) || Number(rate) <= 0) return '每次回调';

        return Number(rate) + ' Hz';
    }

    function formatUtc(value) {
        if (!value) return '—';

        const date = new Date(value);

        if (Number.isNaN(date.getTime())) return String(value);

        return date.toLocaleString();
    }

    function setStatus(message, type) {
        els.statusBar.textContent = message;
        els.statusBar.className = 'status-bar ' + (type || 'neutral');
    }

    function clearAll() {
        destroyAllViews();

        state.data = null;
        state.times = [];
        state.series = [];
        state.stringParams = [];
        state.hoveredSeriesId = null;
        state.seriesSearch = '';
        state.dataMin = 0;
        state.dataMax = 1;
        state.xRange = { min: 0, max: 1 };

        els.metadataSection.classList.add('hidden');
        els.seriesSection.classList.add('hidden');
        els.stringSection.classList.add('hidden');
        els.seriesList.innerHTML = '';
        els.stringList.innerHTML = '';
        els.metaWarnings.innerHTML = '';
        els.seriesSearch.value = '';
        els.seriesCount.textContent = '';
        els.stringCount.textContent = '';
        els.clearButton.disabled = true;
        els.addViewButton.disabled = true;
        els.resetZoomButton.disabled = true;

        els.chartsEmpty.classList.remove('hidden');
        els.chartsEmpty.querySelector('strong').textContent = '还没有数据';
        els.chartsEmpty.querySelector('span').textContent = '拖放导出的 JSON，或点击左侧“载入示例”。';

        setStatus('等待载入 JSON。', 'neutral');
    }

    document.addEventListener('DOMContentLoaded', init);
})();
