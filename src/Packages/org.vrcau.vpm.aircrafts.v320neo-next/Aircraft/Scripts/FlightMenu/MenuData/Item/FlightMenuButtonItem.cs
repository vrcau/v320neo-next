using UdonSharp;
using UnityEngine;

namespace VAU.V320NeoNext.Runtime.FlightMenu.MenuData.Item
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public sealed class FlightMenuButtonItem : FlightMenuItemBase
    {
        public override FlightMenuTriggerResult Trigger()
        {
            // TODO: Handle Button Click
            return FlightMenuTriggerResult.Noop;
        }
    }
}