using System;
using System.Collections.Generic;
using MechJim.Extensions;
using UnityEngine;

namespace MechJim.Manager {

    [Enable(typeof(AutoPanel), typeof(AutoStage), typeof(AutoFairing))]
    public class Mission: ManagerBase {
        public enum MissionState {
            PRELAUNCH,
            LAUNCH,
            CIRCULARIZE,
        }

        public MissionState missionState;

        private IDictionary<MissionState, Func<bool, MissionState>> missionMapping;

        public Mission(Core core): base(core) {
            missionMapping = new Dictionary<MissionState, Func<bool, MissionState>> {
                { MissionState.PRELAUNCH, MissionPrelaunch },
                { MissionState.LAUNCH, MissionLaunch },
                { MissionState.CIRCULARIZE, MissionCircularize },
            };
        }

        protected override void OnDisable() {
            core.ascent.Disable();
            core.node.Disable();
        }

        protected override void OnEnable() {
            missionState = MissionState.PRELAUNCH;
        }

        public override void OnFixedUpdate() {
            MissionState lastMissionState = missionState;

            missionState = missionMapping[missionState](false);
            while (missionState != lastMissionState ) {
                lastMissionState = missionState;
                missionState = missionMapping[missionState](true);
            }
        }

        private MissionState MissionPrelaunch(bool stateChanged) {
            return MissionState.LAUNCH;
        }

        /* FIXME: AscentManager is only enabled for this state */
        private MissionState MissionLaunch(bool stateChanged) {
            if (stateChanged) {
                core.ascent.Enable();
            }
            if (core.ascent.done)
                return MissionState.CIRCULARIZE;
            return MissionState.LAUNCH;
        }

        /* FIXME: NodeExecutor is only enabled for this state */
        private MissionState MissionCircularize(bool stateChanged) {
            if (stateChanged) {
                var maneuver = new Maneuver.Circularize(vessel, orbit, Planetarium.GetUniversalTime() + orbit.timeToAp);
                maneuver.PlaceManeuverNode();
                core.ascent.Disable();
                core.node.Enable();
            }
            return MissionState.CIRCULARIZE;
        }
    }
}
