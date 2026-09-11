using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VAU.V320NeoNext.Runtime.Extensions;
using VAU.V320NeoNext.Runtime.Systems.IndicatingRecording.EfisControl;
using VAU.V320NeoNext.Runtime.Systems.LegacyFlightDataProvider;
using VAU.V320NeoNext.Runtime.Systems.LegacyFlightDataProvider.LegacyADRIRU;
using VAU.V320NeoNext.Runtime.Systems.LegacyInstrument.Utils;
using VirtualCNS;

namespace VAU.V320NeoNext.Runtime.Systems.LegacyInstrument.EFIS.ND.Script
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(1000)] // After Virtual-CNS NavaidDatabase
    public class MapDisplay : UdonSharpBehaviour
    {
        private readonly float UPDATE_INTERVAL = UpdateIntervalUtil.GetUpdateIntervalFromFPS(10);
        private float _lastUpdate;

        [Tooltip("unit: nm")] public int defaultRange = 20;

        private DependenciesInjector _injector;
        private ADIRU _adiru; // Temp workaround

        private GameObject[] _markers = { };
        private NavaidDatabase _navaidDatabase;
        private float magneticDeclination;
        private float scale;

        private NavigationDisplayFilter _currentMapFilterType;

        private bool _isInitialized;

        [PublicAPI] public int Range { get; private set; }

        private void Start()
        {
            _Init();
        }

        public void _Init()
        {
            if (_isInitialized) return;

            _injector = DependenciesInjector.GetInstance(this);

            _navaidDatabase = _injector.navaidDatabase;
            _adiru = _injector.adiru;

            if (_navaidDatabase == null)
            {
                Debug.LogError("Can't get NavaidDatabase instance, Map unavailable", this);
                gameObject.SetActive(false);
                return;
            }

            magneticDeclination = _navaidDatabase.magneticDeclination;
            _isInitialized = true;

            InstantiateMarkers(defaultRange, NavigationDisplayFilter.None);
        }

        private void Update()
        {
            if (!_isInitialized) return;
            if (!UpdateIntervalUtil.CanUpdate(ref _lastUpdate, UPDATE_INTERVAL)) return;

            var aircraftPosition = _adiru.irs.position;
            var heading = _adiru.irs.heading;

            var mapRotation = Quaternion.Euler(0, 0, heading);
            transform.localRotation = mapRotation;

            var uiOffset = -aircraftPosition * scale;
            transform.localPosition = mapRotation * uiOffset;

            var inverseRotation = Quaternion.Inverse(mapRotation);
            UpdateMarkerRotations(_markers, inverseRotation);
        }

        private void InstantiateMarkers(int range, NavigationDisplayFilter efisFilterType)
        {
            Range = range;
            _currentMapFilterType = efisFilterType;
            scale = uiRadius / (range * 926.0f);

            Debug.Log($"PreInstantiateMarkers: range={range}, efisFilterType={efisFilterType}");
            if (!_isInitialized) return;
            Debug.Log($"InstantiateMarkers: range={range}, efisFilterType={efisFilterType}");

            foreach (var marker in _markers) Destroy(marker);
            _markers = new GameObject[0];

            for (var index = 0; index < _navaidDatabase.identities.Length; index++)
            {
                var type = (NavaidCapability)_navaidDatabase.capabilities[index];
                if (type == NavaidCapability.ILS) break;

                var identity = _navaidDatabase.identities[index];
                var navaidTransform = _navaidDatabase.transforms[index];

                switch (type)
                {
                    case NavaidCapability.NDB:
                        if (efisFilterType == NavigationDisplayFilter.Ndb)
                            _markers = _markers.Add(InstantiateMarker(ndbTemplate, identity, navaidTransform));
                        break;
                    case NavaidCapability.VOR:
                        if (efisFilterType == NavigationDisplayFilter.VorDme)
                            _markers = _markers.Add(InstantiateMarker(vorTemplate, identity, navaidTransform));
                        break;
                    case NavaidCapability.VORDME:
                        if (efisFilterType == NavigationDisplayFilter.VorDme)
                            _markers = _markers.Add(InstantiateMarker(vorDmeTemplate, identity, navaidTransform));
                        break;
                    default:
                        if (efisFilterType == NavigationDisplayFilter.VorDme)
                            _markers = _markers.Add(InstantiateMarker(dmeOrTacanTemplate, identity, navaidTransform));
                        break;
                }
            }

            if (efisFilterType != NavigationDisplayFilter.Waypoint &&
                efisFilterType != NavigationDisplayFilter.Airport) return;
            for (var index = 0; index < _navaidDatabase.waypointIdentities.Length; index++)
            {
                var identity = _navaidDatabase.waypointIdentities[index];
                var waypointTransform = _navaidDatabase.waypointTransforms[index];
                var type = (WaypointType)_navaidDatabase.waypointTypes[index];

                switch (type)
                {
                    case WaypointType.Aerodrome:
                        if (efisFilterType == NavigationDisplayFilter.Airport)
                            _markers = _markers.Add(InstantiateMarker(airportTemplate, identity, waypointTransform));
                        break;
                    default:
                        if (efisFilterType == NavigationDisplayFilter.Waypoint)
                            _markers = _markers.Add(InstantiateMarker(waypointTemplate, identity, waypointTransform));
                        break;
                }
            }
        }

        private GameObject InstantiateMarker(GameObject template, string identity, Transform navaidTransform)
        {
            var marker = Instantiate(template);
            var markerTransform = marker.transform;
            markerTransform.gameObject.name = $"Marker-{identity}";
            markerTransform.SetParent(transform, false);
            markerTransform.GetComponentInChildren<Text>().text = identity;

            var navaidPosition = navaidTransform.position * scale;
            markerTransform.localPosition = Vector3.right * navaidPosition.x + Vector3.up * navaidPosition.z;

            return marker;
        }

        private static void UpdateMarkerRotations(GameObject[] markers, Quaternion rotation)
        {
            foreach (var marker in markers)
            {
                if (marker == null) continue;
                marker.transform.localRotation = rotation;
            }
        }

        [PublicAPI]
        public void SetRange(int range)
        {
            InstantiateMarkers(range, _currentMapFilterType);
        }

        [PublicAPI]
        public void SetVisibilityType(NavigationDisplayFilter visibilityType)
        {
            InstantiateMarkers(Range, visibilityType);
        }

        #region UI Elements

        public int uiRadius = 180;

        // Templates
        public GameObject vorTemplate,
            vorDmeTemplate,
            ndbTemplate,
            dmeOrTacanTemplate,
            waypointTemplate,
            airportTemplate;

        #endregion
    }
}