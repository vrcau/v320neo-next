using JetBrains.Annotations;
using UdonRadioCommunicationRedux;
using UdonSharp;
using UnityEngine;
using VAU.V320NeoNext.Runtime.Systems.LegacyFlightDataProvider;
using VirtualCNS;

namespace VAU.V320NeoNext.Runtime.AircraftSetup
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public sealed class VAU320LocalAircraftSettings : UdonSharpBehaviour
    {
        #region Override Settings Fields

        [Header("Override Global Settings")] public NavaidDatabase navaidDatabaseOverride;
        public VoiceBroadcastByChannel voiceProtocolForVhfOverride;

        [Header(
            "Usually this field will be set to global settings behaviour in GameObject with name of VAU320GlobalAircraftSettings during runtime Start()"
        )]
        public VAU320GlobalAircraftSettings globalAircraftSettings;

        #endregion

        #region For Aircraft Developer Only

        [Header("For Aircraft Developer Only")]
        public DependenciesInjector dependenciesInjector;

        public Transceiver udonRadioCommunicationTransceiver;

        public NavSelector vor1Selector;
        public NavSelector vor2Selector;
        public NavSelector ilsSelector;

        #endregion

        #region Runtime Init Fields

        private bool _isRuntimeInit;
        [PublicAPI] public NavaidDatabase RuntimeNavaidDatabase { get; private set; }
        [PublicAPI] public VoiceBroadcastByChannel RuntimeVoiceProtocolForVhf { get; private set; }
        [PublicAPI] public int Vor1DefaultNavaidIndex { get; private set; }
        [PublicAPI] public int Vor2DefaultNavaidIndex { get; private set; }
        [PublicAPI] public int IlsDefaultNavaidIndex { get; private set; }

        #endregion

        private void Start()
        {
            Setup();
            // Do null check only once for performance
            // This will also set child behavior fields if necessary
            RuntimeInit();
        }

        private void Setup()
        {
            if (globalAircraftSettings) return;

            globalAircraftSettings = GameObject.Find(nameof(VAU320GlobalAircraftSettings))
                .GetComponent<VAU320GlobalAircraftSettings>();
            if (!globalAircraftSettings)
                Debug.LogWarning(
                    $"No {nameof(VAU320GlobalAircraftSettings)} found in scene. Please ensure that a global aircraft settings object is present.");
        }

        private void RuntimeInit()
        {
            if (_isRuntimeInit) return;

            RuntimeNavaidDatabase =
                navaidDatabaseOverride ? navaidDatabaseOverride : globalAircraftSettings.navaidDatabase;
            RuntimeVoiceProtocolForVhf =
                voiceProtocolForVhfOverride ? voiceProtocolForVhfOverride : globalAircraftSettings.voiceProtocolForVhf;

            Vor1DefaultNavaidIndex = globalAircraftSettings.vor1DefaultNavaidIndex;
            Vor2DefaultNavaidIndex = globalAircraftSettings.vor2DefaultNavaidIndex;
            IlsDefaultNavaidIndex = globalAircraftSettings.ilsDefaultNavaidIndex;

            dependenciesInjector.navaidDatabase = RuntimeNavaidDatabase;
            udonRadioCommunicationTransceiver.channelManager = RuntimeVoiceProtocolForVhf;

            vor1Selector.defaultIndex = Vor1DefaultNavaidIndex;
            vor2Selector.defaultIndex = Vor2DefaultNavaidIndex;
            ilsSelector.defaultIndex = IlsDefaultNavaidIndex;

            _isRuntimeInit = true;
        }
    }
}