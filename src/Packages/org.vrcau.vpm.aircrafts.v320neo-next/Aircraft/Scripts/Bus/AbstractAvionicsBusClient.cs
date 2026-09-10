using System;
using UdonSharp;
using UnityEngine;

namespace VAU.V320NeoNext.Runtime.Bus
{
    /// <summary>
    /// 所有需要访问 <see cref="AvionicsBus"/> 的类都继承这个类。
    /// <para>
    /// 生命周期完全由 AvionicsBus 驱动：Bus 会注入自己的引用，调用
    /// <see cref="_AvionicsBusStart"/>() 复制数据数组引用，然后触发
    /// <see cref="_OnAvionicsBusStart"/>() 让子类做订阅注册之类的自定义逻辑。
    /// 飞机重生时 Bus 会调用 <see cref="_AvionicsBusResponse"/>()，进而触发
    /// <see cref="_OnAvionicsBusRespawn"/>()。
    /// </para>
    /// <para>
    /// 注意：数据数组引用在 <see cref="_AvionicsBusStart"/>() 之前都是 null，
    /// 所以不要在 Start()/Awake() 里读写 Bus 数据，请使用 <see cref="_OnAvionicsBusStart"/>()。
    /// </para>
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public abstract class AbstractAvionicsBusClient : UdonSharpBehaviour
    {
        /// <summary>由 AvionicsBus 在初始化时注入，不要手动赋值。</summary>
        [HideInInspector] public AvionicsBus _avionicsBus;

        // 数据数组引用，在 _AvionicsBusStart() 中从 Bus 复制；之后读写都是直接操作 Bus 的同一份数组。
        // public 是为了让 AbstractAvionicsBusClientExtensions 能直接使用（否则每次读写都要跳一次 bus）；
        // [NonSerialized] 防止它们被序列化进 prefab。
        [NonSerialized] public float[] _floatData;
        [NonSerialized] public int[] _intData;
        [NonSerialized] public bool[] _boolData;
        [NonSerialized] public string[] _stringData;
        [NonSerialized] public Vector3[] _vector3Data;

        #region Lifetime

        /// <summary>
        /// 由 AvionicsBus 调用，不要手动调用，也不要在子类里重写。
        /// </summary>
        public void _AvionicsBusStart()
        {
            if (_avionicsBus == null) return;

            _floatData = _avionicsBus.floatData;
            _intData = _avionicsBus.intData;
            _boolData = _avionicsBus.boolData;
            _stringData = _avionicsBus.stringData;
            _vector3Data = _avionicsBus.vector3Data;

            _OnAvionicsBusStart();
        }

        /// <summary>
        /// 子类重写：在这里做订阅注册之类的自定义初始化逻辑。
        /// 注意此时 Bus 的订阅表已经清空重建，直接 _SubscribeXxx 即可。
        /// </summary>
        protected virtual void _OnAvionicsBusStart() { }

        /// <summary>
        /// 由 AvionicsBus 在飞机重生时调用。
        /// </summary>
        public void _AvionicsBusResponse()
        {
            // 客户端自身通用的 Respawn 逻辑写在这里（目前没有）
            _OnAvionicsBusRespawn();
        }

        /// <summary>
        /// 子类重写：在这里做重生后的自定义逻辑。
        /// </summary>
        protected virtual void _OnAvionicsBusRespawn() { }

        #endregion

        #region Read

        protected float _ReadFloat(AvionicsBusFloatDataIds id) { return _floatData[(int)id]; }
        protected int _ReadInt(AvionicsBusIntDataIds id) { return _intData[(int)id]; }
        protected bool _ReadBool(AvionicsBusBoolDataIds id) { return _boolData[(int)id]; }
        protected string _ReadString(AvionicsBusStringDataIds id) { return _stringData[(int)id]; }
        protected Vector3 _ReadVector3(AvionicsBusVector3DataIds id) { return _vector3Data[(int)id]; }

        #endregion

        #region Write

        protected void _WriteFloat(AvionicsBusFloatDataIds id, float value) { _floatData[(int)id] = value; }
        protected void _WriteInt(AvionicsBusIntDataIds id, int value) { _intData[(int)id] = value; }
        protected void _WriteBool(AvionicsBusBoolDataIds id, bool value) { _boolData[(int)id] = value; }
        protected void _WriteString(AvionicsBusStringDataIds id, string value) { _stringData[(int)id] = value; }
        protected void _WriteVector3(AvionicsBusVector3DataIds id, Vector3 value) { _vector3Data[(int)id] = value; }

        protected void _WriteAndNotifyFloat(AvionicsBusFloatDataIds id, float value)
        {
            _floatData[(int)id] = value;
            _avionicsBus._NotifyFloat((int)id);
        }

        protected void _WriteAndNotifyInt(AvionicsBusIntDataIds id, int value)
        {
            _intData[(int)id] = value;
            _avionicsBus._NotifyInt((int)id);
        }

        protected void _WriteAndNotifyBool(AvionicsBusBoolDataIds id, bool value)
        {
            _boolData[(int)id] = value;
            _avionicsBus._NotifyBool((int)id);
        }

        protected void _WriteAndNotifyString(AvionicsBusStringDataIds id, string value)
        {
            _stringData[(int)id] = value;
            _avionicsBus._NotifyString((int)id);
        }

        protected void _WriteAndNotifyVector3(AvionicsBusVector3DataIds id, Vector3 value)
        {
            _vector3Data[(int)id] = value;
            _avionicsBus._NotifyVector3((int)id);
        }

        #endregion

        #region Subscribe

        /// <summary>
        /// 订阅某个数据 id，当数据被 _WriteAndNotifyXxx 更新时，Bus 会对本客户端
        /// SendCustomEvent(eventName)。所以 eventName 必须是本类或其子类上的 public 方法名。
        /// </summary>
        protected void _SubscribeFloat(AvionicsBusFloatDataIds id, string eventName) { _avionicsBus._SubscribeFloat((int)id, this, eventName); }
        protected void _SubscribeInt(AvionicsBusIntDataIds id, string eventName) { _avionicsBus._SubscribeInt((int)id, this, eventName); }
        protected void _SubscribeBool(AvionicsBusBoolDataIds id, string eventName) { _avionicsBus._SubscribeBool((int)id, this, eventName); }
        protected void _SubscribeString(AvionicsBusStringDataIds id, string eventName) { _avionicsBus._SubscribeString((int)id, this, eventName); }
        protected void _SubscribeVector3(AvionicsBusVector3DataIds id, string eventName) { _avionicsBus._SubscribeVector3((int)id, this, eventName); }

        #endregion
    }
}
