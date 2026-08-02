using UdonSharp;

namespace VAU.V320NeoNext.Runtime.Systems.LegacFmgc {
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class FMGCFlightPlan : UdonSharpBehaviour {
        public FMGC fmgc;

        public int cruiseAltitude = 2000;

        public int takeoffAirportIndex = -1;
        public int arrivalAirportIndex = -1;
    }
}
