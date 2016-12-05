using System;
using System.Collections.Generic;
using MechJim.Extensions;
using UnityEngine;
using KSP.UI.Screens;

namespace MechJim.Manager {

    [Enable(typeof(AutoPanel))]
    public class Mission: ManagerBase {
        public delegate StateFn StateFn(bool f);

        public StateFn missionState;

        public Mission(Core core): base(core) { }

        protected override void OnDisable() {
            ascent.Disable();
            node.Disable();
            autostage.Disable();
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

        /* FIXME: AscentManager, AutoStage are enabled for this stage */
        private StateFn MissionLaunch(bool stateChanged) {
            if (stateChanged) {
                autostage.Enable();
                /* int_alt: 65653.6212357011 start_alt: 31.1377950716474 start_turn: 19.4863715448819 maxQ: 29.2071798789348 */
                ascent.intermediate_altitude = 65653.6212357011;
                ascent.start_speed = 0;
                ascent.start_altitude = 31.1377950716474;
                ascent.start_turn = 19.4863715448819;
                ascent.maxQlimit = 29.2071798789348;
                ascent.Enable();
            }
            if (ascent.done)
                return MissionCircularize;
            return MissionLaunch;
        }

        /* FIXME: NodeExecutor, AutoStage are enabled for this stage */
        private StateFn MissionCircularize(bool stateChanged) {
            if (stateChanged) {
                var maneuver = new Maneuver.Circularize(vessel, orbit, Planetarium.GetUniversalTime() + orbit.timeToAp);
                maneuver.PlaceManeuverNode();
                ascent.Disable();
                node.Enable();
            }
            if (!node.enabled) {  /* FIXME: standardized "done" method */
                return MissionDecouple;
            }
            return MissionCircularize;
        }

        /* FIXME: AttitudeManager is enabled for this stage */
        private StateFn MissionDecouple(bool stateChanged) {
            if (stateChanged) {
                autostage.Disable();
                attitude.attitudeTo(Vector3d.forward, AttitudeReference.ORBIT);
            }
            if (attitude.AngleFromTarget() < 1) {
                StageManager.ActivateNextStage();
                return MissionCoast;
            }
            return MissionDecouple;
        }

        double coastEnd;

        /* FIXME: AttitudeManager, WarpManager are enabled for this stage */
        private StateFn MissionCoast(bool stateChanged) {
            if (stateChanged) {
                coastEnd = Planetarium.GetUniversalTime() + 3600 * 18;
                warp.WarpToUT(coastEnd);
                attitude.attitudeTo(Vector3d.forward, AttitudeReference.SUN);
            }
            if (Planetarium.GetUniversalTime() > coastEnd) {
                return MissionRetrograde;
            }
            return MissionCoast;
        }

        /* FIXME: AttitudeManager is enabled for this stage */
        private StateFn MissionRetrograde(bool stateChanged) {
            if (stateChanged) {
                warp.Disable();
                attitude.attitudeTo(-Vector3d.forward, AttitudeReference.ORBIT);
            }
            if (attitude.AngleFromTarget() < 1) {
                StageManager.ActivateNextStage();
                return MissionReentryBurn;
            }
            return MissionRetrograde;
        }

        /* FIXME: AttitudeManager from last stage still in effect*/
        private StateFn MissionReentryBurn(bool stateChanged) {
            if (vesselState.thrustMaximum == 0) {
                StageManager.ActivateNextStage();  /* eject the utility section */
                return MissionReentryGlide;
            }
            return MissionReentryBurn;
        }

        private StateFn MissionReentryGlide(bool stateChanged) {
            return MissionReentryGlide;
        }
    }
}
