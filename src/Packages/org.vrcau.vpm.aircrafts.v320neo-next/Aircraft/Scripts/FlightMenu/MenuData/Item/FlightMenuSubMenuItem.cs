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
            var baseResult = base.Trigger();
            if (baseResult == FlightMenuTriggerResult.RequestClosePopup)
                return FlightMenuTriggerResult.RequestClosePopup;

            return isPopupMenu ? FlightMenuTriggerResult.OpenPopupMenu : FlightMenuTriggerResult.OpenNewMenu;
        }

        public override FlightMenuGroup GetNewMenu()
        {
            return subMenu;
        }
    }
}