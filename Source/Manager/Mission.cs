using System;
using System.Collections.Generic;
using MechJim.Extensions;
using UnityEngine;

namespace MechJim.Manager {

    [Enable(typeof(AutoPanel), typeof(AutoStage))]
    public class Mission: ManagerBase {
        public delegate StateFn StateFn(bool f);

        public StateFn missionState;

        public Mission(Core core): base(core) { }

        protected override void OnDisable() {
            ascent.Disable();
            node.Disable();
        }

        protected override void OnEnable() {
            missionState = MissionPrelaunch;
        }

        public override void OnFixedUpdate() {
            StateFn lastMissionState = missionState;

            missionState = missionState(false);
            while (missionState != lastMissionState ) {
                Debug.Log("changed state from: " + lastMissionState.Method.Name + " to " + missionState.Method.Name);
                lastMissionState = missionState;
                missionState = missionState(true);
            }
        }

        private StateFn MissionPrelaunch(bool stateChanged) {
            return MissionLaunch;
        }

        /* FIXME: AscentManager is only enabled for this state */
        private StateFn MissionLaunch(bool stateChanged) {
            if (stateChanged) {
                ascent.Enable();
            }
            if (ascent.done)
                return MissionCircularize;
            return MissionLaunch;
        }


        /* FIXME: NodeExecutor is only enabled for this state */
        private StateFn MissionCircularize(bool stateChanged) {
            if (stateChanged) {
                var maneuver = new Maneuver.Circularize(vessel, orbit, Planetarium.GetUniversalTime() + orbit.timeToAp);
                maneuver.PlaceManeuverNode();
                ascent.Disable();
                node.Enable();
            }
            return MissionCircularize;
        }
    }
}
