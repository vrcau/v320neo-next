using System;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VAU.V320NeoNext.Runtime.Bus;
using VAU.V320NeoNext.Runtime.Systems.IndicatingRecording.EfisControl;
using VAU.V320NeoNext.Runtime.Systems.LegacFmgc;
using VAU.V320NeoNext.Runtime.Systems.LegacyFlightDataProvider;
using VAU.V320NeoNext.Runtime.Systems.LegacyFlightDataProvider.LegacyADRIRU;
using VAU.V320NeoNext.Runtime.Systems.LegacyInstrument.Utils;
using VirtualCNS;

namespace VAU.V320NeoNext.Runtime.Systems.LegacyInstrument.EFIS.ND.Script
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class NDDisplay : AbstractAvionicsBusClient
    {
        private const float MAX_SLIP_ANGLE = 50;

        private int _mainDataSource = 1;

        public bool useRightEfis;

        private AvionicsBusByteDataIds _navigationDisplayPageId;
        private AvionicsBusByteDataIds _navigationDisplayFilterId;

        private NavigationDisplayPage NavigationDisplayPage
        {
            get => (NavigationDisplayPage)Convert.ToInt32(_ReadByte(_navigationDisplayPageId));
            set => _WriteAndNotifyByte(_navigationDisplayPageId, Convert.ToByte(value));
        }

        private NavigationDisplayFilter NavigationDisplayMapFilterType
        {
            get => (NavigationDisplayFilter)Convert.ToInt32(_ReadByte(_navigationDisplayFilterId));
            set => _WriteAndNotifyByte(_navigationDisplayFilterId, Convert.ToByte(value));
        }

        [Tooltip("仪表的动画控制器")] public Animator IndicatorAnimator;
        public CDIAnimationDriver CDIAnimator;

        private FMGC _fmgc;

        private DependenciesInjector _injector;

        private MapDisplay[] _mapDisplays;

        private NavSelector _vor1;
        private NavSelector _vor2;
        private NavSelector _ils;

        private NavSelector _currentNavDataSource; //当前仪表上主界面导航信息来源

        private ADIRU _adiru;
        private SystemEventBus _eventBus;

        public Transform receiverTransform;

        protected override void _OnAvionicsBusStart()
        {
            if (useRightEfis)
            {
                _navigationDisplayPageId =
                    AvionicsBusByteDataIds.V32NN_Infrequent_EFIS_Right_Sync_NavigationDisplayPage;
                _navigationDisplayFilterId =
                    AvionicsBusByteDataIds.V32NN_Infrequent_EFIS_Right_Sync_NavigationDisplayFilter;
                _mainDataSource = 2;
            }
            else
            {
                _navigationDisplayPageId =
                    AvionicsBusByteDataIds.V32NN_Infrequent_EFIS_Left_Sync_NavigationDisplayPage;
                _navigationDisplayFilterId =
                    AvionicsBusByteDataIds.V32NN_Infrequent_EFIS_Left_Sync_NavigationDisplayFilter;
                _mainDataSource = 1;
            }

            _injector = DependenciesInjector.GetInstance(this);
            _adiru = _injector.adiru;
            _fmgc = _injector.fmgc;

            _vor1 = _fmgc.radNav.VOR1;
            _vor2 = _fmgc.radNav.VOR2;
            _ils = _fmgc.radNav.ILS;

            _currentNavDataSource = _ils;

            _mapDisplays = GetComponentsInChildren<MapDisplay>(true);

            if (!receiverTransform) receiverTransform = transform;

            foreach (var mapDisplay in _mapDisplays)
            {
                mapDisplay._Init();
            }

            UpdateNavigationDisplayPage();
            UpdateNavigationDisplayMapFilter();

            _SubscribeByte(_navigationDisplayPageId, nameof(_OnNavigationDisplayPageChanged));
            _SubscribeByte(_navigationDisplayFilterId, nameof(_OnNavigationDisplayMapFilterChanged));
        }

        public void _OnNavigationDisplayPageChanged() => UpdateNavigationDisplayPage();
        public void _OnNavigationDisplayMapFilterChanged() => UpdateNavigationDisplayMapFilter();

        #region Animation Hashs

        private readonly int HEADING_HASH = Animator.StringToHash("HeadingNormalize");
        private readonly int SLIP_ANGLE_HASH = Animator.StringToHash("SlipAngleNormalize");

        #endregion

        #region UI Elements

        [Header("UI element")] public Text TASText;

        public Text GSText;

        [Header("WayPoint & Navigation Indicatior")]
        public Text line1Text; //NI下 ILS or VOR1

        public Text line2Text; //NI下  频率
        public Text line3Text; //NI下 "CRS"
        public Text line4Text; //NI下 NAME

        [Header("Navaid indication")] public Text VOR1Name;

        public Text VOR1Dist;

        public Text VOR2Name;
        public Text VOR2Dist;

        public GameObject VOR1SelectOnly;
        public GameObject VOR2SelectOnly;
        public GameObject NavInfoIndicatior;

        public GameObject GSIndicator;

        #endregion

        #region EFIS Indicator Elements

        [Header("Pages")] public GameObject ARCPage;

        public GameObject VORPage;
        public GameObject ILSPage;

        #endregion

        #region Update

        private readonly float UPDATE_INTERVAL = UpdateIntervalUtil.GetUpdateIntervalFromFPS(30);
        private float _lastUpdate;

        private void LateUpdate()
        {
            if (!UpdateIntervalUtil.CanUpdate(ref _lastUpdate, UPDATE_INTERVAL)) return;

            UpdateHeading();
            UpdateSlip();
            TASText.text = _adiru.adr.trueAirSpeed.ToString("f0");
            GSText.text = _adiru.irs.groundSpeed.ToString("f0");

            UpdateNavigation();
        }

        private void UpdateHeading()
        {
            IndicatorAnimator.SetFloat(HEADING_HASH, _adiru.irs.heading / 360f);
        }

        private void UpdateSlip()
        {
            IndicatorAnimator.SetFloat(SLIP_ANGLE_HASH,
                Mathf.Clamp01((_adiru.irs.trackSlipAngle + MAX_SLIP_ANGLE) / (MAX_SLIP_ANGLE + MAX_SLIP_ANGLE)));
        }

        #region Navaid

        private void UpdateNavigation()
        {
            VOR1SelectOnly.SetActive(_vor1.Index >= 0);
            VOR2SelectOnly.SetActive(_vor2.Index >= 0);


            NavInfoIndicatior.SetActive(_vor2.Index >= 0);

            //功能：waypoint 更新 右下角距离更新 向台背台（TODO）
            //Waypoint & Navaid indication 先只实现一下Navaid indication模式
            //种类

            UpdateNavigationInfo();

            //switch (MainDataSource) {
            //    case 1:
            //        UpdateNavigationInfo(_vor1);
            //        break;
            //    case 2:
            //        UpdateNavigationInfo(_vor2);
            //        break;
            //    default:
            //        UpdateNavigationInfo(_ils);
            //        break;
            //}
        }

        private void UpdateNavigationInfo()
        {
            if (_currentNavDataSource == null) return;

            if (NavigationDisplayPage != NavigationDisplayPage.Plan)
            {
                if (_vor1.Index >= 0)
                {
                    VOR1Name.text = _vor1.Identity;
                    VOR1Dist.text = _vor1.HasDME
                        ? (Vector3.Distance(receiverTransform.position, GetNavaidPosition(_vor1)) / 1852.0f)
                        .ToString("f2")
                        : "--.-";
                }

                if (_vor2.Index >= 0)
                {
                    VOR2Name.text = _vor2.Identity;
                    VOR2Dist.text = _vor2.HasDME
                        ? (Vector3.Distance(receiverTransform.position, GetNavaidPosition(_vor2)) / 1852.0f)
                        .ToString("f2")
                        : "--.-";
                }
            }

            switch (NavigationDisplayPage)
            {
                case NavigationDisplayPage.Ils:
                    line1Text.text = $"ILS{_mainDataSource}";
                    if (_currentNavDataSource.Index >= 0)
                    {
                        //频率
                        line2Text.text =
                            $"<color={AirbusAvionicsTheme.Carmine}>{(_currentNavDataSource.Index >= 0 ? _currentNavDataSource.database.frequencies[_currentNavDataSource.Index].ToString("f2") : "---.--")}</color>";
                        line3Text.text =
                            $"CRS <color={AirbusAvionicsTheme.Carmine}>{_currentNavDataSource.Course:f0}</color> <color={AirbusAvionicsTheme.Blue}>°</color>";
                        line4Text.text =
                            $"<color={AirbusAvionicsTheme.Carmine}>NaviData1.SelectedBeacon.beaconName</color>";
                    }

                    break;
                case NavigationDisplayPage.Vor:
                    if (_currentNavDataSource.Index >= 0)
                    {
                        line1Text.text = $"VOR{_mainDataSource}";
                        //频率
                        line2Text.text = _currentNavDataSource.Index >= 0
                            ? _currentNavDataSource.database.frequencies[_currentNavDataSource.Index].ToString("f2")
                            : "---.--";
                        line3Text.text = "CRS";

                        line4Text.text = _vor1.Identity;
                    }

                    break;
            }
        }

        private Vector3 GetNavaidPosition(NavSelector navSelector)
        {
            var t = navSelector.NavaidTransform;
            return (t ? t : transform).position;
        }

        #endregion

        #endregion

        #region Navigation Display Pages

        private void UpdateNavigationDisplayPage()
        {
            ARCPage.SetActive(false);
            VORPage.SetActive(false);
            ILSPage.SetActive(false);

            switch (NavigationDisplayPage)
            {
                case NavigationDisplayPage.Ils:
                    ILSPage.SetActive(true);
                    _currentNavDataSource = _ils;
                    break;
                case NavigationDisplayPage.Vor:
                    VORPage.SetActive(true);
                    _currentNavDataSource = _vor1;
                    break;
                case NavigationDisplayPage.Arc:
                    ARCPage.SetActive(true);
                    _currentNavDataSource = _ils;
                    break;
                default:
                    //ARCPage.SetActive(true);
                    NavigationDisplayPage =
                        (NavigationDisplayPage)(((int)NavigationDisplayPage + 1) % 5); //页面为空的话自动跳到下一个页面
                    break;
            }

            CDIAnimator.navaidSelector = _currentNavDataSource;
        }

        [PublicAPI]
        public void NDPageNextLocal()
        {
            NavigationDisplayPage = (NavigationDisplayPage)((int)NavigationDisplayPage + 1);
        }

        [PublicAPI]
        public void NDPagePrevLocal()
        {
            NavigationDisplayPage = (NavigationDisplayPage)((int)NavigationDisplayPage - 1);
        }

        [PublicAPI]
        public void NDPageChangeLocal()
        {
            // for one direction
            NavigationDisplayPage = (NavigationDisplayPage)(((int)NavigationDisplayPage + 1) % 5);
        }

        #endregion

        #region EFIS

        private void SetVisibilityType(NavigationDisplayFilter visibilityType)
        {
            Debug.Log(nameof(SetVisibilityType) + " " + visibilityType);
            NavigationDisplayMapFilterType = visibilityType;
        }

        private void UpdateNavigationDisplayMapFilter()
        {
            Debug.Log(nameof(UpdateNavigationDisplayMapFilter) + " " + NavigationDisplayMapFilterType);
            foreach (var mapDisplay in _mapDisplays)
                mapDisplay.SetVisibilityType(NavigationDisplayMapFilterType);
        }

        // For TouchSwitch Event
        [PublicAPI]
        public void ToggleVisibilityTypeCSTR()
        {
            ToggleVisibilityType(NavigationDisplayFilter.Constraint);
        }

        [PublicAPI]
        public void ToggleVisibilityTypeWPT()
        {
            Debug.Log(nameof(ToggleVisibilityTypeWPT));
            ToggleVisibilityType(NavigationDisplayFilter.Waypoint);
        }

        [PublicAPI]
        public void ToggleVisibilityTypeVORD()
        {
            Debug.Log(nameof(ToggleVisibilityTypeVORD));
            ToggleVisibilityType(NavigationDisplayFilter.VorDme);
        }

        [PublicAPI]
        public void ToggleVisibilityTypeNDB()
        {
            ToggleVisibilityType(NavigationDisplayFilter.Ndb);
        }

        [PublicAPI]
        public void ToggleVisibilityTypeAPPT()
        {
            ToggleVisibilityType(NavigationDisplayFilter.Airport);
        }

        [PublicAPI]
        private void ToggleVisibilityType(NavigationDisplayFilter type)
        {
            SetVisibilityType(NavigationDisplayMapFilterType == type ? NavigationDisplayFilter.None : type);
        }

        #endregion
    }
}