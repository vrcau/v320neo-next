using System;

namespace VAU.V320NeoNext.Runtime.Systems.IndicatingRecording.EfisControl
{
    [Flags]
    public enum FlightDirectorLandingSystemFlags
    {
        None = 0,
        FlightDirectorOn = 0b_1000_0000,
        LandingSystemOn = 0b_0100_0000
    }

    public enum NavigationDisplayFilter
    {
        None,
        Constraint,
        Waypoint,
        VorDme,
        Ndb,
        Airport
    }

    public enum NavigationDisplayPage
    {
        Ils,
        Vor,
        Nav,
        Arc,
        Plan
    }

    public enum NavigationDisplayRange
    {
        Range10Nm,
        Range20Nm,
        Range40Nm,
        Range80Nm,
        Range160Nm,
        Range320Nm
    }

    public enum NavigationDisplayVorAdfSelector
    {
        Off,
        Adf,
        Vor
    }
}