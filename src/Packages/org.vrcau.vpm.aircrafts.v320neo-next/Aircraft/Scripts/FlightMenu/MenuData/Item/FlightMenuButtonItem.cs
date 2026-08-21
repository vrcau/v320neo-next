using UdonSharp;
using UnityEngine;

namespace VAU.V320NeoNext.Runtime.FlightMenu.MenuData.Item
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public sealed class FlightMenuButtonItem : FlightMenuItemBase
    {
        public override FlightMenuTriggerResult Trigger()
        {
            Debug.Log("Flight Menu Button Trigger!");
            return FlightMenuTriggerResult.Noop;
        }
    }
}