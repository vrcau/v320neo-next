using UdonSharp;
using UnityEngine;
using VAU.V320NeoNext.Runtime.Systems.AuxiliaryPowerUnit;
using VAU.V320NeoNext.Runtime.Systems.LegacyFlightDataProvider;

namespace VAU.V320NeoNext.Runtime.Systems.LegacyInstrument.Button.APU {
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class APUPanel : UdonSharpBehaviour {
        private SFEXT_AuxiliaryPowerUnit _auxiliaryPowerUnit;
        private AircraftSystemData _aircraftSystemData;
        private DependenciesInjector _dependenciesInjector;

        public Animator indicatorAnimator;

        private readonly int IS_APU_MASTER_SWITCHER_ON_HASH = Animator.StringToHash("IsAPUMasterSwitcherOn");
        private readonly int IS_APU_AVAIL = Animator.StringToHash("IsApuAvail");
        private readonly int IS_APU_STARTER_ON = Animator.StringToHash("IsApuStarterOn");

        private void Start() {
            _dependenciesInjector = DependenciesInjector.GetInstance(this);

            _auxiliaryPowerUnit = _dependenciesInjector.apu;
            _aircraftSystemData = _dependenciesInjector.equipmentData;
        }

        private void LateUpdate() {
            indicatorAnimator.SetBool(IS_APU_STARTER_ON,
                _aircraftSystemData.isApuRunning && !_aircraftSystemData.isApuStarted);
            indicatorAnimator.SetBool(IS_APU_AVAIL, _aircraftSystemData.isApuStarted);
            indicatorAnimator.SetBool(IS_APU_MASTER_SWITCHER_ON_HASH, _aircraftSystemData.isApuRunning);
        }
    }
}