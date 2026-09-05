using System;
using JetBrains.Annotations;
using SaccFlightAndVehicles;
using UdonSharp;
using UnityEngine;
using VAU.V320NeoNext.Runtime.Systems.Engine.SaccExt;
using VAU.V320NeoNext.Runtime.Systems.LegacyFlightDataProvider;
using VRC.SDKBase;

namespace VAU.V320NeoNext.Runtime.Systems.AutoThrust.SaccExt {
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DFUNC_a320_AutoThrust : UdonSharpBehaviour {
        public SFEXT_a320_AdvancedEngine[] engines = { };

        private DependenciesInjector _injector;
        private AircraftSystemData _aircraftSystemData;
        private SaccAirVehicle _saccAirVehicle;

        private VRCPlayerApi localPlayer;

        public KeyCode increaseSpeedKey = KeyCode.Equals;
        public KeyCode decreaseSpeedKey = KeyCode.Minus;

        [NonSerialized] public bool isAutoThrustArm;

        public float kp = .5f;
        //public float CruiseIntegral = .1f;
        public float kd = .1f;
        public float CruiseDerivative = 0f;
        public float CruiseDerivativeLastFrame = 0f;
        //public float CruiseIntegrator;
        //public float CruiseIntegratorMax = 5;
        //public float CruiseIntegratorMin = -5;

        private float CruiseTemp;
        private float SpeedZeroPoint;
        [NonSerialized] public int SetSpeed = 194;

        [NonSerialized] public bool Cruise;
        private bool func_active;
        private bool Piloting;

        private const float MeterToKt = 1.9438445f;
        private const float KtToMeter = 0.514444f;
        private const int MinSpeedInKt = 100;
        private const int MaxSpeedInKt = 399;

        private bool EngineOn => IsEngineOn();
        private bool InReverse => IsReverse();

        private void Init() {
            _injector = DependenciesInjector.GetInstance(this);
            _aircraftSystemData = _injector.equipmentData;
            _saccAirVehicle = _injector.saccAirVehicle;
            _SetSpeedInKt(200);
        }

        private void Start() {
            Init();
        }

        public void SFEXT_L_EntityStart() {
            Init();
        }

        private bool IsReverse() {
            foreach (var engine in engines) {
                if (engine.reversing) return true;
            }

            return false;
        }

        private bool IsEngineOn() {
            foreach (var engine in engines) {
                if (engine.fuel) return true;
            }

            return false;
        }

        public void SFEXT_O_PilotEnter() {
            gameObject.SetActive(true);

            Piloting = true;
        }

        public void SFEXT_O_PilotExit() {
            gameObject.SetActive(false);

            Piloting = false;
        }

        public void SFEXT_G_Explode() {
            isAutoThrustArm = false;
            SetCruiseOff();
        }

        public void SFEXT_G_TouchDown() => SetCruiseOff();

        private float _keyboardSpeedDelta;

        private void LateUpdate() {
            if (!_aircraftSystemData)
                return; // Temp workaround

            if (_aircraftSystemData.isAircraftGrounded) {
                if ((_aircraftSystemData.throttleLevelerSlot == ThrottleLevelerSlot.TOGA ||
                     _aircraftSystemData.throttleLevelerSlot == ThrottleLevelerSlot.FlexMct)
                    &&
                    (_aircraftSystemData.isEngine1Running || _aircraftSystemData.isEngine2Running)) {
                    isAutoThrustArm = true;

                    if (Cruise)
                        SetCruiseOff();
                }
            }
            else {
                if ((_aircraftSystemData.throttleLevelerSlot != ThrottleLevelerSlot.CLB &&
                     _aircraftSystemData.throttleLevelerSlot != ThrottleLevelerSlot.Manuel) && Cruise) {
                    isAutoThrustArm = true;

                    if (Cruise)
                        SetCruiseOff();
                }
            }

            if (_aircraftSystemData.throttleLevelerSlot == ThrottleLevelerSlot.IDLE) {
                isAutoThrustArm = false;

                if (Cruise)
                    SetCruiseOff();
            }

            if (!EngineOn) {
                isAutoThrustArm = false;

                if (Cruise)
                    SetCruiseOff();
            }

            if ((_aircraftSystemData.throttleLevelerSlot == ThrottleLevelerSlot.CLB ||
                 _aircraftSystemData.throttleLevelerSlot == ThrottleLevelerSlot.Manuel) && isAutoThrustArm &&
                !_aircraftSystemData.isAircraftGrounded) {
                SetCruiseOn();
                isAutoThrustArm = false;
            }

            float DeltaTime = Time.deltaTime;
            var isIncreaseKeyPressed = Input.GetKey(increaseSpeedKey);
            var isDecreaseKeyPressed = Input.GetKey(decreaseSpeedKey);

            if (isDecreaseKeyPressed || isIncreaseKeyPressed)
            {
                float equals = isIncreaseKeyPressed ? DeltaTime * 10 : 0;
                float minus = isDecreaseKeyPressed ? DeltaTime * 10 : 0;
                _keyboardSpeedDelta += equals - minus;

                var deltaRoundToInt = Mathf.RoundToInt(_keyboardSpeedDelta);
                if (deltaRoundToInt != 0)
                {
                    _SetSpeedInKt(SetSpeed + deltaRoundToInt);
                    _keyboardSpeedDelta -= deltaRoundToInt;
                }
            }
            else
            {
                _keyboardSpeedDelta = 0;
            }

            if (func_active) {
                var error = SetSpeed * KtToMeter - _saccAirVehicle.AirSpeed;

                CruiseDerivative = (error - CruiseDerivativeLastFrame) / DeltaTime;
                
                //CruiseIntegrator += error * DeltaTime;
                //CruiseIntegrator = Mathf.Clamp(CruiseIntegrator, CruiseIntegratorMin, CruiseIntegratorMax);

                foreach (var engine in engines) {
                    engine.autoThrustInput =
                        Mathf.Clamp((kp * error) + (kd * CruiseDerivative), 0, 1);
                }
                CruiseDerivativeLastFrame = error;
            }
        }

        public void KeyboardInput() {
            if (!(isAutoThrustArm || Cruise)) {
                isAutoThrustArm = true;
            }
            else {
                isAutoThrustArm = false;
                SetCruiseOff();
            }
        }

        public void SetCruiseOn() {
            if (Cruise) {
                return;
            }

            if (Piloting) {
                func_active = true;
            }

            foreach (var engine in engines) {
                engine.isAutoThrustActive = true;
            }

            Cruise = true;
        }

        public void SetCruiseOff() {
            if (!Cruise) {
                return;
            }

            if (Piloting) {
                func_active = false;
            }

            foreach (var engine in engines) {
                engine.isAutoThrustActive = false;
            }

            Cruise = false;
        }

        [PublicAPI]
        public void _IncreaseSetSpeedBy10Kt() => _SetSpeedInKt(SetSpeed + 10);

        [PublicAPI]
        public void _IncreaseSetSpeedBy1Kt() => _SetSpeedInKt(SetSpeed + 1);

        [PublicAPI]
        public void _DecreaseSetSpeedBy10Kt() => _SetSpeedInKt(SetSpeed - 10);

        [PublicAPI]
        public void _DecreaseSetSpeedBy1Kt() => _SetSpeedInKt(SetSpeed - 1);

        public void _SetSpeedInKt(int speed)
        {
            SetSpeed = Mathf.Clamp(speed, MinSpeedInKt, MaxSpeedInKt);
        }

        public void SFEXT_O_LoseOwnership() {
            gameObject.SetActive(false);
            func_active = false;
            if (Cruise) {
                SetCruiseOff();
            }
        }
    }
}