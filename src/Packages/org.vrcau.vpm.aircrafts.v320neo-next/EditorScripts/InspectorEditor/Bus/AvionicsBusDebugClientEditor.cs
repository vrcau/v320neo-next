using System;
using System.Collections.Generic;
using System.Globalization;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using VAU.V320NeoNext.Runtime.Bus;
using VRC.Udon;

namespace VAU.V320NeoNext.Editor.InspectorEditor.Bus
{
    /// <summary>
    /// <see cref="AvionicsBusDebugClient"/> 的调试面板：搜索 data id → 加入 watch 列表 → 看实时值 / 改值。
    /// <para>
    /// 读数与写值都是通过调用 proxy 上的 U# 无参方法完成（等于在 Udon 里执行），
    /// 所以拿到的一定是运行时的真实数据，而不是 proxy 里那份拷贝。
    /// </para>
    /// </summary>
    [CustomEditor(typeof(AvionicsBusDebugClient))]
    public sealed class AvionicsBusDebugClientEditor : UnityEditor.Editor
    {
        private const int MaxSearchResults = 12;

        private static readonly string[] TypeFilterLabels = { "全部", "Float", "Int", "Bool", "String", "Vector3", "Byte" };
        private static readonly string[] TypeNames = { "Float", "Int", "Bool", "String", "Vector3", "Byte" };

        private sealed class DataIdInfo
        {
            public string Name;
            public int DataType;
            public int Id;
        }

        private static List<DataIdInfo> _catalog;

        // 显示用的当前值 / 待写入的新值文本（编辑器侧状态，不进 prefab）
        private readonly List<string> _valueTexts = new List<string>();
        private readonly List<string> _writeTexts = new List<string>();

        private string _search = "";
        private int _typeFilter;
        private bool _autoRefresh = true;
        private double _nextAutoRefreshTime;
        private string _message = "";

        private void OnEnable()
        {
            _search = "";
            _typeFilter = 0;
            _message = "";
            _valueTexts.Clear();
            _writeTexts.Clear();
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            var client = (AvionicsBusDebugClient)target;

            _EnsureLists(client.watchIds.Length);

            _DrawSearchSection(client);
            EditorGUILayout.Space();
            _DrawWatchSection(client);
            EditorGUILayout.Space();
            _DrawFooter(client);

            _RefreshValues(client, false);
        }

        #region 搜索 / 添加

        private void _DrawSearchSection(AvionicsBusDebugClient client)
        {
            EditorGUILayout.LabelField("添加参数", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField);
                _typeFilter = EditorGUILayout.Popup(_typeFilter, TypeFilterLabels, GUILayout.Width(80f));
            }

            List<DataIdInfo> matches = _FindMatches(client);

            if (matches.Count == 0)
            {
                EditorGUILayout.LabelField(
                    string.IsNullOrEmpty(_search) ? "（输入关键字搜索数据 id）" : "没有匹配的数据 id",
                    EditorStyles.miniLabel);
                return;
            }

            int shown = Mathf.Min(matches.Count, MaxSearchResults);

            for (int i = 0; i < shown; i++)
            {
                DataIdInfo info = matches[i];

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(_TypeName(info.DataType), GUILayout.Width(48f));
                    EditorGUILayout.LabelField(info.Name);

                    if (GUILayout.Button("+", GUILayout.Width(24f)))
                    {
                        _AddWatchEntry(client, info);
                        GUI.FocusControl(null);
                    }
                }
            }

            if (matches.Count > shown)
            {
                EditorGUILayout.LabelField("（还有 " + (matches.Count - shown) + " 条，继续输入关键字缩小范围）", EditorStyles.miniLabel);
            }
        }

        private List<DataIdInfo> _FindMatches(AvionicsBusDebugClient client)
        {
            var matches = new List<DataIdInfo>();

            foreach (DataIdInfo info in _GetCatalog())
            {
                if (_typeFilter > 0 && info.DataType != _typeFilter - 1) continue;
                if (_IsWatched(client, info)) continue;
                if (!string.IsNullOrEmpty(_search) && info.Name.IndexOf(_search, StringComparison.OrdinalIgnoreCase) < 0) continue;

                matches.Add(info);
            }

            return matches;
        }

        #endregion

        #region Watch 列表

        private void _DrawWatchSection(AvionicsBusDebugClient client)
        {
            int count = client.watchIds.Length;

            EditorGUILayout.LabelField("Watch 列表 (" + count + ")", EditorStyles.boldLabel);

            if (client.watchDataTypes.Length != count)
            {
                EditorGUILayout.HelpBox("watchDataTypes 与 watchIds 长度不一致，建议清空列表后重新添加。", MessageType.Warning);
            }

            if (count == 0)
            {
                EditorGUILayout.LabelField("（还没有添加参数，用上面的搜索框添加）", EditorStyles.miniLabel);
                return;
            }

            for (int i = 0; i < count; i++)
            {
                // 本帧移除了条目，后面的下帧再画
                if (_DrawWatchEntry(client, i)) return;
            }
        }

        /// <returns>是否移除了该条目。</returns>
        private bool _DrawWatchEntry(AvionicsBusDebugClient client, int index)
        {
            int dataType = index < client.watchDataTypes.Length ? client.watchDataTypes[index] : -1;
            int id = client.watchIds[index];

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(
                        "#" + index + "   " + _TypeName(dataType) + "   " + _GetDataIdName(dataType, id),
                        EditorStyles.boldLabel);

                    if (GUILayout.Button("移除", GUILayout.Width(40f)))
                    {
                        _RemoveWatchEntry(client, index);
                        return true;
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("当前值", GUILayout.Width(48f));
                    EditorGUILayout.LabelField(Application.isPlaying ? _ValueText(index) : "—（未运行）");
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("新值", GUILayout.Width(48f));

                    // 首次显示时用当前值做初值，直接改一下就能写
                    if (_writeTexts[index] == null)
                    {
                        _writeTexts[index] = Application.isPlaying ? _ValueText(index) : "";
                    }

                    _writeTexts[index] = EditorGUILayout.TextField(_writeTexts[index]);

                    using (new EditorGUI.DisabledScope(!Application.isPlaying))
                    {
                        if (GUILayout.Button("写入", GUILayout.Width(48f))) _WriteEntry(client, index, false);
                        if (GUILayout.Button("写入并 Notify", GUILayout.Width(104f))) _WriteEntry(client, index, true);
                    }
                }
            }

            return false;
        }

        private void _AddWatchEntry(AvionicsBusDebugClient client, DataIdInfo info)
        {
            Undo.RecordObject(client, "Add AvionicsBus watch entry");

            client.watchDataTypes = _AppendInt(client.watchDataTypes, info.DataType);
            client.watchIds = _AppendInt(client.watchIds, info.Id);

            _PushConfigToUdon(client);
            EditorUtility.SetDirty(client);

            _EnsureLists(client.watchIds.Length);
            _message = "已添加 " + info.Name;
        }

        private void _RemoveWatchEntry(AvionicsBusDebugClient client, int index)
        {
            Undo.RecordObject(client, "Remove AvionicsBus watch entry");

            int dataType = index < client.watchDataTypes.Length ? client.watchDataTypes[index] : -1;
            int id = client.watchIds[index];

            client.watchDataTypes = _RemoveIntAt(client.watchDataTypes, index);
            client.watchIds = _RemoveIntAt(client.watchIds, index);

            _PushConfigToUdon(client);
            EditorUtility.SetDirty(client);

            if (index < _valueTexts.Count) _valueTexts.RemoveAt(index);
            if (index < _writeTexts.Count) _writeTexts.RemoveAt(index);

            _message = "已移除 " + _GetDataIdName(dataType, id);
        }

        private void _ClearWatchList(AvionicsBusDebugClient client)
        {
            Undo.RecordObject(client, "Clear AvionicsBus watch list");

            client.watchDataTypes = new int[0];
            client.watchIds = new int[0];

            _PushConfigToUdon(client);
            EditorUtility.SetDirty(client);

            _valueTexts.Clear();
            _writeTexts.Clear();
            _message = "已清空 watch 列表";
        }

        #endregion

        #region 读数 / 写值

        private void _RefreshValues(AvionicsBusDebugClient client, bool force)
        {
            if (!Application.isPlaying) return;

            double now = EditorApplication.timeSinceStartup;

            if (!force && (!_autoRefresh || now < _nextAutoRefreshTime)) return;

            _nextAutoRefreshTime = now + 0.25;

            UdonBehaviour udon = _GetUdonBehaviour(client);
            if (udon == null) return;

            int count = client.watchIds.Length;
            _EnsureLists(count);

            for (int i = 0; i < count; i++)
            {
                _valueTexts[i] = _ReadValueText(udon, i);
            }

            Repaint();
        }

        private void _WriteEntry(AvionicsBusDebugClient client, int index, bool notify)
        {
            if (index >= client.watchIds.Length) return;

            UdonBehaviour udon = _GetUdonBehaviour(client);
            if (udon == null)
            {
                _message = "找不到 UdonBehaviour，无法写入";
                return;
            }

            int dataType = index < client.watchDataTypes.Length ? client.watchDataTypes[index] : -1;
            int id = client.watchIds[index];
            string text = index < _writeTexts.Count ? _writeTexts[index] : "";

            if (dataType == (int)AvionicsBusDataType.Float)
            {
                float parsed;
                if (!_TryParseFloat(text, out parsed))
                {
                    _message = "无法解析为 float：" + text;
                    return;
                }

                udon.SetProgramVariable(nameof(AvionicsBusDebugClient._debugWriteFloat), parsed);
            }
            else if (dataType == (int)AvionicsBusDataType.Byte)
            {
                byte parsed;
                if (!byte.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
                {
                    _message = "无法解析为 byte（0-255）：" + text;
                    return;
                }

                udon.SetProgramVariable(nameof(AvionicsBusDebugClient._debugWriteByte), parsed);
            }
            else if (dataType == (int)AvionicsBusDataType.Int)
            {
                int parsed;
                if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
                {
                    _message = "无法解析为 int：" + text;
                    return;
                }

                udon.SetProgramVariable(nameof(AvionicsBusDebugClient._debugWriteInt), parsed);
            }
            else if (dataType == (int)AvionicsBusDataType.Bool)
            {
                bool parsed;
                string trimmed = (text ?? "").Trim();

                if (trimmed == "1") parsed = true;
                else if (trimmed == "0") parsed = false;
                else if (!bool.TryParse(trimmed, out parsed))
                {
                    _message = "无法解析为 bool（true/false 或 1/0）：" + text;
                    return;
                }

                udon.SetProgramVariable(nameof(AvionicsBusDebugClient._debugWriteBool), parsed);
            }
            else if (dataType == (int)AvionicsBusDataType.String)
            {
                udon.SetProgramVariable(nameof(AvionicsBusDebugClient._debugWriteString), text);
            }
            else if (dataType == (int)AvionicsBusDataType.Vector3)
            {
                Vector3 parsed;
                if (!_TryParseVector3(text, out parsed))
                {
                    _message = "无法解析为 Vector3（形如 1, 2, 3）：" + text;
                    return;
                }

                udon.SetProgramVariable(nameof(AvionicsBusDebugClient._debugWriteVector3), parsed);
            }
            else
            {
                _message = "未知的数据类型：" + dataType;
                return;
            }

            udon.SetProgramVariable(nameof(AvionicsBusDebugClient._debugEntryIndex), index);
            udon.SendCustomEvent(notify
                ? nameof(AvionicsBusDebugClient._DebugWriteValueAndNotify)
                : nameof(AvionicsBusDebugClient._DebugWriteValue));

            // 顺手读回来，面板立刻显示写入后的值
            if (index < _valueTexts.Count) _valueTexts[index] = _ReadValueText(udon, index);

            _message = "已写入 " + _GetDataIdName(dataType, id) + (notify ? "（并已 Notify）" : "（未 Notify）");
        }

        #endregion

        #region Udon 交互

        /// <summary>
        /// 取这个 U# 行为真正的 UdonBehaviour。读写必须打在它身上：
        /// 打在编辑器用的 Proxy MonoBehaviour 上只会执行一份 C# 副本，数据是错的。
        /// </summary>
        private static UdonBehaviour _GetUdonBehaviour(AvionicsBusDebugClient client)
        {
            UdonBehaviour udon = UdonSharpEditorUtility.GetBackingUdonBehaviour(client);
            if (udon != null && udon.GetProgramVariable("_debugValueText") != null) return udon;

            // 兜底：同一个物体上可能挂了多个 UdonBehaviour，用只存在于本组件的变量来认领
            foreach (UdonBehaviour candidate in client.GetComponents<UdonBehaviour>())
            {
                if (candidate.GetProgramVariable("_debugValueText") != null) return candidate;
            }

            return null;
        }

        /// <summary>让 Udon 侧读取第 index 项，返回读取结果文本。</summary>
        private static string _ReadValueText(UdonBehaviour udon, int index)
        {
            udon.SetProgramVariable("_debugEntryIndex", index);
            udon.SendCustomEvent(nameof(AvionicsBusDebugClient._DebugReadValue));

            return udon.GetProgramVariable(nameof(AvionicsBusDebugClient._debugValueText)) as string ?? "";
        }

        /// <summary>
        /// 把 watch 列表配置推给 Udon。只在编辑配置（增删条目）时调用；
        /// Play mode 下不要调，否则会用旧副本覆盖运行时数据。
        /// </summary>
        private static void _PushConfigToUdon(AvionicsBusDebugClient client)
        {
            client.ApplyProxyModifications();
        }

        #endregion

        #region 界面辅助

        private void _DrawFooter(AvionicsBusDebugClient client)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                _autoRefresh = EditorGUILayout.ToggleLeft("自动刷新", _autoRefresh, GUILayout.Width(72f));

                using (new EditorGUI.DisabledScope(!Application.isPlaying))
                {
                    if (GUILayout.Button("刷新值")) _RefreshValues(client, true);
                }

                if (GUILayout.Button("清空列表")) _ClearWatchList(client);
            }

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Udon 在编辑模式下不执行，进入 Play mode 后才能读取 / 写入实时值。", MessageType.Info);
            }

            if (!string.IsNullOrEmpty(_message))
            {
                EditorGUILayout.HelpBox(_message, MessageType.None);
            }
        }

        private void _EnsureLists(int count)
        {
            while (_valueTexts.Count < count) _valueTexts.Add(null);
            while (_writeTexts.Count < count) _writeTexts.Add(null);

            while (_valueTexts.Count > count) _valueTexts.RemoveAt(_valueTexts.Count - 1);
            while (_writeTexts.Count > count) _writeTexts.RemoveAt(_writeTexts.Count - 1);
        }

        private string _ValueText(int index)
        {
            if (index < 0 || index >= _valueTexts.Count) return "";

            return _valueTexts[index] ?? "";
        }

        private static string _TypeName(int dataType)
        {
            if (dataType < 0 || dataType >= TypeNames.Length) return "?";

            return TypeNames[dataType];
        }

        private static int[] _AppendInt(int[] source, int value)
        {
            var result = new int[source.Length + 1];
            Array.Copy(source, result, source.Length);
            result[source.Length] = value;

            return result;
        }

        private static int[] _RemoveIntAt(int[] source, int index)
        {
            if (index < 0 || index >= source.Length) return source;

            var result = new int[source.Length - 1];

            for (int i = 0, j = 0; i < source.Length; i++)
            {
                if (i == index) continue;

                result[j] = source[i];
                j++;
            }

            return result;
        }

        private static bool _TryParseFloat(string text, out float value)
        {
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private static bool _TryParseVector3(string text, out Vector3 value)
        {
            value = Vector3.zero;

            if (string.IsNullOrEmpty(text)) return false;

            string normalized = text.Replace('(', ' ').Replace(')', ' ');
            string[] parts = normalized.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 3) return false;

            float x;
            float y;
            float z;

            if (!_TryParseFloat(parts[0], out x)) return false;
            if (!_TryParseFloat(parts[1], out y)) return false;
            if (!_TryParseFloat(parts[2], out z)) return false;

            value = new Vector3(x, y, z);

            return true;
        }

        #endregion

        #region data id 目录（从 5 个 enum 反射出来，自动跟着 enum 走）

        private static List<DataIdInfo> _GetCatalog()
        {
            if (_catalog != null) return _catalog;

            _catalog = new List<DataIdInfo>();

            _AddEnumToCatalog(typeof(AvionicsBusFloatDataIds), (int)AvionicsBusDataType.Float);
            _AddEnumToCatalog(typeof(AvionicsBusByteDataIds), (int)AvionicsBusDataType.Byte);
            _AddEnumToCatalog(typeof(AvionicsBusIntDataIds), (int)AvionicsBusDataType.Int);
            _AddEnumToCatalog(typeof(AvionicsBusBoolDataIds), (int)AvionicsBusDataType.Bool);
            _AddEnumToCatalog(typeof(AvionicsBusStringDataIds), (int)AvionicsBusDataType.String);
            _AddEnumToCatalog(typeof(AvionicsBusVector3DataIds), (int)AvionicsBusDataType.Vector3);

            return _catalog;
        }

        private static void _AddEnumToCatalog(Type enumType, int dataType)
        {
            foreach (string name in Enum.GetNames(enumType))
            {
                // Count 是分配数组用的哨兵，不是真实数据
                if (name == "Count") continue;

                _catalog.Add(new DataIdInfo
                {
                    Name = name,
                    DataType = dataType,
                    Id = (int)Enum.Parse(enumType, name)
                });
            }
        }

        private static string _GetDataIdName(int dataType, int id)
        {
            foreach (DataIdInfo info in _GetCatalog())
            {
                if (info.DataType == dataType && info.Id == id) return info.Name;
            }

            return "<未知：类型 " + dataType + "，id " + id + ">";
        }

        private static bool _IsWatched(AvionicsBusDebugClient client, DataIdInfo info)
        {
            for (int i = 0; i < client.watchIds.Length; i++)
            {
                if (client.watchIds[i] != info.Id) continue;
                if (i < client.watchDataTypes.Length && client.watchDataTypes[i] == info.DataType) return true;
            }

            return false;
        }

        #endregion
    }
}
