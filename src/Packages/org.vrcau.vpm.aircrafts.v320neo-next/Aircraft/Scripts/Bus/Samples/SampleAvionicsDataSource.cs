using UdonSharp;
using UnityEngine;

namespace VAU.V320NeoNext.Runtime.Bus.Samples
{
    /// <summary>
    /// 示例 Client：数据源 / 写入端。
    /// <para>
    /// 模拟 ADR 持续产生航电数据，用 _WriteAndNotifyXxx 写入 Bus 并通知订阅者。
    /// 把本脚本 + 任意一个 SampleSubscribingInstrument / SamplePollingInstrument / SampleCoalescingDisplay
    /// 一起挂在 AvionicsBus 所在物体的子物体上，进入 Play mode 即可看到数据流转。
    /// </para>
    /// <para>演示点：写入与通知、Frequent/Infrequent 两种更新节奏、Respawn 时复位数据。</para>
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SampleAvionicsDataSource : AbstractAvionicsBusClient
    {
        [Header("更新节奏（秒）")]
        [Tooltip("Frequent 数据的更新间隔，0 表示每帧都写")]
        public float frequentInterval = 0.05f;

        [Tooltip("Infrequent 数据的更新间隔")]
        public float infrequentInterval = 1f;

        [Header("模拟场景")]
        [Tooltip("垂直速度正弦波周期（秒）")]
        public float verticalSpeedPeriod = 12f;

        [Header("调试")]
        public bool logWrites = false;

        private float _frequentTimer;
        private float _infrequentTimer;
        private float _heading;
        private int _infrequentStep;

        // 模拟 IRS 对准状态
        private int _alignmentState = 1;
        private bool _isAligned = true;
        private bool _isDataValid = true;

        protected override void _OnAvionicsBusStart()
        {
            _frequentTimer = 0f;
            _infrequentTimer = 0f;
            _heading = 0f;

            // 先写一次初值，让本物体之后注册的订阅者也能立刻拿到有效数据
            _PublishFrequent(0f);
            _PublishInfrequent(0);

            if (logWrites) Debug.Log("[Bus Sample] DataSource started");
        }

        protected override void _OnAvionicsBusRespawnByLocalPlayer()
        {
            // 重生：数据复位为 0，等下一帧 Update 重新填充（也可以在这里直接 _PublishFrequent 一次）
            _WriteFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ADR_AltitudeFeet, 0f);
            _WriteFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ADR_IndicatedAirspeed, 0f);
            _WriteFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ADR_VerticalSpeedFeetPerMinute, 0f);
            _WriteVector3(AvionicsBusVector3DataIds.V32NN_Frequent_ADR_VelocityNED, Vector3.zero);
            _WriteBool(AvionicsBusBoolDataIds.V32NN_Infrequent_EFIS_Left_Sync_LandingSystemOn, false);

            _frequentTimer = frequentInterval + 1f; // 让下一次 Update 立刻重新发布

            if (logWrites) Debug.Log("[Bus Sample] DataSource respawned");
        }

        private void Update()
        {
            // Bus 还没初始化（本物体不在 AvionicsBus 层级下）时什么都不要做
            if (_avionicsBus == null) return;

            _frequentTimer += Time.deltaTime;
            if (_frequentTimer >= frequentInterval)
            {
                _frequentTimer = 0f;
                _PublishFrequent(Time.deltaTime);
            }

            _infrequentTimer += Time.deltaTime;
            if (_infrequentTimer >= infrequentInterval)
            {
                _infrequentTimer = 0f;
                _infrequentStep++;
                _PublishInfrequent(_infrequentStep);
            }
        }

        /// <summary>高频数据：姿态、大气数据等</summary>
        private void _PublishFrequent(float deltaTime)
        {
            float now = Time.time;
            float altitude = 10000f + Mathf.Sin(now * 0.02f) * 800f;
            float airspeed = 250f + Mathf.Sin(now * 0.05f) * 25f;
            float verticalSpeed = Mathf.Cos(now * (6.2831853f / verticalSpeedPeriod)) * 1200f;

            _heading += deltaTime * 45f;
            if (_heading >= 360f) _heading -= 360f;

            // _WriteAndNotifyXxx = 写入 + 立刻通知该 id 的所有订阅者
            _WriteAndNotifyFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ADR_AltitudeFeet, altitude);
            _WriteAndNotifyFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ADR_IndicatedAirspeed, airspeed);
            _WriteAndNotifyFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ADR_VerticalSpeedFeetPerMinute, verticalSpeed);
            _WriteAndNotifyInt(AvionicsBusIntDataIds.V32NN_Frequent_ADR_HeadingDegrees, (int)_heading);
            _WriteAndNotifyVector3(AvionicsBusVector3DataIds.V32NN_Frequent_ADR_VelocityNED, new Vector3(airspeed * 0.514444f, verticalSpeed * 0.00508f, 0f));
            _WriteAndNotifyBool(AvionicsBusBoolDataIds.V32NN_Infrequent_EFIS_Left_Sync_LandingSystemOn, _isDataValid);

            if (logWrites) Debug.Log("[Bus Sample] DataSource wrote altitude = " + altitude);
        }

        /// <summary>低频数据：接通状态、选调值、ECAM 信息等</summary>
        private void _PublishInfrequent(int step)
        {
            // 模拟 IRS 对准过程：启动 2 秒内为对准中(1)，之后对准完成(2)
            _isAligned = Time.time >= 2f;
            _alignmentState = _isAligned ? 2 : 1;

            _WriteAndNotifyFloat(AvionicsBusFloatDataIds.V32NN_Infrequent_FCU_SelectedAltitudeFeet, 30000f + step * 1000f);
            _WriteAndNotifyInt(AvionicsBusIntDataIds.V32NN_Frequent_ADIRS_AlignmentState, _alignmentState);
            _WriteAndNotifyByte(AvionicsBusByteDataIds.V32NN_Infrequent_EFIS_Left_Sync_NavigationDisplayFilter, (byte)_alignmentState);
            _WriteAndNotifyBool(AvionicsBusBoolDataIds.V32NN_Infrequent_EFIS_Left_Sync_FlightDirectorOn, _isAligned);
            _WriteAndNotifyString(AvionicsBusStringDataIds.V32NN_Infrequent_ECAM_ActiveMessage, "ADR 1 (step " + step + ")");
            _WriteAndNotifyVector3(AvionicsBusVector3DataIds.V32NN_Infrequent_ND_WindVector, new Vector3(12f, 0f, -5f - step));

            if (logWrites) Debug.Log("[Bus Sample] DataSource wrote infrequent step = " + step);
        }
    }
}
