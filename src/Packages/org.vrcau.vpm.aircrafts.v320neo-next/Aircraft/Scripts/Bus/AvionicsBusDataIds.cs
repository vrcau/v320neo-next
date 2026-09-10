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
        V32NN_Frequent_ADR_AltitudeFeet = 0,
        V32NN_Frequent_ADR_IndicatedAirspeed = 1,
        V32NN_Frequent_ADR_VerticalSpeedFeetPerMinute = 2,
        V32NN_Infrequent_FCU_SelectedAltitudeFeet = 3,
        Count = 4
    }

    public enum AvionicsBusIntDataIds
    {
        V32NN_Frequent_ADR_HeadingDegrees = 0,
        V32NN_Frequent_ADIRS_AlignmentState = 1,
        Count = 2
    }

    public enum AvionicsBusBoolDataIds
    {
        V32NN_Frequent_ADR_IsDataValid = 0,
        V32NN_Infrequent_ADIRS_IsAligned = 1,
        Count = 2
    }

    public enum AvionicsBusStringDataIds
    {
        V32NN_Infrequent_ECAM_ActiveMessage = 0,
        Count = 1
    }

    public enum AvionicsBusVector3DataIds
    {
        V32NN_Frequent_ADR_VelocityNED = 0,
        V32NN_Infrequent_ND_WindVector = 1,
        Count = 2
    }
}
