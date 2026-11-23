using UdonSharp;
using UnityEngine;
using VAU.V320NeoNext.Runtime.Bus;

namespace VAU.V320NeoNext.Runtime.Systems.IndicatingRecording.Fwc
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public sealed class StallWarningGenerator : AbstractAvionicsBusClient
    {
        public AudioSource stallWarningAudioSource;

        private bool _shouldProduceStallWarningLastFrame;

        private void Update()
        {
            var vsw = _ReadFloat(AvionicsBusFloatDataIds.V32NN_Frequent_FAC_1_Stall_Warning_Speed);
            var ias = _ReadFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ADIRS_ADR_1_IndicatedAirspeed);
            var radioAltitude = _ReadFloat(AvionicsBusFloatDataIds.V32NN_Frequent_RA_1_RadioAltitude);
            var shouldProduceStallWarning = 
                ias < vsw && 
                radioAltitude > 100f &&
                !_ReadBool(AvionicsBusBoolDataIds.V32NN_Infrequent_LandingGear_Sync_GearLeverUp);

            if (_shouldProduceStallWarningLastFrame == shouldProduceStallWarning) return;
            _shouldProduceStallWarningLastFrame = shouldProduceStallWarning;

            if (shouldProduceStallWarning)
            {
                stallWarningAudioSource.Play();
            }
            else
            {
                stallWarningAudioSource.Stop();
            }
        }
    }
}