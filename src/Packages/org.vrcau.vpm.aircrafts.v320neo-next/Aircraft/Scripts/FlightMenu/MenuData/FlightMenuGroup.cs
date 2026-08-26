using System;
using UdonSharp;
using VAU.V320NeoNext.Runtime.FlightMenu.MenuData.Item;

namespace VAU.V320NeoNext.Runtime.FlightMenu.MenuData
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public sealed class FlightMenuGroup : UdonSharpBehaviour
    {
        public string groupName;
        public string description;
        public FlightMenuItemBase[] menuItems = Array.Empty<FlightMenuItemBase>();
    }
}