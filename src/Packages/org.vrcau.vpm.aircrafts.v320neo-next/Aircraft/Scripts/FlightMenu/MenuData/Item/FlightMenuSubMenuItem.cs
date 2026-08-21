using UdonSharp;

namespace VAU.V320NeoNext.Runtime.FlightMenu.MenuData.Item
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public sealed class FlightMenuSubMenuItem : FlightMenuItemBase
    {
        public FlightMenuGroup subMenu;

        public bool isPopupMenu;

        public override FlightMenuTriggerResult Trigger()
        {
            return isPopupMenu ? FlightMenuTriggerResult.OpenPopupMenu : FlightMenuTriggerResult.OpenNewMenu;
        }

        public override FlightMenuGroup GetNewMenu()
        {
            return subMenu;
        }
    }
}