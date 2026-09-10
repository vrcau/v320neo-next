using UdonSharp;
using UnityEngine;

namespace VAU.V320NeoNext.Runtime.Bus.Samples
{
    /// <summary>
    /// 示例 Client：轮询模式（实时更新）。
    /// <para>
    /// 不订阅任何事件，在 Update 里按自己的节奏 _ReadXxx 直接读最新值。
    /// 适合每帧都要用、或需要做平滑/插值/多数据联合计算的数据（姿态、大气数据、显示刷新）。
    /// </para>
    /// <para>
    /// 演示点：按自定义间隔轮询、把数据映射到 Transform（指针）、变化阈值日志。
    /// 注意：数据数组在 <see cref="_OnAvionicsBusStart"/>() 之前是 null，所以 Update 必须自己做就绪检查。
    /// </para>
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SamplePollingInstrument : AbstractAvionicsBusClient
    {
        [Header("轮询")]
        [Tooltip("轮询间隔（秒）")]
        public float pollInterval = 0.1f;

        [Header("显示（可选，留空则只做日志）")]
        [Tooltip("指针 Transform，会绕本地 Z 轴旋转")]
        public Transform needle;

        [Tooltip("指针满量程对应的空速值")]
        public float needleFullScale = 400f;

        [Tooltip("满量程对应的指针角度")]
        public float needleMaxAngle = 300f;

        [Tooltip("指针旋转方向是否反向")]
        public bool needleReversed = true;

        [Header("调试")]
        public bool logChanges = true;

        [Tooltip("空速变化超过该值（kt）才输出日志，避免刷屏")]
        public float logThreshold = 2f;

        private float _pollTimer;
        private float _indicatedAirspeed;
        private int _headingDegrees;
        private float _lastLoggedAirspeed;

        protected override void _OnAvionicsBusStart()
        {
            _pollTimer = 0f;

            _indicatedAirspeed = _ReadFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ADR_IndicatedAirspeed);
            _headingDegrees = _ReadInt(AvionicsBusIntDataIds.V32NN_Frequent_ADR_HeadingDegrees);
            _lastLoggedAirspeed = _indicatedAirspeed;

            _ApplyNeedle();

            if (logChanges) Debug.Log("[Bus Sample] PollingInstrument started, IAS = " + _indicatedAirspeed);
        }

        protected override void _OnAvionicsBusRespawn()
        {
            _pollTimer = 0f;
            _lastLoggedAirspeed = 0f;

            // 重生后指针归零，等待下一轮轮询重新驱动
            if (needle != null) needle.localRotation = Quaternion.identity;
        }

        private void Update()
        {
            // Bus 还没初始化（本物体不在 AvionicsBus 层级下）时不要读数据
            if (_avionicsBus == null) return;

            _pollTimer += Time.deltaTime;
            if (_pollTimer < pollInterval) return;
            _pollTimer = 0f;

            _Poll();
        }

        private void _Poll()
        {
            // 直接读 Bus 的数组，没有额外的方法调用与事件开销
            _indicatedAirspeed = _ReadFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ADR_IndicatedAirspeed);
            _headingDegrees = _ReadInt(AvionicsBusIntDataIds.V32NN_Frequent_ADR_HeadingDegrees);

            _ApplyNeedle();

            if (logChanges && Mathf.Abs(_indicatedAirspeed - _lastLoggedAirspeed) >= logThreshold)
            {
                _lastLoggedAirspeed = _indicatedAirspeed;
                Debug.Log("[Bus Sample] PollingInstrument IAS = " + _indicatedAirspeed + " kt, HDG = " + _headingDegrees);
            }
        }

        private void _ApplyNeedle()
        {
            if (needle == null || needleFullScale <= 0f) return;

            float normalized = Mathf.Clamp01(_indicatedAirspeed / needleFullScale);
            float angle = normalized * needleMaxAngle;
            if (needleReversed) angle = -angle;

            needle.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
