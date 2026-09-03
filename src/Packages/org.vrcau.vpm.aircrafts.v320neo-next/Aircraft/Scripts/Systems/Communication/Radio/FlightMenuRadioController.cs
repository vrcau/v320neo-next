using System;
using System.Text;
using JetBrains.Annotations;
using UdonRadioCommunicationRedux.SaccFlight;
using UdonSharp;
using UnityEngine;
using VAU.V320NeoNext.Runtime.Extensions;
using VAU.V320NeoNext.Runtime.FlightMenu.MenuData;
using VAU.V320NeoNext.Runtime.FlightMenu.MenuData.Item;

namespace VAU.V320NeoNext.Runtime.Systems.Communication.Radio
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public sealed class FlightMenuRadioController : UdonSharpBehaviour
    {
        public SFEXT_URC_VHF transceiver;

        [Header("All in kHz")] public int allowFrequencyStep = 5;
        public int minFrequency = 118000;
        public int maxFrequency = 136975;

        [Header("Number Menu Item")] public FlightMenuGroup numberInputMenuGroup;
        public FlightMenuSubMenuItem number0ButtonItem;
        public FlightMenuSubMenuItem number1ButtonItem;
        public FlightMenuSubMenuItem number2ButtonItem;
        public FlightMenuSubMenuItem number3ButtonItem;
        public FlightMenuSubMenuItem number4ButtonItem;
        public FlightMenuSubMenuItem number5ButtonItem;
        public FlightMenuSubMenuItem number6ButtonItem;
        public FlightMenuSubMenuItem number7ButtonItem;
        public FlightMenuSubMenuItem number8ButtonItem;
        public FlightMenuSubMenuItem number9ButtonItem;
        public FlightMenuSubMenuItem backspaceButtonItem;

        // Only for flight menu item update access
        [Header("Debug Only")] [PublicAPI] public string activeFrequencyText;
        public string vhfStatusOverviewText;
        public bool isVhfRxActivated;

        private string _frequencyDraft = "";
        private int[] _lastValidNextDigits = new int[0];

        private void Start()
        {
            transceiver.OnUpdateChannel();
            _ResetDraft();
            ResetAllMenuItem();

            UpdateStatusText();
            transceiver.CallbackBehaviours = transceiver.CallbackBehaviours.Add(this);
        }

        private void ResetAllMenuItem()
        {
            ResetMenuItem(number0ButtonItem, nameof(_NumberInputZero));
            ResetMenuItem(number1ButtonItem, nameof(_NumberInputOne));
            ResetMenuItem(number2ButtonItem, nameof(_NumberInputTwo));
            ResetMenuItem(number3ButtonItem, nameof(_NumberInputThree));
            ResetMenuItem(number4ButtonItem, nameof(_NumberInputFour));
            ResetMenuItem(number5ButtonItem, nameof(_NumberInputFive));
            ResetMenuItem(number6ButtonItem, nameof(_NumberInputSix));
            ResetMenuItem(number7ButtonItem, nameof(_NumberInputSeven));
            ResetMenuItem(number8ButtonItem, nameof(_NumberInputEight));
            ResetMenuItem(number9ButtonItem, nameof(_NumberInputNine));
            ResetMenuItem(backspaceButtonItem, nameof(_Backspace));
        }

        private void SetAllMenuItemClosePopup(bool closePopup)
        {
            number0ButtonItem.requestClosePopupWhenTrigger = closePopup;
            number1ButtonItem.requestClosePopupWhenTrigger = closePopup;
            number2ButtonItem.requestClosePopupWhenTrigger = closePopup;
            number3ButtonItem.requestClosePopupWhenTrigger = closePopup;
            number4ButtonItem.requestClosePopupWhenTrigger = closePopup;
            number5ButtonItem.requestClosePopupWhenTrigger = closePopup;
            number6ButtonItem.requestClosePopupWhenTrigger = closePopup;
            number7ButtonItem.requestClosePopupWhenTrigger = closePopup;
            number8ButtonItem.requestClosePopupWhenTrigger = closePopup;
            number9ButtonItem.requestClosePopupWhenTrigger = closePopup;
        }

        private void ResetMenuItem(FlightMenuSubMenuItem menuItem, string triggerEventName)
        {
            menuItem.eventTarget = this;
            menuItem.triggerEventName = triggerEventName;
            menuItem.subMenu = numberInputMenuGroup;
            menuItem.requestClosePopupWhenTrigger = false;
        }

        #region Handle Number Item Input

        [PublicAPI]
        public void _ResetDraft()
        {
            _frequencyDraft = "";
            numberInputMenuGroup.groupName = "___.__";
            _lastValidNextDigits = GetValidNextDigits(_frequencyDraft, minFrequency, maxFrequency, allowFrequencyStep);
            ResetAllMenuItem();
            UpdateMenuItemsIsEnabled();
        }

        [PublicAPI]
        public void _NumberInputZero() => InputDigit(0);

        [PublicAPI]
        public void _NumberInputOne() => InputDigit(1);

        [PublicAPI]
        public void _NumberInputTwo() => InputDigit(2);

        [PublicAPI]
        public void _NumberInputThree() => InputDigit(3);

        [PublicAPI]
        public void _NumberInputFour() => InputDigit(4);

        [PublicAPI]
        public void _NumberInputFive() => InputDigit(5);

        [PublicAPI]
        public void _NumberInputSix() => InputDigit(6);

        [PublicAPI]
        public void _NumberInputSeven() => InputDigit(7);

        [PublicAPI]
        public void _NumberInputEight() => InputDigit(8);

        [PublicAPI]
        public void _NumberInputNine() => InputDigit(9);

        [PublicAPI]
        public void _Backspace()
        {
            if (_frequencyDraft.Length <= 0) return;

            _frequencyDraft = _frequencyDraft.Substring(0, _frequencyDraft.Length - 1);
            UpdateDraftFrequencyText();

            _lastValidNextDigits = GetValidNextDigits(_frequencyDraft, minFrequency, maxFrequency, allowFrequencyStep);
            UpdateMenuItemsIsEnabled();
        }

        private void InputDigit(int digit)
        {
            if (!ContainsDigit(_lastValidNextDigits, digit))
                return;
            _frequencyDraft += digit;
            UpdateDraftFrequencyText();

            _lastValidNextDigits = GetValidNextDigits(_frequencyDraft, minFrequency, maxFrequency, allowFrequencyStep);

            if (_lastValidNextDigits.Length == 0)
            {
                var frequency = int.Parse(_frequencyDraft);
                transceiver.SetChannel(frequency);
                _ResetDraft();

                // It will be reset by next time open popup (open popup will trigger _ResetDraft first)
                SetAllMenuItemClosePopup(true);
                return;
            }

            UpdateMenuItemsIsEnabled();
        }

        private void UpdateDraftFrequencyText()
        {
            var maxFrequencyDigits = maxFrequency.ToString().Length;
            var remainingDigits = maxFrequencyDigits - _frequencyDraft.Length;

            if (remainingDigits == maxFrequencyDigits)
            {
                numberInputMenuGroup.groupName = "___.__";
                return;
            }

            var frequencyStrBuilder = new StringBuilder();
            frequencyStrBuilder.Append(int.Parse(_frequencyDraft));
            for (var index = 0; index < remainingDigits; index++)
            {
                frequencyStrBuilder.Append("_");
            }

            frequencyStrBuilder.Insert(frequencyStrBuilder.Length - 3, ".");
            var frequencyStr = frequencyStrBuilder.ToString();

            numberInputMenuGroup.groupName = frequencyStr;
        }

        private void UpdateMenuItemsIsEnabled()
        {
            number0ButtonItem.isDisabled = true;
            number1ButtonItem.isDisabled = true;
            number2ButtonItem.isDisabled = true;
            number3ButtonItem.isDisabled = true;
            number4ButtonItem.isDisabled = true;
            number5ButtonItem.isDisabled = true;
            number6ButtonItem.isDisabled = true;
            number7ButtonItem.isDisabled = true;
            number8ButtonItem.isDisabled = true;
            number9ButtonItem.isDisabled = true;

            for (var index = 0; index < _lastValidNextDigits.Length; index++)
            {
                var digit = _lastValidNextDigits[index];
                switch (digit)
                {
                    case 0:
                        number0ButtonItem.isDisabled = false;
                        break;
                    case 1:
                        number1ButtonItem.isDisabled = false;
                        break;
                    case 2:
                        number2ButtonItem.isDisabled = false;
                        break;
                    case 3:
                        number3ButtonItem.isDisabled = false;
                        break;
                    case 4:
                        number4ButtonItem.isDisabled = false;
                        break;
                    case 5:
                        number5ButtonItem.isDisabled = false;
                        break;
                    case 6:
                        number6ButtonItem.isDisabled = false;
                        break;
                    case 7:
                        number7ButtonItem.isDisabled = false;
                        break;
                    case 8:
                        number8ButtonItem.isDisabled = false;
                        break;
                    case 9:
                        number9ButtonItem.isDisabled = false;
                        break;
                }
            }
        }

        private static bool ContainsDigit(int[] digits, int digit)
        {
            foreach (var d in digits)
            {
                if (d == digit)
                    return true;
            }

            return false;
        }

        private static int[] GetValidNextDigits(string prefix, int minInt, int maxInt, int stepInt)
        {
            var totalDigits = maxInt.ToString().Length;
            var remaining = totalDigits - prefix.Length;

            if (remaining < 0)
                return new int[0];

            var validDigits = new int[10];

            var validDigitsCount = 0;
            for (var d = 0; d <= 9; d++)
            {
                var candidate = prefix + d;
                var candRem = totalDigits - candidate.Length;
                if (candRem < 0)
                    continue;

                var lowStr = candidate + new string('0', candRem);
                var highStr = candidate + new string('9', candRem);
                var lowCand = int.Parse(lowStr);
                var highCand = int.Parse(highStr);

                var overlapLow = Math.Max(minInt, lowCand);
                var overlapHigh = Math.Min(maxInt, highCand);

                if (overlapLow > overlapHigh)
                    continue;

                long firstMultiple = ((overlapLow + stepInt - 1) / stepInt) * stepInt;
                if (firstMultiple <= overlapHigh)
                {
                    validDigits[validDigitsCount] = d;
                    validDigitsCount++;
                }
            }

            var newValidDigits = new int[validDigitsCount];
            for (var index = 0; index < validDigitsCount; index++)
            {
                newValidDigits[index] = validDigits[index];
            }

            return newValidDigits;
        }

        #endregion

        // Callback for SFEXT_URC_VHF
        [PublicAPI]
        public void OnUpdateChannel() => UpdateStatusText();

        [PublicAPI]
        public void OnStartReceive() => UpdateStatusText();

        [PublicAPI]
        public void OnStopReceive() => UpdateStatusText();

        [PublicAPI]
        public void OnStartTransmit() => UpdateStatusText();

        [PublicAPI]
        public void OnStopTransmit() => UpdateStatusText();

        private void UpdateStatusText()
        {
            isVhfRxActivated = transceiver.RxPower;
            activeFrequencyText = (transceiver.Channel * 0.001).ToString("000.000");
            var vhfStatus = transceiver.TxPower
                ? "TX"
                : isVhfRxActivated ? "RX" : "OFF";
            vhfStatusOverviewText = vhfStatus + "\n" + activeFrequencyText;
        }
    }
}