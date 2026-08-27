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

        public UdonSharpBehaviour eventTarget;
        public string triggerEventName;

        public bool updateIsActivatedFromEventTarget;
        public string isActivatedVariableName;

        public bool updateTitleFromEventTarget;
        public string titleVariableName;
        public string titleTemplate = "{0}"; 

        public virtual FlightMenuTriggerResult Trigger()
        {
            if (eventTarget && !string.IsNullOrWhiteSpace(triggerEventName))
            {
                eventTarget.SendCustomEvent(triggerEventName);
            }

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
        OpenPopupMenu = 2,
        InternalBackMenu = 3
    }
}