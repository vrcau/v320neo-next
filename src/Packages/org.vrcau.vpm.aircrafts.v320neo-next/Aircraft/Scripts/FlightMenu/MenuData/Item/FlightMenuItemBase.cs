using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;

namespace VAU.V320NeoNext.Runtime.FlightMenu.MenuData.Item
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public abstract class FlightMenuItemBase : UdonSharpBehaviour
    {
        public string title;
        public Sprite icon;
        public bool isActivated;

        public virtual FlightMenuTriggerResult Trigger()
        {
            return FlightMenuTriggerResult.Noop;
        }

        [CanBeNull]
        public virtual FlightMenuGroup GetNewMenu()
        {
            return null;
        }
    }

    public enum FlightMenuTriggerResult
    {
        Noop = 0,
        OpenNewMenu = 1,
        OpenPopupMenu = 2
    }
}