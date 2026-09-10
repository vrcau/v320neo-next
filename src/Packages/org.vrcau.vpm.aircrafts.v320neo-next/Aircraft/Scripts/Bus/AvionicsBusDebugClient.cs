using UdonSharp;
using UnityEngine;

namespace VAU.V320NeoNext.Runtime.Bus
{
    /// <summary>
    /// watch 列表里一项的数据类型标记，值会存进 <see cref="AvionicsBusDebugClient.watchDataTypes"/>。
    /// </summary>
    public enum AvionicsBusDataType
    {
        Float = 0,
        Int = 1,
        Bool = 2,
        String = 3,
        Vector3 = 4
    }

    /// <summary>
    /// AvionicsBus 的调试客户端：把感兴趣的数据 id 加进 watch 列表，就能在 Inspector 的调试面板里
    /// 看实时值、改值（可选是否触发订阅事件）。
    /// <para>
    /// Data holder 是 [NonSerialized]，默认在 Inspector 里看不到，所以读数/写值都由本组件的无参方法
    /// 在 Udon 侧完成，编辑器面板只负责调用与显示：
    /// <list type="bullet">
    /// <item><description><see cref="_DebugReadValue"/>()：读第 _debugEntryIndex 项的当前值 → _debugValueText</description></item>
    /// <item><description><see cref="_DebugWriteValue"/>() / <see cref="_DebugWriteValueAndNotify"/>()：
    /// 把 _debugWriteXxx 写进该项对应的 id</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// 桥接字段全部是标量，避免数组跨 Udon 边界。只在运行时有效（Udon 不在编辑模式执行），
    /// 因此不会往 prefab 里写数据；调试完删掉本组件即可。
    /// </para>
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class AvionicsBusDebugClient : AbstractAvionicsBusClient
    {
        #region Watch 列表（由 Inspector 的调试面板维护）

        [Tooltip("被监视数据的类型（AvionicsBusDataType 的值），与 watchIds 一一对应")]
        public int[] watchDataTypes = { };

        [Tooltip("被监视数据的 id，与 watchDataTypes 一一对应")]
        public int[] watchIds = { };

        #endregion

        #region 编辑器桥接字段（全部标量）

        [HideInInspector] public int _debugEntryIndex = -1;
        [HideInInspector] public float _debugWriteFloat;
        [HideInInspector] public int _debugWriteInt;
        [HideInInspector] public bool _debugWriteBool;
        [HideInInspector] public string _debugWriteString = "";
        [HideInInspector] public Vector3 _debugWriteVector3;
        [HideInInspector] public string _debugValueText = "";

        #endregion

        #region 编辑器桥接方法

        /// <summary>读取第 _debugEntryIndex 项的当前值到 _debugValueText。</summary>
        public void _DebugReadValue()
        {
            if (_avionicsBus == null)
            {
                _debugValueText = "<AvionicsBus 未初始化>";
                return;
            }

            _debugValueText = _ReadValueText(_EntryDataType(_debugEntryIndex), _EntryId(_debugEntryIndex));
        }

        /// <summary>把 _debugWriteXxx 写进第 _debugEntryIndex 项，不触发订阅事件。</summary>
        public void _DebugWriteValue() { _ApplyWrite(false); }

        /// <summary>把 _debugWriteXxx 写进第 _debugEntryIndex 项，并触发该 id 的订阅事件。</summary>
        public void _DebugWriteValueAndNotify() { _ApplyWrite(true); }

        #endregion

        #region Internal

        private void _ApplyWrite(bool notify)
        {
            if (_avionicsBus == null) return;

            int dataType = _EntryDataType(_debugEntryIndex);
            int id = _EntryId(_debugEntryIndex);
            if (!_IsIdInRange(dataType, id)) return;

            if (dataType == (int)AvionicsBusDataType.Float)
            {
                if (notify) _WriteAndNotifyFloat((AvionicsBusFloatDataIds)id, _debugWriteFloat);
                else _WriteFloat((AvionicsBusFloatDataIds)id, _debugWriteFloat);
            }
            else if (dataType == (int)AvionicsBusDataType.Int)
            {
                if (notify) _WriteAndNotifyInt((AvionicsBusIntDataIds)id, _debugWriteInt);
                else _WriteInt((AvionicsBusIntDataIds)id, _debugWriteInt);
            }
            else if (dataType == (int)AvionicsBusDataType.Bool)
            {
                if (notify) _WriteAndNotifyBool((AvionicsBusBoolDataIds)id, _debugWriteBool);
                else _WriteBool((AvionicsBusBoolDataIds)id, _debugWriteBool);
            }
            else if (dataType == (int)AvionicsBusDataType.String)
            {
                if (notify) _WriteAndNotifyString((AvionicsBusStringDataIds)id, _debugWriteString);
                else _WriteString((AvionicsBusStringDataIds)id, _debugWriteString);
            }
            else if (dataType == (int)AvionicsBusDataType.Vector3)
            {
                if (notify) _WriteAndNotifyVector3((AvionicsBusVector3DataIds)id, _debugWriteVector3);
                else _WriteVector3((AvionicsBusVector3DataIds)id, _debugWriteVector3);
            }
        }

        private string _ReadValueText(int dataType, int id)
        {
            if (!_IsIdInRange(dataType, id)) return "<无效的数据 id>";

            if (dataType == (int)AvionicsBusDataType.Float) return _FloatText(_ReadFloat((AvionicsBusFloatDataIds)id));
            if (dataType == (int)AvionicsBusDataType.Int) return "" + _ReadInt((AvionicsBusIntDataIds)id);
            if (dataType == (int)AvionicsBusDataType.Bool) return "" + _ReadBool((AvionicsBusBoolDataIds)id);

            if (dataType == (int)AvionicsBusDataType.String)
            {
                string text = _ReadString((AvionicsBusStringDataIds)id);
                return text == null ? "(null)" : text;
            }

            if (dataType == (int)AvionicsBusDataType.Vector3) return _Vector3Text(_ReadVector3((AvionicsBusVector3DataIds)id));

            return "<未知类型>";
        }

        private int _EntryDataType(int index)
        {
            if (index < 0 || index >= watchDataTypes.Length) return -1;

            return watchDataTypes[index];
        }

        private int _EntryId(int index)
        {
            if (index < 0 || index >= watchIds.Length) return -1;

            return watchIds[index];
        }

        private bool _IsIdInRange(int dataType, int id)
        {
            if (id < 0) return false;

            if (dataType == (int)AvionicsBusDataType.Float) return id < (int)AvionicsBusFloatDataIds.Count;
            if (dataType == (int)AvionicsBusDataType.Int) return id < (int)AvionicsBusIntDataIds.Count;
            if (dataType == (int)AvionicsBusDataType.Bool) return id < (int)AvionicsBusBoolDataIds.Count;
            if (dataType == (int)AvionicsBusDataType.String) return id < (int)AvionicsBusStringDataIds.Count;
            if (dataType == (int)AvionicsBusDataType.Vector3) return id < (int)AvionicsBusVector3DataIds.Count;

            return false;
        }

        private string _FloatText(float value) { return "" + Mathf.Round(value * 1000f) * 0.001f; }

        private string _Vector3Text(Vector3 value)
        {
            return "(" + _FloatText(value.x) + ", " + _FloatText(value.y) + ", " + _FloatText(value.z) + ")";
        }

        #endregion
    }
}
