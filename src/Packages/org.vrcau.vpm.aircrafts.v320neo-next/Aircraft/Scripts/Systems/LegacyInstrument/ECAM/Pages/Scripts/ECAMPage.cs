using UdonSharp;
using VAU.V320NeoNext.Runtime.Systems.LegacyInstrument.ECAM.Script;

namespace VAU.V320NeoNext.Runtime.Systems.LegacyInstrument.ECAM.Pages.Scripts {
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ECAMPage : UdonSharpBehaviour {
        public virtual void OnPageInit(ECAMDisplay ecamDisplay) {}
        public virtual void OnPageUpdate() { }
    }
}