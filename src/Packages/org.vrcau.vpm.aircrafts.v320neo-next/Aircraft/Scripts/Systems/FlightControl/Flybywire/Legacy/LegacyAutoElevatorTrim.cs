using JetBrains.Annotations;
using UnityEngine;
using VAU.V320NeoNext.Runtime.Bus;

namespace VAU.V320NeoNext.Runtime.Systems.FlightControl.Flybywire.Legacy
{
    public sealed class LegacyAutoElevatorTrim : AbstractAvionicsBusClient
    {
        public KeyCode desktopEnableAuto = KeyCode.F7;

        #region Auto trim controller parameters

        public float criticalAoa = 8f; //临界攻角，sav.pitchaoa低于该数值时，触发afloorProtect;

        [Header("Controller value for curise")]
        public float kp1 = 0.04f; //载荷系数控制率 0.6 0.015 俯仰角控制率 0.02 0.001

        public float ki1 = 0.0015f;
        public float kd1 = 0.0005f;

        [Header("Controller value for low speed (below 220kts)")]
        //低空小表速，使用另一套更稳定的参数
        public float kp2 = 0.25f;

        public float ki2 = 0.4f;
        public float kd2 = 0.0003f;

        #endregion

        [Header("Internal values for debug")] public float targetLoadFactor = 1;
        public float targetAoa;

        public float trimError;
        private float _trimErrorLastFrame;
        public float trimErrorIntergrate;
        public float trimErrorDerivative;

        [Tooltip("0-直接法则 1-飞行模式 2-地面模式 3-拉平模式")]
        public int trimMode = 1; //0-直接法则 1-飞行模式 2-地面模式 3-拉平模式

        public bool afloorProtect;
        public bool lowSpeedMode;

        private float _initialTrim;

        private bool AutoTrimActive
        {
            get => _ReadBool(AvionicsBusBoolDataIds.V32NN_Infrequent_ElevatorTrim_Sync_AutoTrimActive);
            set => _WriteAndNotifyBool(AvionicsBusBoolDataIds.V32NN_Infrequent_ElevatorTrim_Sync_AutoTrimActive, value);
        }
        
        private float Trim
        {
            get => _ReadFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ElevatorTrim_Sync_TrimPosition);
            set => _WriteFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ElevatorTrim_Sync_TrimPosition, value);
        }

        private void ResetStatus()
        {
            trimMode = 0;
            trimError = 0;
            trimErrorIntergrate = 0;
            trimErrorDerivative = 0;
            _trimErrorLastFrame = 0;
            targetAoa = 0;
        }

        protected override void _OnAvionicsBusRespawn_RunInOwner() => ResetStatus();

        protected override void _OnAvionicsBusPostStart()
        {
            _initialTrim = _ReadFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ElevatorTrim_Sync_TrimPosition);
            _SubscribeBool(AvionicsBusBoolDataIds.Sim_Infrequent_IsPilot, nameof(_OnIsPilotChanged));
        }

        public void _OnIsPilotChanged()
        {
            enabled = _ReadBool(AvionicsBusBoolDataIds.Sim_Infrequent_IsPilot);
        }

        private void FixedUpdate()
        {
            //计算配平值
            var deltaTime = Time.fixedDeltaTime;

            var pitchInputs =
                _ReadVector3(AvionicsBusVector3DataIds.V32NN_Frequent_FlightControl_FinalPilotInputToSacc).x;
            var isAircraftGrounded =
                _ReadBool(AvionicsBusBoolDataIds.Sim_Frequent_Grounded);
            var radioAltitude =
                _ReadFloat(AvionicsBusFloatDataIds.V32NN_Frequent_RA_1_RadioAltitude);
            var isAutoPilotEngaged =
                _ReadBool(AvionicsBusBoolDataIds.V32NN_Infrequent_AutoPilot_Engaged);
            var verticalSpeed =
                _ReadFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ADIRS_ADR_1_VerticalSpeedFeetPerMinute);
            var aoa =
                _ReadFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ADIRS_ADR_1_AoA);
            var gLoad =
                _ReadFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ADIRS_IR_1_Vertical_G_Load);
            var ias =
                _ReadFloat(AvionicsBusFloatDataIds.V32NN_Frequent_ADIRS_ADR_1_IndicatedAirspeed);

            //飞行模式
            if (AutoTrimActive &&
                !isAircraftGrounded &&
                radioAltitude >= 50)
            {
                if (trimMode != 1)
                {
                    trimMode = 1;
                    trimError = trimErrorIntergrate = trimErrorDerivative = 0f;
                    Debug.Log("[FBW]Flight Mode");
                }

                if (_ReadBool(AvionicsBusBoolDataIds.V32NN_Infrequent_AutoPilot_Engaged) ||
                    _ReadBool(AvionicsBusBoolDataIds.V32NN_Infrequent_LandingGear_Sync_GearLeverUp))
                {
                    Trim = _initialTrim;
                }
                else
                {
                    if (aoa < criticalAoa)
                    {
                        afloorProtect = false;
                        targetLoadFactor = StickInputToLoadFactor(pitchInputs, deltaTime);
                        trimError = (targetLoadFactor - gLoad);
                        trimErrorIntergrate = Mathf.Clamp(trimError * deltaTime + trimErrorIntergrate, -1, 1); //处理积分饱和
                        trimErrorDerivative = (trimError - _trimErrorLastFrame) / deltaTime;
                    }
                    else
                    {
                        afloorProtect = true;
                        targetAoa = StickInputToAoa(pitchInputs, deltaTime);
                        trimError = targetAoa - aoa; //todo:afloor控制律
                        trimErrorIntergrate = 0;
                        trimErrorDerivative = 0;
                    }

                    var kp = kp1;
                    var ki = ki1;
                    var kd = kd1;

                    lowSpeedMode = ias < 213.8229f;
                    if (lowSpeedMode)
                    {
                        kp = kp2;
                        ki = ki2;
                        kd = kd2;
                    }

                    //trim = Mathf.MoveTowards(trim, Mathf.Clamp(kp * TrimError + ki * TrimErrorIntergrate, -1, 1), 0.1f);
                    Trim = Mathf.Clamp(kp * trimError + ki * trimErrorIntergrate + kd * trimErrorDerivative, -1, 1);
                    _trimErrorLastFrame = trimError;
                }
            }

            //地面模式
            else if (AutoTrimActive && isAircraftGrounded)
            {
                if (trimMode != 2)
                {
                    trimMode = 2;
                    trimError = trimErrorIntergrate = trimErrorDerivative = 0f;
                    Debug.Log("[FBW]Ground Mode");
                }

                Trim = _initialTrim;
                targetLoadFactor = StickInputToLoadFactor(pitchInputs, deltaTime);
            }

            //拉平模式
            else if (AutoTrimActive &&
                     radioAltitude < 50 &&
                     !isAircraftGrounded &&
                     verticalSpeed < -0.6 &&
                     !isAutoPilotEngaged)
            {
                var targetTrim = _initialTrim;
                if (trimMode != 3)
                {
                    trimMode = 3;
                    Debug.Log("[FBW]Touchdown Mode");
                    targetTrim = Trim - 0.05f;
                }

                //俯仰角控制率
                /*
                targetPitch = -2f;
                TrimError = (targetPitch - BasicFlightData.pitch);
                TrimErrorIntergrate += TrimError;
                TrimErrorDerivative = (TrimError - TrimErrorLastFrame) / DeltaTime;
                trim = Mathf.Clamp(kp * TrimError + ki * TrimErrorIntergrate + kd * TrimErrorDerivative, -1, 1);
                */
                trimError = trimErrorIntergrate = trimErrorDerivative = 0f;
                Trim = Mathf.MoveTowards(Trim, targetTrim, deltaTime * 0.025f);
            }
        }

        private float StickInputToLoadFactor(float pitchInputs, float deltaTime)
        {
            var maxLoad = 2f;
            var minLoad = 0f;

            var maxLoadRate = 1f * deltaTime; //每秒最大变化1g
            //SAV脚本中已经做了输入的平方化，所以这里直接线性转换为载荷目标
            if (pitchInputs > 0.01)
            {
                //推杆 
                targetLoadFactor = Mathf.MoveTowards(targetLoadFactor,
                    (minLoad - 1f) * Mathf.Pow(pitchInputs, 2) + 1,
                    maxLoadRate);
            }
            else if ((pitchInputs < -0.01))
            {
                //拉杆 
                targetLoadFactor = Mathf.MoveTowards(targetLoadFactor,
                    (maxLoad - 1f) * Mathf.Pow(pitchInputs, 2) + 1,
                    maxLoadRate);
            }
            else
            {
                targetLoadFactor = Mathf.MoveTowards(targetLoadFactor, 1, maxLoadRate);
            }

            if (trimMode != 1)
                //飞行模式以外给一个比较小的载荷目标约束，避免离地时模式切换导致配平位置突变
                return Mathf.Clamp(targetLoadFactor, 1f - 0.5f, 1f + 0.5f);
            else
                return targetLoadFactor;
        }

        private float StickInputToAoa(float pitchInputs, float deltaTime)
        {
            var maxLoadRate = 10f * deltaTime; //每秒最大变化1单位
            //SAV脚本中已经做了输入的平方化，所以这里直接线性转换为载荷目标
            if (pitchInputs > 0.01)
            {
                //推杆 
                targetAoa = Mathf.MoveTowards(targetAoa,
                    -(criticalAoa) * Mathf.Pow(pitchInputs, 2),
                    maxLoadRate);
            }
            else if ((pitchInputs < -0.01))
            {
                //拉杆
                targetAoa = Mathf.MoveTowards(targetAoa,
                    (criticalAoa) * Mathf.Pow(pitchInputs, 2),
                    maxLoadRate);
            }
            else
            {
                targetAoa = Mathf.MoveTowards(targetAoa, 1, maxLoadRate);
            }

            //if (trimMode != 1) return Mathf.Clamp(targetAoa, -3, 3);
            //else
            return targetAoa;
        }

        #region User Input

        [PublicAPI]
        public void _ToggleAutoTrim()
        {
            AutoTrimActive = !AutoTrimActive;

            Debug.Log(AutoTrimActive ? "[FBW]AUTO TRIM" : "[FBW]MAN TRIM");

            trimError = 0;
            trimErrorIntergrate = 0;
            trimErrorDerivative = 0;
        }

        private void LateUpdate()
        {
            if (Input.GetKeyDown(desktopEnableAuto)) _ToggleAutoTrim();
        }

        #endregion
    }
}