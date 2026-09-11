using System;
using UdonSharp;
using UnityEngine;
using VAU.V320NeoNext.Runtime.Extensions;

namespace VAU.V320NeoNext.Runtime.Bus
{
    /// <summary>
    /// 航电数据总线：持有各基本类型的数据数组（Data Holder），维护每个数据 id 的订阅者，
    /// 并在启动/重生时驱动层级下所有的 <see cref="AbstractAvionicsBusClient"/>。
    /// <para>数据 id 及其命名约定见 AvionicsBusDataIds.cs。</para>
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(-100)] // before all aircraft scripts
    public class AvionicsBus : UdonSharpBehaviour
    {
        #region Data holder

        // 数组下标即对应 enum 的 id；初始化阶段会把引用复制给各 client，
        // 之后 client 的读写都是直接操作这些数组（不走方法调用），所以要保持 public。
        [NonSerialized] public readonly float[] floatData = new float[(int)AvionicsBusFloatDataIds.Count];
        [NonSerialized] public readonly byte[] byteData = new byte[(int)AvionicsBusByteDataIds.Count];
        [NonSerialized] public readonly int[] intData = new int[(int)AvionicsBusIntDataIds.Count];
        [NonSerialized] public readonly bool[] boolData = new bool[(int)AvionicsBusBoolDataIds.Count];
        [NonSerialized] public readonly string[] stringData = new string[(int)AvionicsBusStringDataIds.Count];
        [NonSerialized] public readonly Vector3[] vector3Data = new Vector3[(int)AvionicsBusVector3DataIds.Count];

        #endregion

        #region Subscribers

        // 每个 data id 对应两个并行交错数组：下标 [dataId][n] 分别是第 n 个订阅者本体和它注册的事件名。
        // 刻意不用 object[]：Udon 里把 UdonSharpBehaviour 装箱进 object 再强转回来会抛 InvalidCastException，
        // 用强类型数组后派发路径上没有任何强制转换。
        private UdonSharpBehaviour[][] _floatSubscriberTargets;
        private string[][] _floatSubscriberEvents;

        private UdonSharpBehaviour[][] _intSubscriberTargets;
        private string[][] _intSubscriberEvents;

        private UdonSharpBehaviour[][] _byteSubscriberTargets;
        private string[][] _byteSubscriberEvents;

        private UdonSharpBehaviour[][] _boolSubscriberTargets;
        private string[][] _boolSubscriberEvents;

        private UdonSharpBehaviour[][] _stringSubscriberTargets;
        private string[][] _stringSubscriberEvents;

        private UdonSharpBehaviour[][] _vector3SubscriberTargets;
        private string[][] _vector3SubscriberEvents;

        // Bus 管理的客户端，注意不要和上面的订阅者数组搞混了
        private AbstractAvionicsBusClient[] _clients = { };

        #endregion

        #region Lifetime

        private void Start()
        {
            _AvionicsBusStart();
        }

        /// <summary>
        /// 启动 Bus：清空重建订阅表，收集层级下所有 client 并驱动它们初始化。
        /// 重复调用是安全的（订阅表会被重建），因此 client 只需要在
        /// _OnAvionicsBusStart() 里注册订阅。
        /// </summary>
        public void _AvionicsBusStart()
        {
            _ClearSubscribers();

            _clients = GetComponentsInChildren<AbstractAvionicsBusClient>(true);

            for (int i = 0; i < _clients.Length; i++)
            {
                AbstractAvionicsBusClient client = _clients[i];
                if (client == null) continue;

                client._avionicsBus = this;
                client._AvionicsBusStart();
            }
        }

        /// <summary>
        /// 由本地玩家触发的飞机重生：通知所有 client 执行各自的 Response 逻辑。
        /// </summary>
        public void _AvionicsBusRespawnByLocalPlayer()
        {
            for (int i = 0; i < _clients.Length; i++)
            {
                if (_clients[i] == null) continue;

                _clients[i]._AvionicsBusRespawnByLocalPlayer();
            }
        }

        public void _AvionicsBusRespawnByRemotePlayer()
        {
            for (int i = 0; i < _clients.Length; i++)
            {
                if (_clients[i] == null) continue;

                _clients[i]._AvionicsBusRespawnByRemotePlayer();
            }
        }

        private void _ClearSubscribers()
        {
            _floatSubscriberTargets = _CreateTargetTable((int)AvionicsBusFloatDataIds.Count);
            _floatSubscriberEvents = _CreateEventTable((int)AvionicsBusFloatDataIds.Count);

            _byteSubscriberTargets = _CreateTargetTable((int)AvionicsBusByteDataIds.Count);
            _byteSubscriberEvents = _CreateEventTable((int)AvionicsBusByteDataIds.Count);

            _intSubscriberTargets = _CreateTargetTable((int)AvionicsBusIntDataIds.Count);
            _intSubscriberEvents = _CreateEventTable((int)AvionicsBusIntDataIds.Count);

            _boolSubscriberTargets = _CreateTargetTable((int)AvionicsBusBoolDataIds.Count);
            _boolSubscriberEvents = _CreateEventTable((int)AvionicsBusBoolDataIds.Count);

            _stringSubscriberTargets = _CreateTargetTable((int)AvionicsBusStringDataIds.Count);
            _stringSubscriberEvents = _CreateEventTable((int)AvionicsBusStringDataIds.Count);

            _vector3SubscriberTargets = _CreateTargetTable((int)AvionicsBusVector3DataIds.Count);
            _vector3SubscriberEvents = _CreateEventTable((int)AvionicsBusVector3DataIds.Count);
        }

        private UdonSharpBehaviour[][] _CreateTargetTable(int count)
        {
            UdonSharpBehaviour[][] table = new UdonSharpBehaviour[count][];

            for (int i = 0; i < count; i++) table[i] = new UdonSharpBehaviour[0];

            return table;
        }

        private string[][] _CreateEventTable(int count)
        {
            string[][] table = new string[count][];

            for (int i = 0; i < count; i++) table[i] = new string[0];

            return table;
        }

        #endregion

        #region Subscribe

        /// <summary>注册订阅者，由 AbstractAvionicsBusClient._SubscribeFloat 转发调用。</summary>
        public void _SubscribeFloat(int id, UdonSharpBehaviour subscriber, string eventName) { _Subscribe(_floatSubscriberTargets, _floatSubscriberEvents, id, subscriber, eventName); }
        public void _SubscribeByte(int id, UdonSharpBehaviour subscriber, string eventName) { _Subscribe(_byteSubscriberTargets, _byteSubscriberEvents, id, subscriber, eventName); }
        public void _SubscribeInt(int id, UdonSharpBehaviour subscriber, string eventName) { _Subscribe(_intSubscriberTargets, _intSubscriberEvents, id, subscriber, eventName); }
        public void _SubscribeBool(int id, UdonSharpBehaviour subscriber, string eventName) { _Subscribe(_boolSubscriberTargets, _boolSubscriberEvents, id, subscriber, eventName); }
        public void _SubscribeString(int id, UdonSharpBehaviour subscriber, string eventName) { _Subscribe(_stringSubscriberTargets, _stringSubscriberEvents, id, subscriber, eventName); }
        public void _SubscribeVector3(int id, UdonSharpBehaviour subscriber, string eventName) { _Subscribe(_vector3SubscriberTargets, _vector3SubscriberEvents, id, subscriber, eventName); }

        private void _Subscribe(UdonSharpBehaviour[][] targets, string[][] events, int id, UdonSharpBehaviour subscriber, string eventName)
        {
            if (targets == null || events == null) return;
            if (id < 0 || id >= targets.Length) return;
            if (subscriber == null || string.IsNullOrEmpty(eventName)) return;

            targets[id] = targets[id].Add(subscriber);
            events[id] = events[id].Add(eventName);
        }

        #endregion

        #region Notify

        /// <summary>数据变更后通知该 id 的所有订阅者，由 _WriteAndNotifyXxx 触发。</summary>
        public void _NotifyFloat(int id) { _Notify(_floatSubscriberTargets, _floatSubscriberEvents, id); }
        public void _NotifyByte(int id) { _Notify(_byteSubscriberTargets, _byteSubscriberEvents, id); }
        public void _NotifyInt(int id) { _Notify(_intSubscriberTargets, _intSubscriberEvents, id); }
        public void _NotifyBool(int id) { _Notify(_boolSubscriberTargets, _boolSubscriberEvents, id); }
        public void _NotifyString(int id) { _Notify(_stringSubscriberTargets, _stringSubscriberEvents, id); }
        public void _NotifyVector3(int id) { _Notify(_vector3SubscriberTargets, _vector3SubscriberEvents, id); }

        private void _Notify(UdonSharpBehaviour[][] targets, string[][] events, int id)
        {
            if (targets == null || events == null) return;
            if (id < 0 || id >= targets.Length) return;

            UdonSharpBehaviour[] subscribers = targets[id];
            string[] eventNames = events[id];
            if (subscribers == null) return;

            // 订阅表在 _Subscribe 里总是成对追加，长度理论上一致，这里取小值兜底
            int count = subscribers.Length < eventNames.Length ? subscribers.Length : eventNames.Length;

            for (int i = 0; i < count; i++)
            {
                UdonSharpBehaviour subscriber = subscribers[i];
                if (subscriber == null) continue;

                string eventName = eventNames[i];
                if (string.IsNullOrEmpty(eventName)) continue;

                subscriber.SendCustomEvent(eventName);
            }
        }

        #endregion
    }
}