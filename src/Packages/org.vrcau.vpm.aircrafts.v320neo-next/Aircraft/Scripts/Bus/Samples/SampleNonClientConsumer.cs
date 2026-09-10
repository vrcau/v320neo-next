using UdonSharp;
using UnityEngine;

namespace VAU.V320NeoNext.Runtime.Bus.Samples
{
    /// <summary>
    /// 示例：**不是** <see cref="AbstractAvionicsBusClient"/> 子类的普通脚本，如何访问 Bus 数据。
    /// <para>
    /// 关键点：bus 上的数据数组是 public 的，**初始化阶段把数组引用拿到本地存起来**，
    /// 之后读写就都是本地数组访问，不再产生"跳去 bus 取变量"的开销 —— 这正是
    /// <see cref="AbstractAvionicsBusClient"/> 内部的做法。
    /// </para>
    /// <para>
    /// 只有两件事避不开对 bus 的调用：订阅（订阅表在 bus 上，初始化期一次性）和通知
    /// （写入后要让 bus 派发事件）。如果这个脚本要高频读写，直接继承 client 并用它的
    /// 扩展方法 ReadXxx / WriteXxx / SubscribeXxx 会更省事。
    /// </para>
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SampleNonClientConsumer : UdonSharpBehaviour
    {
        [Tooltip("留空则自动向上查找 AvionicsBus")]
        public AvionicsBus avionicsBus;

        [Header("写值演示")]
        [Tooltip("写值间隔（秒），0 = 不写")]
        public float writeInterval = 1f;

        [Tooltip("勾选则写完让 bus 通知订阅者，不勾选则静默写入")]
        public bool writeWithNotify = true;

        // 初始化时从 bus 复制一次数组引用；要用到哪几种类型就缓存哪几种
        private float[] _floatData;
        private bool[] _boolData;

        private float _writeTimer;

        private void Start()
        {
            if (avionicsBus == null) avionicsBus = GetComponentInParent<AvionicsBus>();

            if (avionicsBus == null)
            {
                Debug.LogWarning("[Bus Sample] 找不到 AvionicsBus，NonClientConsumer 不工作");

                return;
            }

            // 只跨 vm 这一次，之后全部走本地引用
            // （AvionicsBus 的 DefaultExecutionOrder 是 -100，此时它已经初始化完毕）
            _floatData = avionicsBus.floatData;
            _boolData = avionicsBus.boolData;

            // 订阅必须登记到 bus 的订阅表上，这是初始化期的一次性开销
            avionicsBus._SubscribeBool((int)AvionicsBusBoolDataIds.V32NN_Frequent_ADR_IsDataValid, this, nameof(BusSample_OnDataValidChanged));
            avionicsBus._SubscribeFloat((int)AvionicsBusFloatDataIds.V32NN_Infrequent_FCU_SelectedAltitudeFeet, this, nameof(BusSample_OnSelectedAltitudeChanged));

            Debug.Log("[Bus Sample] NonClientConsumer 订阅完成，当前高度 = "
                + _floatData[(int)AvionicsBusFloatDataIds.V32NN_Frequent_ADR_AltitudeFeet]);
        }

        private void Update()
        {
            if (_floatData == null) return;
            if (writeInterval <= 0f) return;

            _writeTimer += Time.deltaTime;
            if (_writeTimer < writeInterval) return;

            _writeTimer = 0f;

            int id = (int)AvionicsBusFloatDataIds.V32NN_Infrequent_FCU_SelectedAltitudeFeet;

            // 本地数组写入，零跨 vm
            _floatData[id] = 20000f + Time.time * 10f;

            // 想让订阅者收到通知时才付出这次调用
            if (writeWithNotify) avionicsBus._NotifyFloat(id);
        }

        #region 事件接收方法（必须是 public）

        public void BusSample_OnDataValidChanged()
        {
            Debug.Log("[Bus Sample] NonClientConsumer: IsDataValid = "
                + _boolData[(int)AvionicsBusBoolDataIds.V32NN_Frequent_ADR_IsDataValid]);
        }

        public void BusSample_OnSelectedAltitudeChanged()
        {
            Debug.Log("[Bus Sample] NonClientConsumer: FCU 选定高度 = "
                + _floatData[(int)AvionicsBusFloatDataIds.V32NN_Infrequent_FCU_SelectedAltitudeFeet]);
        }

        #endregion
    }
}
