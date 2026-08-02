using UdonSharp;
using VAU.V320NeoNext.Runtime.Systems.LegacFmgc;
using VAU.V320NeoNext.Runtime.Systems.LegacyFlightDataProvider;
using VAU.V320NeoNext.Runtime.Systems.LegacyInstrument.MCDU.Scripts;

namespace VAU.V320NeoNext.Runtime.Systems.LegacyInstrument.MCDU.Pages.Performance {
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public partial class PerformancePage : MCDUPage {
        private DependenciesInjector _injector;

        private FMGC _fmgc;

        private Scripts.MCDU _mcdu;

        private void Start() {
            _injector = DependenciesInjector.GetInstance(this);
            _fmgc = _injector.fmgc;
        }

        public override void OnPageInit(Scripts.MCDU mcdu) {
            _mcdu = mcdu;

            TakeoffUI();
        }
    }
}