using UdonSharp;
using UnityEngine;
using VAU.V320NeoNext.Runtime.Bus;

namespace VAU.V320NeoNext.Runtime.Systems.FlightControl.Flybywire.Fac
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(AircraftScriptsExecutionOrder.Fac)]
    public sealed class FacComputer : AbstractAvionicsBusClient
    {
        private const float FlapUpMaxAoA = 7f;
        private const float Flap1MaxAoa = 9.5f;
        private const float Flap2MaxAoa = 9.5f;
        private const float Flap3MaxAoa = 9.5f;
        private const float FlapFullMaxAoa = 9.5f;

        private const float Vmo = 320;
        private const float Vle = 280;

        private const float Vfe1 = 230;
        private const float Vfe1F = 215;
        private const float Vfe2 = 200;
        private const float Vfe3 = 185;
        private const float VfeFull = 170;

        private void FixedUpdate()
        {
            var ias = _ReadFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ADIRS_ADR_1_IndicatedAirspeed);
            var aoa = _ReadFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ADIRS_ADR_1_AoA);
            var gLoad = _ReadFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ADIRS_IR_1_Vertical_G_Load);

            var maxAoa = GetMaxAoa();
            var protectionAoa = maxAoa - 1f;
            var vlsAoa = maxAoa - 2f;

            var vs1g = ias * Mathf.Sqrt(aoa / maxAoa);
            var vStall = vs1g;
            var alphaProtectSpeed = ias * Mathf.Sqrt(aoa / protectionAoa);
            var lowestSelectableSpeed = ias * Mathf.Sqrt(aoa / vlsAoa);

            _WriteFloat(AvionicsBusFloatDataIds.V32NN_Frequent_FAC_1_MaxAoa, maxAoa);
            _WriteFloat(AvionicsBusFloatDataIds.V32NN_Frequent_FAC_1_ProtectionAoa, protectionAoa);

            _WriteFloat(AvionicsBusFloatDataIds.V32NN_Frequent_FAC_1_Stall_Warning_Speed, vStall);
            _WriteFloat(AvionicsBusFloatDataIds.V32NN_Frequent_FAC_1_MaxAlphaSpeed, vStall);
            _WriteFloat(AvionicsBusFloatDataIds.V32NN_Frequent_FAC_1_AlphaProtectionSpeed, alphaProtectSpeed);
            _WriteFloat(AvionicsBusFloatDataIds.v32NN_Frequent_FAC_1_LowestSelectableSpeed, lowestSelectableSpeed);

            UpdateVMax();
        }

        private void UpdateVMax()
        {
            var targetFlapIndex = _ReadInt(AvionicsBusIntDataIds.V32NN_Frequent_Flap_LevelerPosition);
            var vMax = Vmo;
            switch (targetFlapIndex)
            {
                case 1:
                    vMax = Vfe1;
                    break;
                case 2:
                    vMax = Vfe1F;
                    break;
                case 3:
                    vMax = Vfe2;
                    break;
                case 4:
                    vMax = Vfe3;
                    break;
                case 5:
                    vMax = VfeFull;
                    break;
            }

            if (!_ReadBool(AvionicsBusBoolDataIds.V32NN_Infrequent_LandingGear_Sync_GearLeverUp)) 
            {
                vMax = Mathf.Min(vMax, Vle);
            }

            _WriteFloat(AvionicsBusFloatDataIds.V32NN_Frequent_FAC_1_MaxSpeed, vMax);
        }

        private float GetMaxAoa()
        {
            var flapIndex = _ReadInt(AvionicsBusIntDataIds.V32NN_Frequent_Flap_SFCC_1_ActualFlapPosition);
            // 负值表示正在向 -index-1 档移动，取目标档位
            if (flapIndex < 0) flapIndex = -flapIndex - 1;
            switch (flapIndex)
            {
                case 1:
                    return Flap1MaxAoa;
                case 2:
                    return Flap2MaxAoa;
                case 3:
                    return Flap3MaxAoa;
                case 4:
                    return FlapFullMaxAoa;
                default:
                    return FlapUpMaxAoA;
            }
        }
    }
}