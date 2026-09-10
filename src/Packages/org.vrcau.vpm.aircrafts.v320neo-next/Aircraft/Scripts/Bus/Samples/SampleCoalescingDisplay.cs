using UdonSharp;
using UnityEngine;

namespace VAU.V320NeoNext.Runtime.Bus.Samples
{
    /// <summary>
    /// 示例 Client：订阅 + 每帧合并刷新（脏标记合并）。
    /// <para>
    /// 高频数据每帧会派发多次通知，如果每次通知都立刻刷新显示就会做大量重复工作。
    /// 这里让事件接收方法只置脏标记，真正的读取/刷新统一放到 LateUpdate 里做一次，
    /// 并用分字段脏标记做到"只读变化过的数据"。
    /// </para>
    /// <para>
    /// 演示点：订阅 + 合并刷新、分字段脏标记、通知次数 / 实际刷新次数统计。
    /// </para>
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SampleCoalescingDisplay : AbstractAvionicsBusClient
    {
        [Header("统计")]
        [Tooltip("每隔多少次刷新输出一次通知/刷新次数统计")]
        public int logStatsEvery = 600;

        [Header("调试")]
        public bool logRefresh = false;

        // 分字段脏标记：只有被通知过的数据才需要重新读取
        private bool _altitudeDirty;
        private bool _airspeedDirty;
        private bool _verticalSpeedDirty;
        private bool _headingDirty;

        private int _notifyCount;
        private int _refreshCount;

        // 当前显示值
        private float _altitudeFeet;
        private float _airspeed;
        private float _verticalSpeedFeetPerMinute;
        private int _headingDegrees;

        protected override void _OnAvionicsBusStart()
        {
            _SubscribeFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ADR_AltitudeFeet, nameof(Sample_OnAltitudeChanged));
            _SubscribeFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ADR_IndicatedAirspeed, nameof(Sample_OnAirspeedChanged));
            _SubscribeFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ADR_VerticalSpeedFeetPerMinute, nameof(Sample_OnVerticalSpeedChanged));
            _SubscribeInt(AvionicsBusIntDataIds.V32NN_Frequent_ADR_HeadingDegrees, nameof(Sample_OnHeadingChanged));

            // 订阅之后先全量读一次，保证首帧显示是完整的
            _altitudeFeet = _ReadFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ADR_AltitudeFeet);
            _airspeed = _ReadFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ADR_IndicatedAirspeed);
            _verticalSpeedFeetPerMinute = _ReadFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ADR_VerticalSpeedFeetPerMinute);
            _headingDegrees = _ReadInt(AvionicsBusIntDataIds.V32NN_Frequent_ADR_HeadingDegrees);
        }

        protected override void _OnAvionicsBusRespawn()
        {
            _notifyCount = 0;
            _refreshCount = 0;
            _altitudeDirty = false;
            _airspeedDirty = false;
            _verticalSpeedDirty = false;
            _headingDirty = false;
        }

        #region 事件接收方法（必须 public）：只置脏标记，不做任何重活

        public void Sample_OnAltitudeChanged() { _notifyCount++; _altitudeDirty = true; }
        public void Sample_OnAirspeedChanged() { _notifyCount++; _airspeedDirty = true; }
        public void Sample_OnVerticalSpeedChanged() { _notifyCount++; _verticalSpeedDirty = true; }
        public void Sample_OnHeadingChanged() { _notifyCount++; _headingDirty = true; }

        #endregion

        private void LateUpdate()
        {
            if (_avionicsBus == null) return;

            // 本帧没有任何变化就直接跳过，完全零开销
            if (!_altitudeDirty && !_airspeedDirty && !_verticalSpeedDirty && !_headingDirty) return;

            _refreshCount++;

            if (_altitudeDirty)
            {
                _altitudeDirty = false;
                _altitudeFeet = _ReadFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ADR_AltitudeFeet);
            }

            if (_airspeedDirty)
            {
                _airspeedDirty = false;
                _airspeed = _ReadFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ADR_IndicatedAirspeed);
            }

            if (_verticalSpeedDirty)
            {
                _verticalSpeedDirty = false;
                _verticalSpeedFeetPerMinute = _ReadFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ADR_VerticalSpeedFeetPerMinute);
            }

            if (_headingDirty)
            {
                _headingDirty = false;
                _headingDegrees = _ReadInt(AvionicsBusIntDataIds.V32NN_Frequent_ADR_HeadingDegrees);
            }

            _RefreshDisplay();
        }

        private void _RefreshDisplay()
        {
            if (logRefresh)
            {
                Debug.Log("[Bus Sample] CoalescingDisplay ALT = " + _altitudeFeet
                    + " ft, IAS = " + _airspeed
                    + " kt, VS = " + _verticalSpeedFeetPerMinute
                    + " fpm, HDG = " + _headingDegrees);
            }

            if (logStatsEvery > 0 && _refreshCount % logStatsEvery == 0)
            {
                Debug.Log("[Bus Sample] CoalescingDisplay refresh = " + _refreshCount
                    + ", notify = " + _notifyCount
                    + " (每次刷新平均收到 " + (_notifyCount / _refreshCount) + " 次通知)");
            }
        }
    }
}
