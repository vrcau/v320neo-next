namespace VAU.V320NeoNext.Runtime.FlightMenu.MenuData.Item
{
    public sealed class FlightMenuBackItem : FlightMenuItemBase
    {
        public override FlightMenuTriggerResult Trigger()
        {
            return FlightMenuTriggerResult.InternalBackMenu;
        }
    }
}