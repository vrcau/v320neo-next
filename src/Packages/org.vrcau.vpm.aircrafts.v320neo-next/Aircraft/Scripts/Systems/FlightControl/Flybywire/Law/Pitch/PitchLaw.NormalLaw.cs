using UnityEngine;
using VAU.V320NeoNext.Runtime.Bus;

namespace VAU.V320NeoNext.Runtime.Systems.FlightControl.Flybywire.Law.Pitch
{
    public partial class PitchLaw
    {
        [Header("Pitch Law PID")] [Header("C Star -> Pitch Rate")]
        public float cStarToPitchRateKp = 1;

        public float cStarToPitchRateKi = 10;
        public float cStarToPitchRateKd = 0.05f;
        public float cStarToPitchRateKdMinIntegrator = -1;
        public float cStarToPitchRateKdMaxIntegrator = 1;

        [Header("Pitch Attitude Limit")] public float pitchAttitudeLimitKp = 0.5f;
        [Header("High Speed Protection")] public float highSpeedProtectionKp = -0.3f;

        private const float MaxPitchLimit = 25;
        private const float MaxPitchLimitFlapFull = 15;
        private const float MinPitchLimit = -15;

        [Header("C Star Debug")]
        public float currentCStar;
        public float cStarTarget;
        public float cStarError;
        public float cStarToPitchRateIntegrator;

        private float _cStarToPitchRateIntegrator;
        private float _cStarToPitchRateLastError;

        [Header("High Speed Protection Debug")]
        public float highSpeedProtectionError;
        public float highSpeedProtectionLastError;
        public float highSpeedProtectionOutput;
        public float highSpeedProtectionIntegrator;

        private float RunCStarToPitchRate(float cStarTarget)
        {
            var kp = cStarToPitchRateKp;
            var ki = cStarToPitchRateKi;
            var kd = cStarToPitchRateKd;

            var currentCStar = GetCStar();
            _WriteFloat(AvionicsBusFloatDataIds.V32NN_Frequent_FlyByWireDebug_NormalLawTargetCStar, cStarTarget);
            _WriteFloat(AvionicsBusFloatDataIds.V32NN_Frequent_FlyByWireDebug_NormalLawCurrentCStar, currentCStar);
            this.cStarTarget = cStarTarget;
            this.currentCStar = currentCStar;
            this.cStarError = cStarTarget - currentCStar;
            this.cStarToPitchRateIntegrator = _cStarToPitchRateIntegrator;

            var pitchRateOutput = FlightControlLaw.RunPid(
                cStarTarget, currentCStar,
                ref _cStarToPitchRateIntegrator, ref _cStarToPitchRateLastError, kp, ki, kd,
                cStarToPitchRateKdMinIntegrator, cStarToPitchRateKdMaxIntegrator,
                _inAoAProtection
            );
            _WriteFloat(AvionicsBusFloatDataIds.V32NN_Frequent_FlyByWireDebug_NormalLawIntegrator, _cStarToPitchRateIntegrator);
            _WriteFloat(AvionicsBusFloatDataIds.V32NN_Frequent_FlyByWireDebug_NormalLawOutput, pitchRateOutput);
            return pitchRateOutput;
        }

        private float GetCStar()
        {
            var gLoad = _ReadFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ADIRS_IR_1_Vertical_G_Load);
            var pitchRad = _ReadFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ADIRS_IR_1_Pitch) * Mathf.Deg2Rad;
            var rollRad = _ReadFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ADIRS_IR_1_Roll) * Mathf.Deg2Rad;
            var pitchRateRad =
                _ReadFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ADIRS_IR_1_PitchRateInDegPerSecond) * Mathf.Deg2Rad;

            const float crossSpeed = 121.92f; // m/s 400ft/s~
            const float gravity = 9.80665f;

            var nzAero = gLoad - Mathf.Cos(pitchRad) / Mathf.Cos(rollRad);

            return nzAero + (crossSpeed / gravity) * pitchRateRad;
        }

        private float GetFinalCStarTarget(float pilotPitchInput)
        {
            var flapPosition = _ReadInt(AvionicsBusIntDataIds.V32NN_Frequent_Flap_SFCC_1_ActualFlapPosition);
            var pitch = _ReadFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ADIRS_IR_1_Pitch);
            var ias = _ReadFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ADIRS_ADR_1_IndicatedAirspeed);
            var vMax = _ReadFloat(AvionicsBusFloatDataIds.V32NN_Frequent_FAC_1_MaxSpeed);
            var isCleanConfig = flapPosition == 0;
            var isFlapFull = flapPosition == 5;

            var cStarTarget = -(isCleanConfig
                ? pilotPitchInput < 0 ? pilotPitchInput * 1.5f : pilotPitchInput
                : pilotPitchInput);

            float cStarMaxLimit;
            float cStarMinLimit;
            if (isCleanConfig)
            {
                cStarMaxLimit = 1.5f;
                cStarMinLimit = -2f;
            }
            else
            {
                cStarMaxLimit = 1f;
                cStarMinLimit = -1f;
            }

            // High Speed Protection
            if (ias > vMax + 6)
            {
                // Max Speed vMax+16
                var targetSpeed = vMax + pilotPitchInput * 10;
                var speedError = targetSpeed - ias;
                highSpeedProtectionError = speedError;
                highSpeedProtectionOutput = speedError * highSpeedProtectionKp;

                cStarTarget += Mathf.Clamp(highSpeedProtectionOutput, cStarMinLimit, cStarMaxLimit);;
            }

            // Pitch Attitude Limit
            var maxPitchUpAngle = isFlapFull ? MaxPitchLimitFlapFull : MaxPitchLimit;
            if (pitch > maxPitchUpAngle || pitch < MinPitchLimit)
            {
                var pitchLimit = pitch > 0 ? maxPitchUpAngle : MinPitchLimit;
                var pitchLimitError = pitchLimit - pitch;
                cStarTarget += pitchLimitError * pitchAttitudeLimitKp;
            }

            return Mathf.Clamp(cStarTarget, cStarMinLimit, cStarMaxLimit);
        }
    }
}