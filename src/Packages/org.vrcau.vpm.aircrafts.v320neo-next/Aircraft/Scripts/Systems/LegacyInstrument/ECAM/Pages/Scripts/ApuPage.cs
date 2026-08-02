using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VAU.V320NeoNext.Runtime.Systems.AuxiliaryPowerUnit;
using VAU.V320NeoNext.Runtime.Systems.LegacyFlightDataProvider;

namespace VAU.V320NeoNext.Runtime.Systems.LegacyInstrument.ECAM.Pages.Scripts {
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ApuPage : ECAMPage {
        private DependenciesInjector _injector;
        private AircraftSystemData _aircraftSystemData;
        private SFEXT_AuxiliaryPowerUnit _apu;

        public GameObject apuAvailText;
        public GameObject flapOpenText;
        public Text apuNText;

        private bool _initialized;

        private void Start() {
            _injector = DependenciesInjector.GetInstance(this);
            _aircraftSystemData = _injector.equipmentData;
            _apu = _injector.apu;

            _initialized = true;
        }

        public override void OnPageUpdate() {
            if (!_initialized) return;

            flapOpenText.SetActive(_aircraftSystemData.isApuRunning);
            apuAvailText.SetActive(_aircraftSystemData.isApuStarted);

            apuNText.text = ((int)(_apu.apuAudioSource.volume * 100f)).ToString();
        }
    }
}