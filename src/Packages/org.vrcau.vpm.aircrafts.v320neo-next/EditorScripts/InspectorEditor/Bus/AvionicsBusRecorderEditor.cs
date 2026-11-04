using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using VAU.V320NeoNext.Runtime.Bus;
using VRC.SDK3.Data;
using VRC.Udon;

namespace VAU.V320NeoNext.Editor.InspectorEditor.Bus
{
    [CustomEditor(typeof(AvionicsBusRecorder))]
    public sealed class AvionicsBusRecorderEditor : UnityEditor.Editor
    {
        private const int StateIdle = (int)AvionicsBusRecorderState.Idle;
        private const int StateRecording = (int)AvionicsBusRecorderState.Recording;
        private const int StateFinished = (int)AvionicsBusRecorderState.Finished;
        private const int StateError = (int)AvionicsBusRecorderState.Error;
        private const int MaxSearchResults = 12;
        private const int MaxSamplesLimit = 1000000;

        private string _search = "";
        private int _typeFilter;
        private string _message = "";
        private MessageType _messageType = MessageType.None;
        private string _lastSavedPath = "";
        private double _nextRepaintTime;

        private void OnEnable()
        {
            _search = "";
            _typeFilter = 0;
            _message = "";
            _messageType = MessageType.None;
            _lastSavedPath = "";
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            AvionicsBusRecorder recorder = (AvionicsBusRecorder)target;

            EditorGUILayout.LabelField("AvionicsBus 参数录制", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _DrawStatus(recorder);
            EditorGUILayout.Space();

            _DrawConfig(recorder);
            EditorGUILayout.Space();

            _DrawParameters(recorder);
            EditorGUILayout.Space();

            _DrawActions(recorder);

            if (Application.isPlaying && _GetState(recorder) == StateRecording)
            {
                double now = EditorApplication.timeSinceStartup;

                if (now >= _nextRepaintTime)
                {
                    _nextRepaintTime = now + 0.1;
                    Repaint();
                }
            }
        }

        private void _DrawStatus(AvionicsBusRecorder recorder)
        {
            UdonBehaviour udon = _GetUdonBehaviour(recorder);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Udon 在编辑模式下不执行。进入 Play mode 后才能开始采样。", MessageType.Info);
                return;
            }

            if (udon == null)
            {
                EditorGUILayout.HelpBox("找不到 UdonBehaviour，请先编译 UdonSharp 程序资产。", MessageType.Error);
                return;
            }

            int state = _GetInt(udon, "_recordingState");
            int sampleCount = _GetInt(udon, "_sampleCount");
            double duration = _GetDouble(udon, "_recordedDuration");
            bool overflowed = _GetBool(udon, "_overflowed");
            string lastError = _GetString(udon, "_lastError");
            string json = _GetString(udon, "exportedJson");

            EditorGUILayout.LabelField("状态", _StateName(state));

            EditorGUILayout.LabelField("样本数", sampleCount.ToString(CultureInfo.InvariantCulture));
            EditorGUILayout.LabelField("已记录时长", duration.ToString("F3", CultureInfo.InvariantCulture) + " s");
            EditorGUILayout.LabelField("JSON 大小", _FormatBytes(json.Length == 0 ? 0 : Encoding.UTF8.GetByteCount(json)));

            if (overflowed)
            {
                EditorGUILayout.HelpBox("缓冲区已满，采样已自动停止并导出了已记录的数据。", MessageType.Warning);
            }

            if (state == StateError && !string.IsNullOrEmpty(lastError))
            {
                EditorGUILayout.HelpBox(lastError, MessageType.Error);
            }
        }

        private void _DrawConfig(AvionicsBusRecorder recorder)
        {
            EditorGUILayout.LabelField("采样配置", EditorStyles.boldLabel);

            bool editable = !Application.isPlaying;

            using (new EditorGUI.DisabledScope(!editable))
            {
                EditorGUI.BeginChangeCheck();

                AvionicsBusSamplingMethod samplingMethod = (AvionicsBusSamplingMethod)EditorGUILayout.EnumPopup(
                    "采样方式", recorder.samplingMethod);
                float sampleRateHz = EditorGUILayout.FloatField("采样率 (Hz)", recorder.sampleRateHz);
                int maxSamples = EditorGUILayout.IntField("最大样本数", recorder.maxSamples);
                bool autoStart = EditorGUILayout.Toggle("Bus 启动时自动开始", recorder.autoStartOnBusStart);
                float autoStop = EditorGUILayout.FloatField("自动停止 (秒)", recorder.autoStopAfterSeconds);
                bool logJson = EditorGUILayout.Toggle("输出到 Debug.Log", recorder.logExportedJson);

                if (EditorGUI.EndChangeCheck())
                {
                    if (sampleRateHz < 0f || float.IsNaN(sampleRateHz) || float.IsInfinity(sampleRateHz)) sampleRateHz = 0f;
                    if (maxSamples < 1) maxSamples = 1;
                    if (maxSamples > MaxSamplesLimit) maxSamples = MaxSamplesLimit;
                    if (autoStop < 0f || float.IsNaN(autoStop) || float.IsInfinity(autoStop)) autoStop = 0f;

                    Undo.RecordObject(recorder, "Edit AvionicsBusRecorder settings");
                    recorder.samplingMethod = samplingMethod;
                    recorder.sampleRateHz = sampleRateHz;
                    recorder.maxSamples = maxSamples;
                    recorder.autoStartOnBusStart = autoStart;
                    recorder.autoStopAfterSeconds = autoStop;
                    recorder.logExportedJson = logJson;
                    EditorUtility.SetDirty(recorder);
                    UdonSharpEditorUtility.CopyProxyToUdon(recorder);
                }
            }

            if (editable)
            {
                EditorGUILayout.HelpBox("采样率 0 表示每次回调都采样。autoStart 在 Bus 启动/重生时触发；重生不会中断正在进行的采样。", MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox("Play mode 下采样配置只读。运行时请使用 AvionicsBusRecorder 的公开 API。", MessageType.None);
            }

            _DrawCapacityEstimate(recorder);

            EditorGUILayout.Space();
            _DrawPath(recorder);
        }

        private void _DrawCapacityEstimate(AvionicsBusRecorder recorder)
        {
            int[] types = _GetTypes(recorder);
            int[] ids = _GetIds(recorder);

            int count = types.Length < ids.Length ? types.Length : ids.Length;
            int floatCount = 0;
            int byteCount = 0;
            int intCount = 0;
            int boolCount = 0;
            int stringCount = 0;
            int vector3Count = 0;

            for (int i = 0; i < count; i++)
            {
                int type = types[i];

                if (type == (int)AvionicsBusDataType.Float) floatCount++;
                else if (type == (int)AvionicsBusDataType.Byte) byteCount++;
                else if (type == (int)AvionicsBusDataType.Int) intCount++;
                else if (type == (int)AvionicsBusDataType.Bool) boolCount++;
                else if (type == (int)AvionicsBusDataType.String) stringCount++;
                else if (type == (int)AvionicsBusDataType.Vector3) vector3Count++;
            }

            int capacity = recorder.maxSamples;
            if (capacity < 1) capacity = 1;
            if (capacity > MaxSamplesLimit) capacity = MaxSamplesLimit;

            long bytesPerSample = 8L + floatCount * 4L + byteCount + intCount * 4L + boolCount + stringCount * 8L + vector3Count * 12L;
            long totalBytes = bytesPerSample * capacity;

            EditorGUILayout.LabelField("估算缓冲", _FormatBytes(totalBytes) + "（" + capacity + " 样本，按 60Hz 约 " + (capacity / 60) + " 秒）");

            if (totalBytes > 50L * 1024L * 1024L)
            {
                EditorGUILayout.HelpBox("缓冲估算超过 50 MB，建议降低最大样本数或减少导出参数。", MessageType.Warning);
            }
        }

        private void _DrawPath(AvionicsBusRecorder recorder)
        {
            EditorGUILayout.LabelField("导出路径", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            string path = EditorGUILayout.TextField("路径", recorder.editorExportPath);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(recorder, "Edit AvionicsBusRecorder export path");
                recorder.editorExportPath = path;
                EditorUtility.SetDirty(recorder);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("浏览…"))
                {
                    string selected = EditorUtility.SaveFilePanel("导出 AvionicsBus JSON", _DefaultExportDirectory(), "avionics-bus-export.json", "json");

                    if (!string.IsNullOrEmpty(selected))
                    {
                        Undo.RecordObject(recorder, "Edit AvionicsBusRecorder export path");
                        recorder.editorExportPath = _ToProjectRelative(selected);
                        EditorUtility.SetDirty(recorder);
                    }
                }

                using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(recorder.editorExportPath)))
                {
                    if (GUILayout.Button("显示所在文件夹"))
                    {
                        string fullPath = _ResolvePath(recorder.editorExportPath);
                        if (!string.IsNullOrEmpty(fullPath)) EditorUtility.RevealInFinder(fullPath);
                    }
                }
            }

            string resolved = _ResolvePath(recorder.editorExportPath);

            if (string.IsNullOrEmpty(resolved))
            {
                EditorGUILayout.HelpBox("未设置路径时，保存会弹出文件选择框。相对路径基于 Unity 项目根目录。", MessageType.None);
            }
            else
            {
                EditorGUILayout.LabelField("实际路径", resolved, EditorStyles.miniLabel);

                if (resolved.StartsWith(Application.dataPath, StringComparison.OrdinalIgnoreCase))
                {
                    EditorGUILayout.HelpBox("路径位于 Assets/ 下，保存后 Unity 会导入该 JSON 文件。建议改到 Assets/ 外。", MessageType.Warning);
                }
            }
        }

        private void _DrawParameters(AvionicsBusRecorder recorder)
        {
            int[] types = _GetTypes(recorder);
            int[] ids = _GetIds(recorder);
            string[] names = _GetNames(recorder);

            EditorGUILayout.LabelField("导出参数 (" + types.Length + ")", EditorStyles.boldLabel);

            if (types.Length != ids.Length)
            {
                EditorGUILayout.HelpBox("exportDataTypes 与 exportIds 长度不一致，请同步或清空后重新添加。", MessageType.Warning);
            }

            if (types.Length > names.Length)
            {
                EditorGUILayout.HelpBox("exportNames 比参数列表短，缺失项会使用 类型:id 作为名称。", MessageType.Warning);
            }

            if (!Application.isPlaying)
            {
                _DrawSearch(recorder, types, ids);
            }
            else
            {
                EditorGUILayout.HelpBox("Play mode 下参数列表只读。运行时请通过公开 API 修改。", MessageType.None);
            }

            int count = types.Length < ids.Length ? types.Length : ids.Length;

            if (count == 0)
            {
                EditorGUILayout.LabelField("（还没有选择参数，用上面的搜索框添加）", EditorStyles.miniLabel);
                return;
            }

            for (int i = 0; i < count; i++)
            {
                if (_DrawParameterRow(recorder, i, types[i], ids[i], i < names.Length ? names[i] : "")) return;
            }

            if (!Application.isPlaying)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("同步参数名称")) _SyncNames(recorder);
                    if (GUILayout.Button("清空参数")) _ClearParameters(recorder);
                }
            }
        }

        private void _DrawSearch(AvionicsBusRecorder recorder, int[] types, int[] ids)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField);
                _typeFilter = EditorGUILayout.Popup(_typeFilter, AvionicsBusEditorUtil.TypeFilterLabels, GUILayout.Width(80f));
            }

            List<AvionicsBusEditorUtil.DataIdInfo> matches = _FindMatches(types, ids);

            if (matches.Count == 0)
            {
                EditorGUILayout.LabelField(string.IsNullOrEmpty(_search) ? "（输入关键字搜索数据 id）" : "没有匹配的数据 id", EditorStyles.miniLabel);
                return;
            }

            int shown = Mathf.Min(matches.Count, MaxSearchResults);

            for (int i = 0; i < shown; i++)
            {
                AvionicsBusEditorUtil.DataIdInfo info = matches[i];

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(AvionicsBusEditorUtil.TypeName(info.DataType), GUILayout.Width(48f));
                    EditorGUILayout.LabelField(info.Name);

                    if (GUILayout.Button("+", GUILayout.Width(24f)))
                    {
                        _AddParameter(recorder, info.DataType, info.Id, info.Name);
                        GUI.FocusControl(null);
                        return;
                    }
                }
            }

            if (matches.Count > shown)
            {
                EditorGUILayout.LabelField("（还有 " + (matches.Count - shown) + " 条，继续输入关键字缩小范围）", EditorStyles.miniLabel);
            }
        }

        private List<AvionicsBusEditorUtil.DataIdInfo> _FindMatches(int[] types, int[] ids)
        {
            List<AvionicsBusEditorUtil.DataIdInfo> matches = new List<AvionicsBusEditorUtil.DataIdInfo>();

            foreach (AvionicsBusEditorUtil.DataIdInfo info in AvionicsBusEditorUtil.GetCatalog())
            {
                if (_typeFilter > 0 && info.DataType != _typeFilter - 1) continue;
                if (_IsSelected(types, ids, info)) continue;
                if (!string.IsNullOrEmpty(_search) && info.Name.IndexOf(_search, StringComparison.OrdinalIgnoreCase) < 0) continue;

                matches.Add(info);
            }

            return matches;
        }

        private bool _DrawParameterRow(AvionicsBusRecorder recorder, int index, int dataType, int id, string storedName)
        {
            bool known = AvionicsBusEditorUtil.TryGetDataIdInfo(dataType, id, out AvionicsBusEditorUtil.DataIdInfo info);
            string name = known ? info.Name : (string.IsNullOrEmpty(storedName) ? "<未知参数>" : storedName);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("#" + index + "  " + AvionicsBusEditorUtil.TypeName(dataType) + "  " + name, EditorStyles.boldLabel);

                    if (Application.isPlaying) return false;

                    if (GUILayout.Button("↑", GUILayout.Width(26f)))
                    {
                        _MoveParameter(recorder, index, -1);
                        return true;
                    }

                    if (GUILayout.Button("↓", GUILayout.Width(26f)))
                    {
                        _MoveParameter(recorder, index, 1);
                        return true;
                    }

                    if (GUILayout.Button("移除", GUILayout.Width(40f)))
                    {
                        _RemoveParameter(recorder, index);
                        return true;
                    }
                }

                if (!known)
                {
                    EditorGUILayout.HelpBox("当前 enum 中找不到这个参数，可能是 enum 改名或删除。", MessageType.Warning);
                }
                else if (!string.IsNullOrEmpty(storedName) && storedName != info.Name)
                {
                    EditorGUILayout.LabelField("存储名称：" + storedName, EditorStyles.miniLabel);
                }
            }

            return false;
        }

        private bool _IsSelected(int[] types, int[] ids, AvionicsBusEditorUtil.DataIdInfo info)
        {
            int count = types.Length < ids.Length ? types.Length : ids.Length;

            for (int i = 0; i < count; i++)
            {
                if (types[i] == info.DataType && ids[i] == info.Id) return true;
            }

            return false;
        }

        private void _DrawActions(AvionicsBusRecorder recorder)
        {
            EditorGUILayout.LabelField("操作", EditorStyles.boldLabel);

            bool ready = Application.isPlaying;
            int state = ready ? recorder._recordingState : StateIdle;
            string json = ready ? recorder.exportedJson : "";

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!ready || state == StateRecording))
                {
                    if (GUILayout.Button("开始采样"))
                    {
                        recorder._StartRecording();
                        _message = "已请求开始采样。";
                        _messageType = MessageType.Info;
                    }
                }

                using (new EditorGUI.DisabledScope(!ready || state != StateRecording))
                {
                    if (GUILayout.Button("停止并保存"))
                    {
                        _StopAndSave(recorder);
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!ready || string.IsNullOrEmpty(json)))
                {
                    if (GUILayout.Button("保存当前导出"))
                    {
                        _SaveJson(recorder, recorder.exportedJson);
                    }

                    if (GUILayout.Button("复制 JSON"))
                    {
                        EditorGUIUtility.systemCopyBuffer = recorder.exportedJson;
                        _message = "JSON 已复制到剪贴板。";
                        _messageType = MessageType.Info;
                    }
                }

                using (new EditorGUI.DisabledScope(!ready))
                {
                    if (GUILayout.Button("清空导出"))
                    {
                        recorder._ClearExport();
                        _message = "已清空导出数据。";
                        _messageType = MessageType.Info;
                    }
                }
            }

            if (!string.IsNullOrEmpty(_lastSavedPath))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("上次保存", _lastSavedPath, EditorStyles.miniLabel);

                    if (GUILayout.Button("显示", GUILayout.Width(48f)))
                    {
                        EditorUtility.RevealInFinder(_lastSavedPath);
                    }
                }
            }

            if (!string.IsNullOrEmpty(_message))
            {
                EditorGUILayout.HelpBox(_message, _messageType);
            }

            if (ready && state == StateRecording)
            {
                EditorGUILayout.HelpBox("正在采样。停止后数据会写入 exportedJson；离开 Play mode 前请先保存。", MessageType.Info);
            }
        }

        private void _StopAndSave(AvionicsBusRecorder recorder)
        {
            recorder._StopRecording();

            int state = recorder._recordingState;
            string json = recorder.exportedJson;

            if (state == StateError)
            {
                _message = recorder._lastError;
                _messageType = MessageType.Error;
                return;
            }

            if (string.IsNullOrEmpty(json))
            {
                _message = "停止后没有生成 JSON。";
                _messageType = MessageType.Error;
                return;
            }

            _SaveJson(recorder, json);
        }

        private void _SaveJson(AvionicsBusRecorder recorder, string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                _message = "没有可保存的 JSON。";
                _messageType = MessageType.Error;
                return;
            }

            if (!VRCJson.TryDeserializeFromJson(json, out DataToken token) || token.TokenType != TokenType.DataDictionary)
            {
                _message = "导出的 JSON 校验失败：" + token;
                _messageType = MessageType.Error;
                return;
            }

            string path = recorder.editorExportPath;

            if (string.IsNullOrEmpty(path))
            {
                path = _PromptExportPath();

                if (string.IsNullOrEmpty(path)) return;

                Undo.RecordObject(recorder, "Edit AvionicsBusRecorder export path");
                recorder.editorExportPath = path;
                EditorUtility.SetDirty(recorder);
            }

            string fullPath = _ResolvePath(path);

            if (string.IsNullOrEmpty(fullPath))
            {
                _message = "无法解析导出路径。";
                _messageType = MessageType.Error;
                return;
            }

            try
            {
                string directory = Path.GetDirectoryName(fullPath);

                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

                File.WriteAllText(fullPath, json, new UTF8Encoding(false));

                _lastSavedPath = fullPath;
                _message = "已保存到 " + fullPath + "（" + _FormatBytes(Encoding.UTF8.GetByteCount(json)) + "）。";
                _messageType = MessageType.Info;

                if (fullPath.StartsWith(Application.dataPath, StringComparison.OrdinalIgnoreCase))
                {
                    AssetDatabase.Refresh();
                }
            }
            catch (Exception exception)
            {
                _message = "保存失败：" + exception.Message;
                _messageType = MessageType.Error;
            }
        }

        private void _AddParameter(AvionicsBusRecorder recorder, int dataType, int id, string name)
        {
            Undo.RecordObject(recorder, "Add AvionicsBusRecorder parameter");
            _NormalizeArrays(recorder);

            recorder.exportDataTypes = AvionicsBusEditorUtil.AppendInt(recorder.exportDataTypes, dataType);
            recorder.exportIds = AvionicsBusEditorUtil.AppendInt(recorder.exportIds, id);
            recorder.exportNames = AvionicsBusEditorUtil.AppendString(recorder.exportNames, name);

            EditorUtility.SetDirty(recorder);
            UdonSharpEditorUtility.CopyProxyToUdon(recorder);

            _message = "已添加 " + name;
            _messageType = MessageType.Info;
        }

        private void _RemoveParameter(AvionicsBusRecorder recorder, int index)
        {
            Undo.RecordObject(recorder, "Remove AvionicsBusRecorder parameter");
            _NormalizeArrays(recorder);

            recorder.exportDataTypes = AvionicsBusEditorUtil.RemoveIntAt(recorder.exportDataTypes, index);
            recorder.exportIds = AvionicsBusEditorUtil.RemoveIntAt(recorder.exportIds, index);
            recorder.exportNames = AvionicsBusEditorUtil.RemoveStringAt(recorder.exportNames, index);

            EditorUtility.SetDirty(recorder);
            UdonSharpEditorUtility.CopyProxyToUdon(recorder);
        }

        private void _MoveParameter(AvionicsBusRecorder recorder, int index, int offset)
        {
            Undo.RecordObject(recorder, "Move AvionicsBusRecorder parameter");
            _NormalizeArrays(recorder);

            recorder.exportDataTypes = AvionicsBusEditorUtil.MoveInt(recorder.exportDataTypes, index, offset);
            recorder.exportIds = AvionicsBusEditorUtil.MoveInt(recorder.exportIds, index, offset);
            recorder.exportNames = AvionicsBusEditorUtil.MoveString(recorder.exportNames, index, offset);

            EditorUtility.SetDirty(recorder);
            UdonSharpEditorUtility.CopyProxyToUdon(recorder);
        }

        private void _ClearParameters(AvionicsBusRecorder recorder)
        {
            Undo.RecordObject(recorder, "Clear AvionicsBusRecorder parameters");

            recorder.exportDataTypes = new int[0];
            recorder.exportIds = new int[0];
            recorder.exportNames = new string[0];

            EditorUtility.SetDirty(recorder);
            UdonSharpEditorUtility.CopyProxyToUdon(recorder);
        }

        private void _SyncNames(AvionicsBusRecorder recorder)
        {
            Undo.RecordObject(recorder, "Sync AvionicsBusRecorder parameter names");
            _NormalizeArrays(recorder);

            int count = recorder.exportDataTypes.Length;
            string[] names = new string[count];
            int updated = 0;

            for (int i = 0; i < count; i++)
            {
                int type = recorder.exportDataTypes[i];
                int id = recorder.exportIds[i];

                if (AvionicsBusEditorUtil.TryGetDataIdInfo(type, id, out AvionicsBusEditorUtil.DataIdInfo info))
                {
                    names[i] = info.Name;

                    if (recorder.exportNames[i] != info.Name) updated++;
                }
                else
                {
                    names[i] = recorder.exportNames[i];
                }
            }

            Undo.RecordObject(recorder, "Sync AvionicsBusRecorder parameter names");
            recorder.exportNames = names;
            EditorUtility.SetDirty(recorder);
            UdonSharpEditorUtility.CopyProxyToUdon(recorder);

            _message = "已同步参数名称（" + updated + " 项变化）。";
            _messageType = MessageType.Info;
        }

        private void _NormalizeArrays(AvionicsBusRecorder recorder)
        {
            int[] sourceTypes = recorder.exportDataTypes ?? new int[0];
            int[] sourceIds = recorder.exportIds ?? new int[0];
            string[] sourceNames = recorder.exportNames ?? new string[0];

            int count = sourceTypes.Length;

            int[] types = new int[count];
            int[] ids = new int[count];
            string[] names = new string[count];

            for (int i = 0; i < count; i++)
            {
                types[i] = sourceTypes[i];
                ids[i] = i < sourceIds.Length ? sourceIds[i] : 0;
                names[i] = i < sourceNames.Length ? sourceNames[i] : "";
            }

            recorder.exportDataTypes = types;
            recorder.exportIds = ids;
            recorder.exportNames = names;
        }

        private int[] _GetTypes(AvionicsBusRecorder recorder)
        {
            if (Application.isPlaying)
            {
                UdonBehaviour udon = _GetUdonBehaviour(recorder);
                if (udon != null) return udon.GetProgramVariable("exportDataTypes") as int[] ?? new int[0];
            }

            return recorder.exportDataTypes ?? new int[0];
        }

        private int[] _GetIds(AvionicsBusRecorder recorder)
        {
            if (Application.isPlaying)
            {
                UdonBehaviour udon = _GetUdonBehaviour(recorder);
                if (udon != null) return udon.GetProgramVariable("exportIds") as int[] ?? new int[0];
            }

            return recorder.exportIds ?? new int[0];
        }

        private string[] _GetNames(AvionicsBusRecorder recorder)
        {
            if (Application.isPlaying)
            {
                UdonBehaviour udon = _GetUdonBehaviour(recorder);
                if (udon != null) return udon.GetProgramVariable("exportNames") as string[] ?? new string[0];
            }

            return recorder.exportNames ?? new string[0];
        }

        private int _GetState(AvionicsBusRecorder recorder)
        {
            UdonBehaviour udon = _GetUdonBehaviour(recorder);

            return udon != null ? _GetInt(udon, "_recordingState") : StateIdle;
        }

        private static UdonBehaviour _GetUdonBehaviour(AvionicsBusRecorder recorder)
        {
            UdonBehaviour udon = UdonSharpEditorUtility.GetBackingUdonBehaviour(recorder);

            if (udon != null && udon.GetProgramVariable("_recordingState") != null) return udon;

            foreach (UdonBehaviour candidate in recorder.GetComponents<UdonBehaviour>())
            {
                if (candidate.GetProgramVariable("_recordingState") != null) return candidate;
            }

            return null;
        }

        private static int _GetInt(UdonBehaviour udon, string name)
        {
            object value = udon.GetProgramVariable(name);

            return value is int ? (int)value : 0;
        }

        private static double _GetDouble(UdonBehaviour udon, string name)
        {
            object value = udon.GetProgramVariable(name);

            return value is double ? (double)value : 0.0;
        }

        private static bool _GetBool(UdonBehaviour udon, string name)
        {
            object value = udon.GetProgramVariable(name);

            return value is bool && (bool)value;
        }

        private static string _GetString(UdonBehaviour udon, string name)
        {
            object value = udon.GetProgramVariable(name);

            return value as string ?? "";
        }

        private static string _StateName(int state)
        {
            if (state == StateRecording) return "采样中";
            if (state == StateFinished) return "已完成";
            if (state == StateError) return "错误";

            return "空闲";
        }

        private string _PromptExportPath()
        {
            string selected = EditorUtility.SaveFilePanel(
                "导出 AvionicsBus JSON",
                _DefaultExportDirectory(),
                "avionics-bus-export.json",
                "json");

            return string.IsNullOrEmpty(selected) ? "" : _ToProjectRelative(selected);
        }

        private static string _DefaultExportDirectory()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string exportDirectory = Path.Combine(projectRoot, "AvionicsBusExports");

            return Directory.Exists(exportDirectory) ? exportDirectory : projectRoot;
        }

        private static string _ResolvePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return "";

            try
            {
                if (Path.IsPathRooted(path)) return Path.GetFullPath(path);

                string projectRoot = Path.GetDirectoryName(Application.dataPath);

                return Path.GetFullPath(Path.Combine(projectRoot, path));
            }
            catch
            {
                return "";
            }
        }

        private static string _ToProjectRelative(string path)
        {
            if (string.IsNullOrEmpty(path)) return "";

            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string fullPath = Path.GetFullPath(path);
            string prefix = projectRoot + Path.DirectorySeparatorChar;

            if (fullPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return fullPath.Substring(prefix.Length).Replace('\\', '/');
            }

            return fullPath;
        }

        private static string _FormatBytes(long bytes)
        {
            if (bytes < 1024) return bytes + " B";
            if (bytes < 1024L * 1024L) return (bytes / 1024.0).ToString("F1", CultureInfo.InvariantCulture) + " KB";

            return (bytes / (1024.0 * 1024.0)).ToString("F2", CultureInfo.InvariantCulture) + " MB";
        }
    }
}
