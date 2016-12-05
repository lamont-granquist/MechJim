using System;
using System.Collections.Generic;
using MechJim.Extensions;
using UnityEngine;
using KSP.UI.Screens;

namespace MechJim.Manager {

    [Enable(typeof(AutoPanel))]
    public class Mission: ManagerBase {
        public delegate StateFn StateFn();

        public StateFn missionState;

        public Mission(Core core): base(core) { }

        protected override void OnDisable() {
            ascent.Disable();
            node.Disable();
            autostage.Disable();
        }

        protected override void OnEnable() {
            missionState = Start;
        }

        public override void OnFixedUpdate() {
            StateFn lastMissionState = missionState;

            missionState = missionState();
            if (missionState != lastMissionState ) {
                Debug.Log("changed state from: " + lastMissionState.Method.Name + " to " + missionState.Method.Name);
            }
        }

        private StateFn Start() {
            autostage.Enable();
            /* int_alt: 65653.6212357011 start_alt: 31.1377950716474 start_turn: 19.4863715448819 maxQ: 29.2071798789348 */
            ascent.intermediate_altitude = 65653.6212357011;
            ascent.start_speed = 0;
            ascent.start_altitude = 31.1377950716474;
            ascent.start_turn = 19.4863715448819;
            ascent.maxQlimit = 29.2071798789348;
            ascent.Enable();
            return WaitAscend;
        }

        /* FIXME: AscentManager, AutoStage are enabled for this stage */
        private StateFn WaitAscend() {
            if (ascent.done)
                return StartCirc;
            return WaitAscend;
        }

        /* FIXME: NodeExecutor, AutoStage are enabled for this stage */
        private StateFn StartCirc() {
            var maneuver = new Maneuver.Circularize(vessel, orbit, Planetarium.GetUniversalTime() + orbit.timeToAp);
            maneuver.PlaceManeuverNode();
            ascent.Disable();
            node.Enable();
            return WaitNode;
        }

        private StateFn WaitNode() {
            if (!node.enabled) {  /* FIXME: standardized "done" method */
                return Prograde;
            }
            return WaitNode;
        }

        private StateFn Prograde() {
            autostage.Disable();
            attitude.attitudeTo(Vector3d.forward, AttitudeReference.ORBIT);
            return WaitPrograde;
        }

        private StateFn WaitPrograde() {
            if (attitude.AngleFromTarget() < 1) {
                return Decouple;
            }
            return WaitPrograde;
        }

        private StateFn Decouple() {
            StageManager.ActivateNextStage();
            return Sun;
        }

        private StateFn Sun() {
            attitude.attitudeTo(Vector3d.forward, AttitudeReference.SUN);
            return WaitSun;
        }

        private StateFn WaitSun() {
            if (attitude.AngleFromTarget() < 1) {
                return CoastStart;
            }
            return WaitSun;
        }

        double coastEnd;

        private StateFn CoastStart() {
            coastEnd = Planetarium.GetUniversalTime() + 3600 * 18;
            warp.WarpToUT(coastEnd);
            return WaitCoast;
        }

        private StateFn WaitCoast() {
            if (Planetarium.GetUniversalTime() > coastEnd) {
                return Retrograde;
            }
            return WaitCoast;
        }

        private StateFn Retrograde() {
            warp.Disable();
            attitude.attitudeTo(-Vector3d.forward, AttitudeReference.ORBIT);
            return WaitRetrograde;
        }

        private StateFn WaitRetrograde() {
            if (attitude.AngleFromTarget() < 1) {
                return Decouple2;
            }
            return WaitRetrograde;
        }

        private StateFn ReentryBurn() {
            StageManager.ActivateNextStage();
            return WaitReentryBurn;
        }

        private StateFn WaitReentryBurn() {
            if (vesselState.thrustMaximum == 0) {
                return Decouple2;
            }
            return WaitReentryBurn;
        }

        private StateFn Decouple2() {
            StageManager.ActivateNextStage();
            return Done;
        }

        private StateFn Done() {
            return Done;
        }
    }
}
