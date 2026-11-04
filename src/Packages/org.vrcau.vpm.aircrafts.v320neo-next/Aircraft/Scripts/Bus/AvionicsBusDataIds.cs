namespace VAU.V320NeoNext.Runtime.Bus
{
    /// <summary>
    /// 航电总线的数据 id 定义。
    /// <para>
    /// 参数命名约定：<c>飞机名称_更新频繁程度_系统_自定义变量名</c>，例如
    /// <c>V32NN_Frequent_ADR_AltitudeFeet</c>：
    /// <list type="bullet">
    /// <item><description>飞机名称：V32NN</description></item>
    /// <item><description>更新频繁程度：Frequent_ / Infrequent_</description></item>
    /// <item><description>系统：例如 ADR</description></item>
    /// <item><description>自定义变量名：例如 AltitudeFeet</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// 每个 enum 的成员值即 AvionicsBus 中对应数组的下标，末尾的 <c>Count</c> 是分配数组用的哨兵，
    /// 必须始终放在最后且不要给它赋值以外的用途。
    /// </para>
    /// </summary>
    public enum AvionicsBusFloatDataIds
    {
        Sim_Frequent_IndicatedAirSpeedInMeter,

        // ATA22_40. Auto Flight / Flight Augmentation / FAC
        V32NN_Frequent_FAC_1_Stall_Warning_Speed,
        V32NN_Frequent_FAC_1_MaxAoa,
        V32NN_Frequent_FAC_1_ProtectionAoa,
        V32NN_Frequent_FAC_1_AlphaProtectionSpeed,
        V32NN_Frequent_FAC_1_MaxAlphaSpeed,
        v32NN_Frequent_FAC_1_LowestSelectableSpeed,
        V32NN_Frequent_FAC_1_MaxSpeed,

        // ATA32. Landing Gear / Brake
        V32NN_Frequent_Brake_Sync_PedalInput,
        V32NN_Frequent_Brake_Sync_AutoBrakeInput,
        V32NN_Frequent_Brake_FinalBrakeInput,

        // ATA34. Navigation / ADIRS / ADR
        V32NN_Frequent_ADIRS_ADR_1_AltitudeFeet,
        V32NN_Frequent_ADIRS_ADR_1_IndicatedAirspeed,
        V32NN_Frequent_ADIRS_ADR_1_VerticalSpeedFeetPerMinute,
        V32NN_Frequent_ADIRS_ADR_1_MachNumber,
        V32NN_Frequent_ADIRS_ADR_1_AoA,

        // ATA34. Navigation / ADIRS / IR
        V32NN_Frequent_ADIRS_IR_1_Vertical_G_Load,
        V32NN_Frequent_ADIRS_IR_1_Pitch,
        V32NN_Frequent_ADIRS_IR_1_PitchRateInDegPerSecond,
        V32NN_Frequent_ADIRS_IR_1_Roll,

        // ATA34. Navigation / Radio Altimeter
        V32NN_Frequent_RA_1_RadioAltitude,

        // ATA70. Engine
        V32NN_Frequent_Engine_Both_ThrustLever,

        // ATA27. Flight Control / FlyByWire Debug
        V32NN_Frequent_FlyByWireDebug_HighAoAProtectionWeight,
        V32NN_Frequent_FlyByWireDebug_HighAoAProtectionOutput,
        V32NN_Frequent_FlyByWireDebug_HighAoAProtectionTargetAoA,
        V32NN_Frequent_FlyByWireDebug_HighAoAProtectionIntegrator,
        V32NN_Frequent_FlyByWireDebug_NormalLawOutput,
        V32NN_Frequent_FlyByWireDebug_NormalLawTargetCStar,
        V32NN_Frequent_FlyByWireDebug_NormalLawCurrentCStar,
        V32NN_Frequent_FlyByWireDebug_NormalLawIntegrator,
        V32NN_Frequent_FlyByWireDebug_PitchRateTarget,
        // ATA27. Flight Control / Flaps & Elevator Trim
        // 襟翼移动期间每帧都会变：只写不 Notify，由 FlapFlightMenuController 轮询后自行算状态文本
        V32NN_Frequent_Flap_SlatAngle,
        V32NN_Frequent_Flap_FlapAngle,

        // 配平位置（-1~1）：总线上的这个变量就是配平状态本身。
        // 自动配平控制律、桌面按键与菜单 bridge（ElevatorTrimFlightMenuController）都直接写它，
        // 非 owner 读到的是网络同步下来的值。每帧都可能变化，所以只写不 Notify，消费方轮询；
        // 网络镜像由 ElevatorTrimAvionicsBusContinuousSync 负责。
        V32NN_Frequent_ElevatorTrim_Sync_TrimPosition,

        // Avionics Bus
        Count
    }

    public enum AvionicsBusIntDataIds
    {
        // ATA20_10. Auto Flight / Pilot Interface / FCU
        V32NN_Infrequent_FCU_Sync_SelectedAirspeedInKt,

        // ATA27. Flight Control / Flaps
        // 襟翼移动期间每帧都会变，只写不 Notify（由 FlapFlightMenuController 轮询）
        V32NN_Frequent_Flap_LevelerPosition,
        V32NN_Frequent_Flap_SFCC_1_ActualFlapPosition,

        V32NN_Frequent_ADR_HeadingDegrees,
        V32NN_Frequent_ADIRS_AlignmentState,

        // --- FlightMenu (InputSystem/FlightMenuController) ---
        // 说明：总线只承载「系统需要的值」与「对任何总线客户端都有意义的系统状态」。
        // 用户输入（含长按 hold）由 <X>FlightMenuController 解读成这些值后再写入，
        // 不使用 pulse / 事件型变量，飞机系统也不感知原始输入。
        // 每帧变化的量标 Frequent 且只写不 Notify（消费方轮询）；很少变化的量才用事件通知。

        // ATA27. Flight Control / Flaps
        // 请求档位：无 _Sync_ —— leverIndex 由 DFUNC_a320_FlapController 自行维护网络同步
        V32NN_Infrequent_Flap_RequestedLeverIndex,
        V32NN_Frequent_Flap_LeverIndex,

        // ATA32. Landing Gear / Auto Brake
        // 总线即状态本身（0=Off 1=Low 2=Med 3=Max），由 AutoBrakeAvionicsBusSync 做网络同步
        V32NN_Infrequent_AutoBrake_Sync_Mode,

        // ATA23. Communication / VHF
        V32NN_Infrequent_VHF_ChannelKhz,

        Count
    }

    public enum AvionicsBusByteDataIds
    {
        // ATA31. Indicating Recording / EFIS Control
        // Left EFIS
        V32NN_Infrequent_EFIS_Left_Sync_NavigationDisplayFilter,
        V32NN_Infrequent_EFIS_Left_Sync_NavigationDisplayPage,
        V32NN_Infrequent_EFIS_Left_Sync_NavigationDisplayRange,
        V32NN_Infrequent_EFIS_Left_Sync_NavigationDisplayVorAdfSelector,

        // Right EFIS
        V32NN_Infrequent_EFIS_Right_Sync_NavigationDisplayFilter,
        V32NN_Infrequent_EFIS_Right_Sync_NavigationDisplayPage,
        V32NN_Infrequent_EFIS_Right_Sync_NavigationDisplayRange,
        V32NN_Infrequent_EFIS_Right_Sync_NavigationDisplayVorAdfSelector,

        // Avionics Bus
        Count
    }

    public enum AvionicsBusBoolDataIds
    {
        Sim_Infrequent_HaveAircraftOwnership,
        Sim_Infrequent_HasPilot,
        Sim_Infrequent_IsPilot,
        Sim_Frequent_Grounded,

        // ATA31. Indicating Recording / EFIS Control
        // Left EFIS
        V32NN_Infrequent_EFIS_Left_Sync_FlightDirectorOn,
        V32NN_Infrequent_EFIS_Left_Sync_LandingSystemOn,

        // Right EFIS
        V32NN_Infrequent_EFIS_Right_Sync_FlightDirectorOn,
        V32NN_Infrequent_EFIS_Right_Sync_LandingSystemOn,

        // ATA32. Landing Gear
        V32NN_Infrequent_LandingGear_Sync_GearLeverUp,

        // ATA32. Landing Gear / Brake
        V32NN_Infrequent_Brake_Sync_ParkBrakeSet,

        // ATA70. Engine
        V32NN_Infrequent_Engine_Engine_1_Sync_ReverserLeverOn,
        V32NN_Infrequent_Engine_Engine_2_Sync_ReverserLeverOn,

        // ATA49. APU / Auto Start (FlightMenu)
        V32NN_Infrequent_AutoStart_Sync_Start,
        V32NN_Infrequent_AutoStart_IndicatorActivated,

        // ATA27. Flight Control / Elevator Trim (FlightMenu)
        // Auto Trim 状态本身（总线即状态），由 ElevatorTrimAvionicsBusSync 做网络同步
        V32NN_Infrequent_ElevatorTrim_Sync_AutoTrimActive,

        // ATA22. Auto Flight / Auto Thrust (FlightMenu)
        // Engage 只是「按了一次 Toggle A/THR」的目标状态：接通状态只在 owner 本地有意义，
        // 因此 **不做网络同步**（无 _Sync_、无 Sync 类），谁按谁本地应用、只有 owner 生效。
        V32NN_Infrequent_AutoPilot_Engaged,
        V32NN_Infrequent_AutoThrust_EngageRequest,
        V32NN_Infrequent_AutoThrust_Cruise,

        // Armed：本机「已预位或已在巡航」状态，bridge 用它算反值
        V32NN_Infrequent_AutoThrust_Armed,

        // ATA23. Communication / VHF (FlightMenu)
        // 无 _Sync_ —— SFEXT_URC_VHF 自己负责网络同步，总线不重复同步。
        // 只有按下菜单的客户端会写自己的本地总线，也就只有它的 Adapter 会应用。
        V32NN_Infrequent_VHF_RequestedRxOn,
        V32NN_Infrequent_VHF_RequestedTxOn,
        V32NN_Infrequent_VHF_RxPower,
        V32NN_Infrequent_VHF_TxPower,

        // ATA32. Landing Gear / Auto Brake (FlightMenu)
        V32NN_Infrequent_AutoBrake_Active,
        V32NN_Infrequent_AutoBrake_ReachDecelTarget,

        // Avionics Bus
        Count
    }

    public enum AvionicsBusStringDataIds
    {
        V32NN_Infrequent_ECAM_ActiveMessage = 0,
        Count = 1
    }

    public enum AvionicsBusVector3DataIds
    {
        // ATA27. Flight Control
        V32NN_Frequent_FlightControl_LocalPilotInput,
        V32NN_Frequent_FlightControl_AutoPilotInput,
        V32NN_Frequent_FlightControl_FinalPilotInputToSacc,

        V32NN_Infrequent_ND_WindVector,

        Count
    }
}