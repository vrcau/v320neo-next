using UdonSharp;
using UnityEngine;

namespace VAU.V320NeoNext.Runtime.Bus
{
    /// <summary>
    /// 给 <see cref="AbstractAvionicsBusClient"/> 的便利扩展：直接用 enum 作为 id 做 Read / Write / WriteAndNotify / Subscribe。
    /// <para>
    /// 与基类里 protected 的 _ReadXxx / _WriteXxx / _WriteAndNotifyXxx / _SubscribeXxx 功能等价，
    /// 区别是这些是 <b>public 扩展方法</b>：
    /// <list type="bullet">
    /// <item><description>子类里直接写 ReadFloat(id) 即可，不用带下划线；</description></item>
    /// <item><description>任何持有 client 引用的脚本都能调 client.ReadFloat(id)，而且用的是 client <b>本地的数组引用</b>，
    /// 不需要为每次读写再跳一次 bus（这正是把数组引用复制到 client 的意义）。</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// 用法：
    /// <code>
    /// ReadFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ADR_AltitudeFeet);
    /// WriteFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ADR_AltitudeFeet, 12000f);
    /// WriteAndNotifyFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ADR_AltitudeFeet, 12000f);
    /// SubscribeFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ADR_AltitudeFeet, nameof(OnAltitudeChanged));
    /// </code>
    /// </para>
    /// <para>
    /// 注意：
    /// <list type="bullet">
    /// <item><description>数据数组引用在 <see cref="AbstractAvionicsBusClient._AvionicsBusStart"/>() 之后才有效，
    /// 所以要在 <see cref="AbstractAvionicsBusClient._OnAvionicsBusStart"/>() 或更晚的时机使用。</description></item>
    /// <item><description>UdonSharp 不支持方法重载，方法名必须带类型后缀（ReadFloat / ReadInt / ...）。</description></item>
    /// <item><description>只有 Subscribe 与 WriteAndNotify 會出现一次对 bus 的跳转（订阅表在 bus 上），这是避不开的；
    /// 纯 Read / Write 全程都是本地数组访问。</description></item>
    /// </list>
    /// </para>
    /// </summary>
    public static class AbstractAvionicsBusClientExtensions
    {
        #region Read

        public static float ReadFloat(this AbstractAvionicsBusClient client, AvionicsBusFloatDataIds id) { return client._floatData[(int)id]; }
        public static int ReadInt(this AbstractAvionicsBusClient client, AvionicsBusIntDataIds id) { return client._intData[(int)id]; }
        public static bool ReadBool(this AbstractAvionicsBusClient client, AvionicsBusBoolDataIds id) { return client._boolData[(int)id]; }
        public static string ReadString(this AbstractAvionicsBusClient client, AvionicsBusStringDataIds id) { return client._stringData[(int)id]; }
        public static Vector3 ReadVector3(this AbstractAvionicsBusClient client, AvionicsBusVector3DataIds id) { return client._vector3Data[(int)id]; }

        #endregion

        #region Write

        public static void WriteFloat(this AbstractAvionicsBusClient client, AvionicsBusFloatDataIds id, float value) { client._floatData[(int)id] = value; }
        public static void WriteInt(this AbstractAvionicsBusClient client, AvionicsBusIntDataIds id, int value) { client._intData[(int)id] = value; }
        public static void WriteBool(this AbstractAvionicsBusClient client, AvionicsBusBoolDataIds id, bool value) { client._boolData[(int)id] = value; }
        public static void WriteString(this AbstractAvionicsBusClient client, AvionicsBusStringDataIds id, string value) { client._stringData[(int)id] = value; }
        public static void WriteVector3(this AbstractAvionicsBusClient client, AvionicsBusVector3DataIds id, Vector3 value) { client._vector3Data[(int)id] = value; }

        #endregion

        #region WriteAndNotify

        public static void WriteAndNotifyFloat(this AbstractAvionicsBusClient client, AvionicsBusFloatDataIds id, float value)
        {
            client._floatData[(int)id] = value;
            client._avionicsBus._NotifyFloat((int)id);
        }

        public static void WriteAndNotifyInt(this AbstractAvionicsBusClient client, AvionicsBusIntDataIds id, int value)
        {
            client._intData[(int)id] = value;
            client._avionicsBus._NotifyInt((int)id);
        }

        public static void WriteAndNotifyBool(this AbstractAvionicsBusClient client, AvionicsBusBoolDataIds id, bool value)
        {
            client._boolData[(int)id] = value;
            client._avionicsBus._NotifyBool((int)id);
        }

        public static void WriteAndNotifyString(this AbstractAvionicsBusClient client, AvionicsBusStringDataIds id, string value)
        {
            client._stringData[(int)id] = value;
            client._avionicsBus._NotifyString((int)id);
        }

        public static void WriteAndNotifyVector3(this AbstractAvionicsBusClient client, AvionicsBusVector3DataIds id, Vector3 value)
        {
            client._vector3Data[(int)id] = value;
            client._avionicsBus._NotifyVector3((int)id);
        }

        #endregion

        #region Subscribe

        /// <summary>
        /// 以本 client 作为订阅者注册。eventName 必须是本类或其子类上的 public 方法名。
        /// 订阅表在每次 <see cref="AvionicsBus._AvionicsBusStart"/>() 时会被重建，
        /// 所以请在 <see cref="AbstractAvionicsBusClient._OnAvionicsBusStart"/>() 里注册。
        /// </summary>
        public static void SubscribeFloat(this AbstractAvionicsBusClient client, AvionicsBusFloatDataIds id, string eventName)
        {
            client._avionicsBus._SubscribeFloat((int)id, client, eventName);
        }

        public static void SubscribeInt(this AbstractAvionicsBusClient client, AvionicsBusIntDataIds id, string eventName)
        {
            client._avionicsBus._SubscribeInt((int)id, client, eventName);
        }

        public static void SubscribeBool(this AbstractAvionicsBusClient client, AvionicsBusBoolDataIds id, string eventName)
        {
            client._avionicsBus._SubscribeBool((int)id, client, eventName);
        }

        public static void SubscribeString(this AbstractAvionicsBusClient client, AvionicsBusStringDataIds id, string eventName)
        {
            client._avionicsBus._SubscribeString((int)id, client, eventName);
        }

        public static void SubscribeVector3(this AbstractAvionicsBusClient client, AvionicsBusVector3DataIds id, string eventName)
        {
            client._avionicsBus._SubscribeVector3((int)id, client, eventName);
        }

        #endregion
    }
}
