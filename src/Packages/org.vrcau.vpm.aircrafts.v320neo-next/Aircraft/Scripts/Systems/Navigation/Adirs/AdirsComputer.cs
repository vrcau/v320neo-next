using SaccFlightAndVehicles;
using UnityEngine;
using VAU.V320NeoNext.Runtime.Bus;

namespace VAU.V320NeoNext.Runtime.Systems.Navigation.Adirs
{
    [DefaultExecutionOrder(AircraftScriptsExecutionOrder.Adr)]
    public sealed class AdirsComputer : AbstractAvionicsBusClient
    {
        public SaccAirVehicle airVehicle;

        private AvionicsBusFloatDataIds _altitudeId;
        private AvionicsBusFloatDataIds _indicatedAirspeedId;
        private AvionicsBusFloatDataIds _verticalSpeedId;
        private AvionicsBusFloatDataIds _machNumberId;
        private AvionicsBusFloatDataIds _aoaId;

        private AvionicsBusFloatDataIds _verticalGLoadId;
        private AvionicsBusFloatDataIds _pitchId;
        private AvionicsBusFloatDataIds _rollId;

        private Transform _saccEntityTransform;

        protected override void _OnAvionicsBusStart()
        {
            _altitudeId = AvionicsBusFloatDataIds.V32NN_Frequent_ADIRS_ADR_1_AltitudeFeet;
            _indicatedAirspeedId = AvionicsBusFloatDataIds.V32NN_Frequent_ADIRS_ADR_1_IndicatedAirspeed;
            _verticalSpeedId = AvionicsBusFloatDataIds.V32NN_Frequent_ADIRS_ADR_1_VerticalSpeedFeetPerMinute;
            _machNumberId = AvionicsBusFloatDataIds.V32NN_Frequent_ADIRS_ADR_1_MachNumber;
            _aoaId = AvionicsBusFloatDataIds.V32NN_Frequent_ADIRS_ADR_1_AoA;

            _verticalGLoadId = AvionicsBusFloatDataIds.V32NN_Frequent_ADIRS_IR_1_Vertical_G_Load;
            _pitchId = AvionicsBusFloatDataIds.V32NN_Frequent_ADIRS_IR_1_Pitch;
            _rollId = AvionicsBusFloatDataIds.V32NN_Frequent_ADIRS_IR_1_Roll;

            _saccEntityTransform = airVehicle.EntityControl.transform;
        }

        private void FixedUpdate()
        {
            var entityPositionY = _saccEntityTransform.position.y;
            var airVehicleVelocity = airVehicle.CurrentVel;

            var altitudeInMeter = entityPositionY - airVehicle.SeaLevel;
            var airSpeedInMeter = _ReadFloat(AvionicsBusFloatDataIds.Sim_Frequent_IndicatedAirSpeedInMeter);

            _WriteFloat(_altitudeId, altitudeInMeter * UnitConverterUtils.FeetPerMeter);
            _WriteFloat(_indicatedAirspeedId, airSpeedInMeter * UnitConverterUtils.MetersPerSecondToKnots);
            _WriteFloat(_verticalSpeedId, airVehicleVelocity.y * 60f * UnitConverterUtils.FeetPerMeter);
            _WriteFloat(_aoaId, airVehicle.AngleOfAttackPitch);

            if (altitudeInMeter < 11000)
            {
                var mach = airSpeedInMeter / (20.05f * Mathf.Sqrt(288f - altitudeInMeter * 0.65f / 100f));
                _WriteFloat(_machNumberId, mach);
            }
            else
            {
                _WriteFloat(_machNumberId, airSpeedInMeter * 295f);
            }

            _WriteFloat(_verticalGLoadId, airVehicle.VertGs);

            var oldPitch = _ReadFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ADIRS_IR_1_Pitch);
            var pitch = -_saccEntityTransform.eulerAngles.x;
            pitch = pitch < -180 ? 360 + pitch : pitch;
            _WriteFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ADIRS_IR_1_Pitch, pitch);

            var pitchRateInDegPerSecond = (pitch - oldPitch) / Time.fixedDeltaTime;
            _WriteFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ADIRS_IR_1_PitchRateInDegPerSecond,
                pitchRateInDegPerSecond);

            var roll = -_saccEntityTransform.eulerAngles.z;
            _WriteFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ADIRS_IR_1_Roll, roll < -180 ? 360 + roll : roll);
        }
    }
}