using A320VAU.Common;
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;
using VirtualCNS;

namespace A320VAU.FMGC {
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [DefaultExecutionOrder(2050)]// after FWS
    public class FMGC : UdonSharpBehaviour
    {
        public NavaidDatabase NavaidDatabase => injector.navaidDatabase;

        public FMGCRadNav radNav;
        public FMGCFlightPhase flightPhase;
        public FMGCFlightPlan flightPlan;
        public FMGCPerformance performance;

        public DependenciesInjector injector;

        private void Start() {
            injector = DependenciesInjector.GetInstance(this);

            if (!NavaidDatabase)
                Debug.LogError("You don't have a NavaidDatabase in your scene, FMGC won't work.", this);

            radNav = GetComponentInChildren<FMGCRadNav>();
            radNav.fmgc = this;

            flightPhase = GetComponentInChildren<FMGCFlightPhase>();
            flightPhase.fmgc = this;

            flightPlan = GetComponentInChildren<FMGCFlightPlan>();
            flightPlan.fmgc = this;

            performance = GetComponentInChildren<FMGCPerformance>();
            performance.fmgc = this;
        }
    }
}