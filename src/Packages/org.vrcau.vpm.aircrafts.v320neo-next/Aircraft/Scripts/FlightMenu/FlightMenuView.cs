using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VAU.V320NeoNext.Runtime.FlightMenu.MenuData;
using VAU.V320NeoNext.Runtime.FlightMenu.MenuData.Item;

namespace VAU.V320NeoNext.Runtime.FlightMenu
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public sealed class FlightMenuView : UdonSharpBehaviour
    {
        public FlightMenuGroup rootMenuGroup;

        [Header("Core")] 
        public FlightMenuController menuController;
        public FlightMenuBackItem backButtonItem;

        [Header("Popup Menu Extension")] 
        public bool isPopupMenu;

        public FlightMenuView mainMenuView;
        public GameObject popupMenuRoot;
        public FlightMenuView popupMenuView;

        [Header("Child Root")] 
        public Transform backgroundRoot;
        public Transform hoverRoot;
        public Transform activatedRoot;
        public Transform titleRoot;

        [Header("UI")] 
        public GameObject menuGroupTitleBox;
        public TextMeshProUGUI menuGroupTitleText;
        public TextMeshProUGUI menuGroupDescriptionText;
        public TextMeshProUGUI closePopupMenuTipText;

        public Color normalItemTitleColor = Color.white;
        public Color disabledItemTitleColor = new Color(1, 1, 1, 0.2f);

        [Header("Item Image Template")] 
        public GameObject backgroundClipTemplate;
        public GameObject hoverClipTemplate;
        public GameObject activatedClipTemplate;
        public GameObject titleTemplate;
        public float marginAngle = 2f;

        private int _itemNumber = 8;

        [Header("Debug Only")] 
        public FlightMenuGroup[] menuGroupHistory = new FlightMenuGroup[0];
        public FlightMenuGroup menuGroupActivated;
        public bool menuActivated;

        private GameObject[] _itemGenerated = new GameObject[0];
        private TextMeshProUGUI[] _itemTitleGenerated = new TextMeshProUGUI[0];
        private FlightMenuItemBase[] _flightMenuActivated = new FlightMenuItemBase[0];

        private void Start()
        {
            if (!isPopupMenu) NavigateToMenu(rootMenuGroup);
        }

        private void Update()
        {
            if (!menuActivated) return;

            for (var index = 0; index < _flightMenuActivated.Length; index++)
            {
                var menuItem = _flightMenuActivated[index];
                if (menuItem.updateIsActivatedFromEventTarget)
                {
                    var isActivated = (bool)menuItem.eventTarget.GetProgramVariable(menuItem.isActivatedVariableName);
                    if (menuItem.invertIsActivatedVariable) isActivated = !isActivated;
                    SetItemActivatedIndicator(index, isActivated);
                }
                else
                {
                    SetItemActivatedIndicator(index, menuItem.isActivated);
                }

                var titleText = _itemTitleGenerated[index];
                if (menuItem.updateTitleFromEventTarget)
                {
                    titleText.text = 
                        string.Format(
                            menuItem.titleTemplate, 
                            menuItem.eventTarget.GetProgramVariable(menuItem.titleVariableName));
                }

                var itemIsEnabled = IsMenuItemEnabled(menuItem);
                titleText.color = itemIsEnabled ? normalItemTitleColor : disabledItemTitleColor;
            }

            if (menuGroupActivated.keepUpdateGroupTitle)
            {
                UpdateMenuGroupTitleBox(menuGroupActivated);
            }
        }

        private void NavigateToMenu(FlightMenuGroup newMenuGroup, bool clearHistory = false)
        {
            menuActivated = false;
            if (clearHistory)
            {
                menuGroupActivated = null;
                ClearHistory();
            }

            var newMenuGroupItems = FilterHiddenMenuItems(newMenuGroup.menuItems);
            if (menuGroupActivated && menuGroupActivated != newMenuGroup)
            {
                PushHistory(menuGroupActivated);
                _flightMenuActivated = new FlightMenuItemBase[newMenuGroupItems.Length + 1];
                _flightMenuActivated[0] = backButtonItem;
                newMenuGroupItems.CopyTo(_flightMenuActivated, 1);
            }
            else
            {
                _flightMenuActivated = new FlightMenuItemBase[newMenuGroupItems.Length];
                newMenuGroupItems.CopyTo(_flightMenuActivated, 0);
            }

            UpdateMenuGroupTitleBox(newMenuGroup);

            menuGroupActivated = newMenuGroup;
            _itemNumber = _flightMenuActivated.Length;
            GenerateMenuView();
            menuController.RequestMenuUpdate(_itemNumber);

            menuActivated = true;
        }

        private FlightMenuItemBase[] FilterHiddenMenuItems(FlightMenuItemBase[] itemSource)
        {
            var filteredItems = new FlightMenuItemBase[itemSource.Length];
            var filteredItemIndex = 0;
            for (int index = 0; index < itemSource.Length; index++)
            {
                if (itemSource[index].isHide) continue;
                filteredItems[filteredItemIndex] = itemSource[index];
                filteredItemIndex++;
            }

            var newFilteredItems = new FlightMenuItemBase[filteredItemIndex];
            for (int index = 0; index < filteredItemIndex; index++)
            {
                newFilteredItems[index] = filteredItems[index];
            }
            return newFilteredItems;
        }

        private void GoBack()
        {
            if (menuGroupHistory.Length == 0) return;
            menuActivated = false;

            var menuToGoBack = PopHistory();
            var menuToGoBackItems = menuToGoBack.menuItems;

            if (menuGroupHistory.Length != 0)
            {
                _flightMenuActivated = new FlightMenuItemBase[menuToGoBackItems.Length + 1];
                _flightMenuActivated[0] = backButtonItem;

                menuToGoBackItems.CopyTo(_flightMenuActivated, 1);
            }
            else
            {
                _flightMenuActivated = new FlightMenuItemBase[menuToGoBackItems.Length];
                menuToGoBackItems.CopyTo(_flightMenuActivated, 0);
            }

            UpdateMenuGroupTitleBox(menuToGoBack);

            menuGroupActivated = menuToGoBack;
            _itemNumber = _flightMenuActivated.Length;
            GenerateMenuView();
            menuController.RequestMenuUpdate(_itemNumber);

            menuActivated = true;
        }

        private void UpdateMenuGroupTitleBox(FlightMenuGroup newMenuGroup)
        {
            var menuGroupTitleExist = !string.IsNullOrWhiteSpace(newMenuGroup.groupName);
            var menuGroupDescriptionExist = !string.IsNullOrWhiteSpace(newMenuGroup.description);
            if (!menuGroupTitleExist && !menuGroupDescriptionExist && !isPopupMenu)
            {
                menuGroupTitleBox.SetActive(false);
            }
            else
            {
                menuGroupTitleBox.SetActive(true);
                menuGroupTitleText.text = newMenuGroup.groupName;
                menuGroupTitleText.gameObject.SetActive(menuGroupTitleExist);
                menuGroupDescriptionText.text = newMenuGroup.description;
                menuGroupDescriptionText.gameObject.SetActive(menuGroupDescriptionExist);
                closePopupMenuTipText.gameObject.SetActive(isPopupMenu);
            }
        }

        private void GenerateMenuView()
        {
            foreach (var itemToDestroy in _itemGenerated)
            {
                Destroy(itemToDestroy);
            }

            var menuLength = _flightMenuActivated.Length;
            _itemGenerated = new GameObject[menuLength * 4];
            _itemTitleGenerated = new TextMeshProUGUI[menuLength];

            var anglePerItem = 360f / menuLength;
            var initialItemAngle = anglePerItem / 2f;
            var fillAmount = (anglePerItem - marginAngle) / 360f;

            for (var index = 0; index < menuLength; index++)
            {
                var targetLocalEulerAngle = new Vector3(0, 0, initialItemAngle - anglePerItem * index);

                // Background clip
                var backgroundItem = Instantiate(backgroundClipTemplate, backgroundRoot);
                backgroundItem.transform.localPosition = Vector3.zero;
                backgroundItem.transform.localEulerAngles = targetLocalEulerAngle;
                backgroundItem.gameObject.name = $"BackgroundClip_{index}";
                backgroundItem.GetComponentInChildren<Image>().fillAmount = fillAmount;

                _itemGenerated[index] = backgroundItem;

                // Hover clip
                var clipItem = Instantiate(hoverClipTemplate, hoverRoot);
                clipItem.transform.localPosition = Vector3.zero;
                clipItem.transform.localEulerAngles = targetLocalEulerAngle;
                clipItem.gameObject.name = $"HoverClip_{index}";
                clipItem.GetComponentInChildren<Image>().fillAmount = fillAmount;

                _itemGenerated[menuLength + index] = clipItem;

                // activated clip
                var activatedItem = Instantiate(activatedClipTemplate, activatedRoot);
                activatedItem.transform.localPosition = Vector3.zero;
                activatedItem.transform.localEulerAngles = targetLocalEulerAngle;
                activatedItem.gameObject.name = $"ActivatedClip_{index}";
                activatedItem.GetComponentInChildren<Image>().fillAmount = fillAmount;
                activatedItem.SetActive(false);

                _itemGenerated[menuLength * 2 + index] = activatedItem;

                // title and icon
                var titleItem = Instantiate(titleTemplate, titleRoot);
                titleItem.transform.localPosition = Vector3.zero;
                titleItem.transform.localEulerAngles = new Vector3(0, 0, anglePerItem * -index);
                titleItem.gameObject.name = $"TitleTemplate_{index}";

                var menuItem = _flightMenuActivated[index];

                var imageComponent = titleItem.GetComponentInChildren<Image>();
                if (menuItem.icon)
                {
                    imageComponent.sprite = menuItem.icon;
                    imageComponent.gameObject.SetActive(true);
                }
                else
                {
                    imageComponent.gameObject.SetActive(false);
                }

                var titleText = titleItem.GetComponentInChildren<TextMeshProUGUI>();
                titleText.text = menuItem.title;
                var itemIsEnabled = menuItem.updateIsEnabledFromEventTarget ?
                    (bool)menuItem.eventTarget.GetProgramVariable(menuItem.isDisabledVariableName) :
                    menuItem.isDisabled;
                titleText.color = itemIsEnabled ? normalItemTitleColor : disabledItemTitleColor;
                _itemTitleGenerated[index] = titleText;

                titleItem.GetComponentInChildren<FlightMenuItemTitleRotationTarget>()
                    .transform.localRotation = Quaternion.Inverse(titleItem.transform.localRotation);

                _itemGenerated[menuLength * 3 + index] = titleItem;
            }
        }

        public void _OnItemHover(int itemIndex)
        {
            if (itemIndex < 0 || itemIndex > _itemNumber)
            {
                Debug.LogWarning(
                    "_OnItemHover: itemIndex is out of range, input: " + itemIndex + ", max: " + _itemNumber);
                return;
            }

            _itemGenerated[_itemNumber + itemIndex].SetActive(true);
        }

        public void _OnItemHoverLost(int itemIndex)
        {
            if (itemIndex < 0 || itemIndex > _itemNumber)
            {
                Debug.LogWarning(
                    "_OnItemHoverLost: itemIndex is out of range, input: " + itemIndex + ", max: " + _itemNumber);
                return;
            }

            _itemGenerated[_itemNumber + itemIndex].SetActive(false);
        }

        public void _OnTriggerOnBlank()
        {
            if (isPopupMenu)
            {
                ReturnToMainMenu();
            }
        }

        public void _OnItemTrigger(int itemIndex)
        {
            if (itemIndex < 0 || itemIndex > _itemNumber)
            {
                Debug.LogWarning(
                    "_OnItemTrigger: itemIndex is out of range, input: " + itemIndex + ", max: " + _itemNumber);
                return;
            }

            var menuItem = _flightMenuActivated[itemIndex];
            var itemIsEnabled = IsMenuItemEnabled(menuItem);

            if (!itemIsEnabled) return;

            var triggerResult = menuItem.Trigger();

            switch (triggerResult)
            {
                case FlightMenuTriggerResult.Noop:
                    // Do nothing, all logic should handle by menu item itself
                    break;
                case FlightMenuTriggerResult.OpenNewMenu:
                    var newMenuGroup = menuItem.GetNewMenu();
                    if (!newMenuGroup)
                    {
                        Debug.LogWarning(
                            "_OnItemTrigger: triggerResult is OpenNewMenu, but GetNewMenu() returns null, itemIndex: " +
                            itemIndex);
                        return;
                    }

                    NavigateToMenu(newMenuGroup);
                    break;
                case FlightMenuTriggerResult.InternalBackMenu:
                    GoBack();
                    break;
                case FlightMenuTriggerResult.OpenPopupMenu:
                    if (isPopupMenu)
                    {
                        Debug.LogWarning(
                            "_OnItemTrigger: triggerResult is OpenPopupMenu, but this menu is already a popup menu, itemIndex: " + 
                            itemIndex);
                        break;
                    }

                    OpenPopupMenu(menuItem.GetNewMenu(), itemIndex);
                    break;
                case FlightMenuTriggerResult.RequestClosePopup:
                    ReturnToMainMenu();
                    break;
                default:
                    Debug.LogWarning(
                        "_OnItemTrigger: triggerResult is unknown, input: " + triggerResult + ", itemIndex: " +
                        itemIndex);
                    break;
            }
        }

        private void SetItemActivatedIndicator(int index, bool activated)
        {
            var item = _itemGenerated[_itemNumber  * 2 + index];
            item.SetActive(activated);
        }

        #region Popup Menu Handling

        private void OpenPopupMenu(FlightMenuGroup newMenuGroup, int itemIndex)
        {
            if (!popupMenuView || !popupMenuRoot || !mainMenuView)
            {
                Debug.LogWarning(
                    "OpenPopupMenu: popupMenuView/popupMenuRoot/mainMenuView is null, cannot open popup menu");
                return;
            }

            popupMenuView.NavigateToMenu(newMenuGroup, true);
            popupMenuRoot.SetActive(true);
            popupMenuRoot.GetComponent<RectTransform>().anchoredPosition = menuController.GetPopupMenuPosition(itemIndex);
            mainMenuView.menuController.enabled = false;
        }

        // For popup menu to bring main menu back and hide itself
        private void ReturnToMainMenu()
        {
            if (!popupMenuView || !popupMenuRoot || !mainMenuView)
            {
                Debug.LogWarning(
                    "ReturnToMainMenu: popupMenuView/popupMenuRoot/mainMenuView is null, cannot return to main menu");
                return;
            }

            popupMenuView.NavigateToMenu(popupMenuView.rootMenuGroup, true);
            popupMenuRoot.SetActive(false);
            mainMenuView.menuController.enabled = true;
        }

        #endregion

        #region Menu Group History

        private void PushHistory(FlightMenuGroup newMenuGroup)
        {
            var newHistoryArray = new FlightMenuGroup[menuGroupHistory.Length + 1];
            menuGroupHistory.CopyTo(newHistoryArray, 0);
            newHistoryArray[menuGroupHistory.Length] = newMenuGroup;
            menuGroupHistory = newHistoryArray;
        }

        private FlightMenuGroup PopHistory()
        {
            var lastMenuGroup = menuGroupHistory[menuGroupHistory.Length - 1];
            var newHistoryArray = new FlightMenuGroup[menuGroupHistory.Length - 1];
            Array.Copy(menuGroupHistory, newHistoryArray, menuGroupHistory.Length - 1);
            menuGroupHistory = newHistoryArray;

            return lastMenuGroup;
        }

        private void ClearHistory()
        {
            menuGroupHistory = new FlightMenuGroup[0];
        }

        #endregion

        private bool IsMenuItemEnabled(FlightMenuItemBase menuItem)
        {
            var itemIsDisabled = menuItem.updateIsEnabledFromEventTarget ?
                (bool)menuItem.eventTarget.GetProgramVariable(menuItem.isDisabledVariableName) :
                menuItem.isDisabled;

            return menuItem.invertIsDisabledVariable ? itemIsDisabled : !itemIsDisabled;
        }
    }
}