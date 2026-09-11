using System;
using UdonSharp;
using VAU.V320NeoNext.Runtime.Bus;

namespace VAU.V320NeoNext.Runtime.Systems.IndicatingRecording.EfisControl
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public sealed class EfisLifetimeController : AbstractAvionicsBusClient
    {
        public bool isRightEfis;

        public bool flightDirectorDefaultOn = true;
        public bool landingSystemDefaultOn;

        public NavigationDisplayFilter navigationDisplayFilterDefault = NavigationDisplayFilter.Constraint;
        public NavigationDisplayPage navigationDisplayPageDefault = NavigationDisplayPage.Arc;
        public NavigationDisplayRange navigationDisplayRangeDefault = NavigationDisplayRange.Range20Nm;

        public NavigationDisplayVorAdfSelector vorAdfSelectorDefault = NavigationDisplayVorAdfSelector.Vor;

        private AvionicsBusBoolDataIds _flightDirectorId;
        private AvionicsBusBoolDataIds _landingSystemId;

        private AvionicsBusByteDataIds _navigationDisplayFilterId;
        private AvionicsBusByteDataIds _navigationDisplayPageId;
        private AvionicsBusByteDataIds _navigationDisplayRangeId;

        private AvionicsBusByteDataIds _vorAdfSelectorId;

        protected override void _OnAvionicsBusStart()
        {
            _flightDirectorId = isRightEfis
                ? AvionicsBusBoolDataIds.V32NN_Infrequent_EFIS_Right_Sync_FlightDirectorOn
                : AvionicsBusBoolDataIds.V32NN_Infrequent_EFIS_Left_Sync_FlightDirectorOn;
            _landingSystemId = isRightEfis
                ? AvionicsBusBoolDataIds.V32NN_Infrequent_EFIS_Right_Sync_LandingSystemOn
                : AvionicsBusBoolDataIds.V32NN_Infrequent_EFIS_Left_Sync_LandingSystemOn;

            _navigationDisplayFilterId = isRightEfis
                ? AvionicsBusByteDataIds.V32NN_Infrequent_EFIS_Right_Sync_NavigationDisplayFilter
                : AvionicsBusByteDataIds.V32NN_Infrequent_EFIS_Left_Sync_NavigationDisplayFilter;
            _navigationDisplayPageId = isRightEfis
                ? AvionicsBusByteDataIds.V32NN_Infrequent_EFIS_Right_Sync_NavigationDisplayPage
                : AvionicsBusByteDataIds.V32NN_Infrequent_EFIS_Left_Sync_NavigationDisplayPage;
            _navigationDisplayRangeId = isRightEfis
                ? AvionicsBusByteDataIds.V32NN_Infrequent_EFIS_Right_Sync_NavigationDisplayRange
                : AvionicsBusByteDataIds.V32NN_Infrequent_EFIS_Left_Sync_NavigationDisplayRange;

            _vorAdfSelectorId = isRightEfis
                ? AvionicsBusByteDataIds.V32NN_Infrequent_EFIS_Right_Sync_NavigationDisplayVorAdfSelector
                : AvionicsBusByteDataIds.V32NN_Infrequent_EFIS_Left_Sync_NavigationDisplayVorAdfSelector;

            ResetStatusToDefault();
        }

        protected override void _OnAvionicsBusRespawnByLocalPlayer()
        {
            ResetStatusToDefault();
        }

        private void ResetStatusToDefault()
        {
            _WriteAndNotifyBool(_flightDirectorId, flightDirectorDefaultOn);
            _WriteAndNotifyBool(_landingSystemId, landingSystemDefaultOn);

            _WriteAndNotifyByte(_navigationDisplayFilterId, Convert.ToByte(navigationDisplayFilterDefault));
            _WriteAndNotifyByte(_navigationDisplayPageId, Convert.ToByte(navigationDisplayPageDefault));
            _WriteAndNotifyByte(_navigationDisplayRangeId, Convert.ToByte(navigationDisplayRangeDefault));

            _WriteAndNotifyByte(_vorAdfSelectorId, Convert.ToByte(vorAdfSelectorDefault));
        }
    }
}