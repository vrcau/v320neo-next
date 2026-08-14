using UdonRadioCommunicationRedux;
using UdonSharp;
using VirtualCNS;

namespace VAU.V320NeoNext.Runtime.AircraftSetup
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public sealed class VAU320GlobalAircraftSettings : UdonSharpBehaviour
    {
        public NavaidDatabase navaidDatabase;
        public VoiceBroadcastByChannel voiceProtocolForVhf;

        private void Start()
        {
            Setup();
        }

        internal void Setup()
        {
            if (!navaidDatabase)
            {
                navaidDatabase = NavaidDatabase.GetInstance();
            }
        }
    }
}
