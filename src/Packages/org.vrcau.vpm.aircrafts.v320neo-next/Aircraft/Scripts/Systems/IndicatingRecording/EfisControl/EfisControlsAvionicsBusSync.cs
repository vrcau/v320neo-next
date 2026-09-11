using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VAU.V320NeoNext.Runtime.Bus;

namespace VAU.V320NeoNext.Runtime.Systems.IndicatingRecording.EfisControl
{
    /// <summary>
    /// 把本侧 EFIS 控制面板对应的 bus 变量（ND 滤波器 / 页面 / 距离 / VOR-ADF 选择 + FD / LS 按钮）
    /// 打包进一个 ushort 做手动同步，并在收到远端数据时写回本机 bus。
    /// <para>
    /// 数据流：
    /// <list type="bullet">
    /// <item><description>本机任一项变化 → 订阅回调 → 抢所有权 → 打包 + RequestSerialization()</description></item>
    /// <item><description>收到远端数据 → OnDeserialization() → 解包 → _WriteAndNotifyXxx 写回本机 bus，
    /// 本机上订阅了这些 id 的仪表会跟着更新</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// 所有权与循环防护沿用仓库里 A320LandingLightToggle 的做法：改动前先 EnsureOwnership()，
    /// 只有 owner 才写 _data 并请求同步；应用远端数据时用 _isApplyingSyncedData 挡住
    /// “写回 bus → 又触发本组件回调 → 再次请求同步”的循环。
    /// </para>
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public sealed class EfisControlsAvionicsBusSync : AbstractAvionicsBusClient
    {
        /// <summary>true = 同步右侧 EFIS，false = 左侧。</summary>
        public bool isRightEfis;

        /// <summary>
        /// 打包后的同步数据：bit0 FD、bit1 LS、bit2-4 filter、bit5-7 page、bit8-10 range、bit11-12 VOR/ADF。
        /// </summary>
        [NonSerialized] [UdonSynced] private ushort _data;

        // 本侧对应的 bus id，在 _OnAvionicsBusStart() 里按 isRightEfis 解析一次
        private AvionicsBusByteDataIds _filterId;
        private AvionicsBusByteDataIds _pageId;
        private AvionicsBusByteDataIds _rangeId;
        private AvionicsBusByteDataIds _vorAdfId;
        private AvionicsBusBoolDataIds _flightDirectorId;
        private AvionicsBusBoolDataIds _landingSystemId;

        // 正在把远端数据写回 bus 时置位，避免自己的订阅回调又把同样的值同步出去
        private bool _isApplyingSyncedData;

        #region Bus lifetime

        protected override void _OnAvionicsBusPostStart()
        {
            _ResolveDataIds();

            // 六项全部指向同一个回调：任一项变化就重新打包整份状态（读的都是本地数组，开销可忽略）
            _SubscribeByte(_filterId, nameof(_OnEfisControlsChanged));
            _SubscribeByte(_pageId, nameof(_OnEfisControlsChanged));
            _SubscribeByte(_rangeId, nameof(_OnEfisControlsChanged));
            _SubscribeByte(_vorAdfId, nameof(_OnEfisControlsChanged));
            _SubscribeBool(_flightDirectorId, nameof(_OnEfisControlsChanged));
            _SubscribeBool(_landingSystemId, nameof(_OnEfisControlsChanged));
        }

        #endregion

        #region 同步出入口

        /// <summary>bus 里任一项变化时由 Bus 回调：重新打包并请求同步。</summary>
        public void _OnEfisControlsChanged()
        {
            // 正在应用远端数据，不要回写
            if (_isApplyingSyncedData) return;

            // 和已同步的值一致就什么都不做：既不抢所有权也不发同步请求
            ushort packed = _PackFromBus();
            if (packed == _data) return;

            EnsureOwnership();

            _data = packed;
            RequestSerialization();
        }

        public override void OnDeserialization()
        {
            _ApplySyncedDataToBus();
        }

        private void EnsureOwnership()
        {
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }

        #endregion

        #region bus <-> _data

        private void _ResolveDataIds()
        {
            if (isRightEfis)
            {
                _filterId = AvionicsBusByteDataIds.V32NN_Infrequent_EFIS_Right_Sync_NavigationDisplayFilter;
                _pageId = AvionicsBusByteDataIds.V32NN_Infrequent_EFIS_Right_Sync_NavigationDisplayPage;
                _rangeId = AvionicsBusByteDataIds.V32NN_Infrequent_EFIS_Right_Sync_NavigationDisplayRange;
                _vorAdfId = AvionicsBusByteDataIds.V32NN_Infrequent_EFIS_Right_Sync_NavigationDisplayVorAdfSelector;

                _flightDirectorId = AvionicsBusBoolDataIds.V32NN_Infrequent_EFIS_Right_Sync_FlightDirectorOn;
                _landingSystemId = AvionicsBusBoolDataIds.V32NN_Infrequent_EFIS_Right_Sync_LandingSystemOn;
            }
            else
            {
                _filterId = AvionicsBusByteDataIds.V32NN_Infrequent_EFIS_Left_Sync_NavigationDisplayFilter;
                _pageId = AvionicsBusByteDataIds.V32NN_Infrequent_EFIS_Left_Sync_NavigationDisplayPage;
                _rangeId = AvionicsBusByteDataIds.V32NN_Infrequent_EFIS_Left_Sync_NavigationDisplayRange;
                _vorAdfId = AvionicsBusByteDataIds.V32NN_Infrequent_EFIS_Left_Sync_NavigationDisplayVorAdfSelector;

                _flightDirectorId = AvionicsBusBoolDataIds.V32NN_Infrequent_EFIS_Left_Sync_FlightDirectorOn;
                _landingSystemId = AvionicsBusBoolDataIds.V32NN_Infrequent_EFIS_Left_Sync_LandingSystemOn;
            }
        }

        private ushort _PackFromBus()
        {
            var flagsBits = 0;

            if (_ReadBool(_flightDirectorId))
                flagsBits |= (int)FlightDirectorLandingSystemFlags.FlightDirectorOn;

            if (_ReadBool(_landingSystemId))
                flagsBits |= (int)FlightDirectorLandingSystemFlags.LandingSystemOn;

            return Pack(
                (FlightDirectorLandingSystemFlags)flagsBits,
                (NavigationDisplayFilter)_ReadByte(_filterId),
                (NavigationDisplayPage)_ReadByte(_pageId),
                (NavigationDisplayRange)_ReadByte(_rangeId),
                (NavigationDisplayVorAdfSelector)_ReadByte(_vorAdfId));
        }

        private void _ApplySyncedDataToBus()
        {
            Unpack(_data, out var flags, out var filter, out var page, out var range, out var vorAdf);

            var flagsBits = (int)flags;

            _isApplyingSyncedData = true;

            // 用 WriteAndNotify：本机上订阅了这些 id 的仪表也会收到通知
            _WriteAndNotifyBool(_flightDirectorId,
                (flagsBits & (int)FlightDirectorLandingSystemFlags.FlightDirectorOn) != 0);
            _WriteAndNotifyBool(_landingSystemId,
                (flagsBits & (int)FlightDirectorLandingSystemFlags.LandingSystemOn) != 0);
            _WriteAndNotifyByte(_filterId, Convert.ToByte(filter));
            _WriteAndNotifyByte(_pageId, Convert.ToByte(page));
            _WriteAndNotifyByte(_rangeId, Convert.ToByte(range));
            _WriteAndNotifyByte(_vorAdfId, Convert.ToByte(vorAdf));

            _isApplyingSyncedData = false;
        }

        #endregion

        private const int FilterShift = 2;
        private const int PageShift = 5;
        private const int RangeShift = 8;
        private const int VorAdfShift = 11;

        private static ushort Pack(
            FlightDirectorLandingSystemFlags flags,
            NavigationDisplayFilter filter,
            NavigationDisplayPage page,
            NavigationDisplayRange range,
            NavigationDisplayVorAdfSelector vorAdf)
        {
            var flagsBits = 0;
            var flagsValue = (int)flags;

            if ((flagsValue & (int)FlightDirectorLandingSystemFlags.FlightDirectorOn) != 0)
                flagsBits |= 1 << 0;

            if ((flagsValue & (int)FlightDirectorLandingSystemFlags.LandingSystemOn) != 0)
                flagsBits |= 1 << 1;

            return Convert.ToUInt16(
                flagsBits |
                (((int)filter & 0b111) << FilterShift) |
                (((int)page & 0b111) << PageShift) |
                (((int)range & 0b111) << RangeShift) |
                (((int)vorAdf & 0b11) << VorAdfShift)
            );
        }

        private static void Unpack(
            ushort packed,
            out FlightDirectorLandingSystemFlags flags,
            out NavigationDisplayFilter filter,
            out NavigationDisplayPage page,
            out NavigationDisplayRange range,
            out NavigationDisplayVorAdfSelector vorAdf)
        {
            var flagsBits = 0;
            var temp = Convert.ToInt32(packed);

            if ((temp & (1 << 0)) != 0)
                flagsBits |= (int)FlightDirectorLandingSystemFlags.FlightDirectorOn;

            if ((temp & (1 << 1)) != 0)
                flagsBits |= (int)FlightDirectorLandingSystemFlags.LandingSystemOn;

            flags = (FlightDirectorLandingSystemFlags)flagsBits;

            filter = (NavigationDisplayFilter)((temp >> FilterShift) & 0b111);
            page = (NavigationDisplayPage)((temp >> PageShift) & 0b111);
            range = (NavigationDisplayRange)((temp >> RangeShift) & 0b111);
            vorAdf = (NavigationDisplayVorAdfSelector)((temp >> VorAdfShift) & 0b11);
        }
    }
}