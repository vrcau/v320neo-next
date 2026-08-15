using UdonRadioCommunicationRedux;
using UdonSharp;
using UnityEngine;
using VirtualCNS;

namespace VAU.V320NeoNext.Runtime.AircraftSetup
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public sealed class VAU320GlobalAircraftSettings : UdonSharpBehaviour
    {
        [Header("Navaid")]
        public NavaidDatabase navaidDatabase;
        public int vor1DefaultNavaidIndex;
        public int vor2DefaultNavaidIndex;
        public int ilsDefaultNavaidIndex;

        [Header("Udon Radio Communication")]
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
