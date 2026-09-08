namespace VAU.V320NeoNext.Runtime.FlightMenu.MenuData.Item.Custom.Slider
{
    public class FlightMenuSliderItem : FlightMenuItemBase
    {
        public int sliderLength;

        public string sliderIndexVariableName;
        public string onSliderIndexChangedEventName;

        public string sliderDescription;

        public bool updateSliderDescriptionFromEventTarget;
        public string sliderDescriptionTemplate = "{0}";
        public string sliderDescriptionVariableName;

        public override FlightMenuTriggerResult Trigger()
        {
            base.Trigger();

            return FlightMenuTriggerResult.OpenSliderMenu;
        }
    }
}