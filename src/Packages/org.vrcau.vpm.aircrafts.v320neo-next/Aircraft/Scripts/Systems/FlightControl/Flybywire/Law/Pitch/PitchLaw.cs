using UdonSharp;
using UnityEngine;
using VAU.V320NeoNext.Runtime.Bus;

namespace VAU.V320NeoNext.Runtime.Systems.FlightControl.Flybywire.Law.Pitch
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public sealed partial class PitchLaw : AbstractAvionicsBusClient
    {
        [Header("Pitch Rate -> Sacc Output")]
        public float pitchRateToSaccInputKp = -1;
        public float pitchRateToSaccInputKi = -1;
        public float pitchRateToSaccInputKd = -0.02f;
        public float pitchRateToSaccInputMinIntegrator = -1;
        public float pitchRateToSaccInputMaxIntegrator = 1;

        private float _pitchRateToSaccInputIntegrator;
        private float _pitchRateToSaccInputLastError;

        [Header("Pitch Rate Debug")]
        public float currentPitchRate;
        public float pitchRateTarget;
        public float pitchRateError;
        public float pitchRateToSaccInputIntegrator;
        public float pitchRateToSaccInput;

        [Header("Pitch Rate Override Debug")]
        public bool pilotInputAsPitchRate;
        public float pilotInputAsPitchRateScale;
        public bool overridePitchRateCommand;
        public float overridePitchRateCommandValue;

        public float RunPitchLaw(float pilotPitchInput)
        {
            var cStarTarget = GetFinalCStarTarget(pilotPitchInput);
            var targetPitchRate = RunCStarToPitchRate(cStarTarget);

            var highAoaProtectionWeight = GetHighAoaProtectionWeight();
            if (highAoaProtectionWeight > 0)
            {
                targetPitchRate = targetPitchRate * (1 - highAoaProtectionWeight) +
                                  RunHighAoaProtection(pilotPitchInput) * highAoaProtectionWeight;
            }

            if (pilotInputAsPitchRate)
                targetPitchRate = pilotPitchInput * pilotInputAsPitchRateScale;
            if (overridePitchRateCommand)
                targetPitchRate = overridePitchRateCommandValue;
            _WriteFloat(AvionicsBusFloatDataIds.V32NN_Frequent_FlyByWireDebug_PitchRateTarget, targetPitchRate);
            var outputToSacc = RunPitchRateToSaccOutput(targetPitchRate);

            if (!_ReadBool(AvionicsBusBoolDataIds.V32NN_Infrequent_LandingGear_Sync_GearLeverUp))
                return pilotPitchInput;
            return outputToSacc;
        }
        
        private float RunPitchRateToSaccOutput(float pitchRateTarget)
        {
            var kp = pitchRateToSaccInputKp;
            var ki = pitchRateToSaccInputKi;
            var kd = pitchRateToSaccInputKd;

            var currentPitchRate =
                _ReadFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ADIRS_IR_1_PitchRateInDegPerSecond);
            this.currentPitchRate = currentPitchRate;
            this.pitchRateTarget = pitchRateTarget;
            this.pitchRateError = pitchRateTarget - currentPitchRate;
            this.pitchRateToSaccInputIntegrator = _pitchRateToSaccInputIntegrator;

            var pitchRateRawOutput = FlightControlLaw.RunPid(
                pitchRateTarget, currentPitchRate,
                ref _pitchRateToSaccInputIntegrator, ref _pitchRateToSaccInputLastError, kp, ki, kd,
                pitchRateToSaccInputMinIntegrator, pitchRateToSaccInputMaxIntegrator
            );

            this.pitchRateToSaccInput = pitchRateRawOutput;
            return Mathf.Clamp(pitchRateRawOutput, -1f, 1f);
        }
    }
}