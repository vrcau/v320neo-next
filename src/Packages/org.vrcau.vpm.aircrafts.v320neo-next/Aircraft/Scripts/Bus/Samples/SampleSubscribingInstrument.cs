using UdonSharp;
using UnityEngine;

namespace VAU.V320NeoNext.Runtime.Bus.Samples
{
    /// <summary>
    /// 示例 Client：纯订阅模式（事件驱动）。
    /// <para>
    /// 不轮询，只在 <see cref="_OnAvionicsBusStart"/>() 里注册订阅，数据变化时由 Bus 回调
    /// 下面那些 public 的 Sample_OnXxx 方法。适合"变化才需要干活"的场景（告警、离散状态、
    /// 低频数据、需要立刻响应的逻辑）。
    /// </para>
    /// <para>
    /// 演示点：订阅注册时机、public 事件接收方法、通知节流、值变化检测、未订阅数据的按需读取。
    /// </para>
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SampleSubscribingInstrument : AbstractAvionicsBusClient
    {
        [Header("通知节流")]
        [Tooltip("每收到多少次通知才真正处理一次，1 表示不节流")]
        public int notifyThrottle = 1;

        [Header("值变化检测")]
        [Tooltip("高度变化小于该值（ft）时不处理")]
        public float altitudeDeadband = 1f;

        [Header("调试")]
        public bool logOnNotify = true;

        private int _altitudeNotifyCount;
        private int _processedCount;
        private float _altitudeFeet;
        private int _verticalSpeedFeetPerMinute;
        private bool _isDataValid;
        private string _ecamMessage;

        protected override void _OnAvionicsBusStart()
        {
            // 订阅必须在这里注册：Bus 每次启动都会清空重建订阅表，
            // 所以只有 _OnAvionicsBusStart() 里的注册才是有效的。
            _SubscribeFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ADR_AltitudeFeet, nameof(Sample_OnAdrAltitudeChanged));
            _SubscribeFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ADR_VerticalSpeedFeetPerMinute, nameof(Sample_OnAdrVerticalSpeedChanged));
            _SubscribeBool(AvionicsBusBoolDataIds.V32NN_Frequent_ADR_IsDataValid, nameof(Sample_OnAdrDataValidChanged));
            _SubscribeFloat(AvionicsBusFloatDataIds.V32NN_Infrequent_FCU_SelectedAltitudeFeet, nameof(Sample_OnFcuSelectedAltitudeChanged));
            _SubscribeBool(AvionicsBusBoolDataIds.V32NN_Infrequent_ADIRS_IsAligned, nameof(Sample_OnAdirsAlignedChanged));
            _SubscribeString(AvionicsBusStringDataIds.V32NN_Infrequent_ECAM_ActiveMessage, nameof(Sample_OnEcamMessageChanged));
            _SubscribeVector3(AvionicsBusVector3DataIds.V32NN_Infrequent_ND_WindVector, nameof(Sample_OnNdWindChanged));

            // 没有订阅的数据（例如只为读数用的空速）直接读一次即可，不需要订阅
            _altitudeFeet = _ReadFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ADR_AltitudeFeet);
            _isDataValid = _ReadBool(AvionicsBusBoolDataIds.V32NN_Frequent_ADR_IsDataValid);

            if (logOnNotify) Debug.Log("[Bus Sample] SubscribingInstrument subscribed");
        }

        protected override void _OnAvionicsBusRespawn()
        {
            _altitudeNotifyCount = 0;
            _processedCount = 0;
            _ecamMessage = "";
        }

        #region 事件接收方法（必须是 public，名字通过 nameof 注册给 Bus）

        public void Sample_OnAdrAltitudeChanged()
        {
            _altitudeNotifyCount++;

            // 节流：高频数据每 N 次通知才处理一次
            if (notifyThrottle > 1 && _altitudeNotifyCount % notifyThrottle != 0) return;

            // 变化检测：拿到通知后按需读取最新值（写入方可能已经把值改掉了）
            float newAltitude = _ReadFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ADR_AltitudeFeet);
            if (Mathf.Abs(newAltitude - _altitudeFeet) < altitudeDeadband) return;

            _altitudeFeet = newAltitude;
            _processedCount++;

            if (logOnNotify) Debug.Log("[Bus Sample] SubscribingInstrument altitude = " + _altitudeFeet + " ft, VS = " + _verticalSpeedFeetPerMinute + " fpm");
        }

        public void Sample_OnAdrVerticalSpeedChanged()
        {
            _verticalSpeedFeetPerMinute = (int)_ReadFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ADR_VerticalSpeedFeetPerMinute);
        }

        public void Sample_OnAdrDataValidChanged()
        {
            _isDataValid = _ReadBool(AvionicsBusBoolDataIds.V32NN_Frequent_ADR_IsDataValid);

            if (_isDataValid) return;

            // 数据失效时做兜底处理（真实实现里可以切备用源或触发告警），这里只打日志
            if (logOnNotify) Debug.Log("[Bus Sample] SubscribingInstrument ADR data invalid, last ECAM message = " + _ecamMessage);
        }

        public void Sample_OnFcuSelectedAltitudeChanged()
        {
            float selected = _ReadFloat(AvionicsBusFloatDataIds.V32NN_Infrequent_FCU_SelectedAltitudeFeet);

            if (logOnNotify) Debug.Log("[Bus Sample] SubscribingInstrument FCU selected altitude = " + selected + " ft");
        }

        public void Sample_OnAdirsAlignedChanged()
        {
            bool aligned = _ReadBool(AvionicsBusBoolDataIds.V32NN_Infrequent_ADIRS_IsAligned);

            if (logOnNotify) Debug.Log("[Bus Sample] SubscribingInstrument ADIRS aligned = " + aligned);
        }

        public void Sample_OnEcamMessageChanged()
        {
            _ecamMessage = _ReadString(AvionicsBusStringDataIds.V32NN_Infrequent_ECAM_ActiveMessage);

            if (logOnNotify) Debug.Log("[Bus Sample] SubscribingInstrument ECAM message = " + _ecamMessage);
        }

        public void Sample_OnNdWindChanged()
        {
            Vector3 wind = _ReadVector3(AvionicsBusVector3DataIds.V32NN_Infrequent_ND_WindVector);

            if (logOnNotify) Debug.Log("[Bus Sample] SubscribingInstrument wind = " + wind.x + " / " + wind.z);
        }

        #endregion
    }
}
