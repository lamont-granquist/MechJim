using System;
using System.Collections.Generic;
using MechJim.Extensions;
using UnityEngine;
using KSP.UI.Screens;

namespace MechJim.Manager {

    [Enable(typeof(AutoPanel), typeof(AutoChute), typeof(AutoScience), typeof(AutoStage), typeof(WarpManager))]
    public class Mission: ManagerBase {
        public delegate StateFn StateFn();

        public StateFn missionState;

        public Mission(Core core): base(core) { }

        protected override void OnDisable() {
            ascent.UnRegister(this);
            node.UnRegister(this);
        }

        protected override void OnEnable() {
            missionState = Begin;
        }

        public override void OnFixedUpdate() {
            StateFn lastMissionState = missionState;

            missionState = missionState();
            if (missionState != lastMissionState ) {
                Debug.Log("changed state from: " + lastMissionState.Method.Name + " to " + missionState.Method.Name);
            }
        }

        private StateFn Begin() {
            autostage.Register(this);
            /* [LOG 07:41:56.657] NEW BEST mass: 9.85823153331876int: 65653.6212357011 start_alt: 50 start_turn: 19.8115793679895 maxQ: 27.8286833352543 */
            ascent.intermediate_altitude = 65653.6212357011;
            ascent.start_speed = 0;
            ascent.start_altitude = 50;
            ascent.start_turn = 19.8115793679895;
            ascent.maxQlimit = 27.8286833352543;
            ascent.Register(this);
            autostage.maxstage = 4;
            return WaitAscend;
        }

        private StateFn WaitAscend() {
            if (ascent.done)
                return StartCirc;
            return WaitAscend;
        }

        private StateFn StartCirc() {
            var maneuver = new Maneuver.Circularize(vessel, orbit, Planetarium.GetUniversalTime() + orbit.timeToAp);
            maneuver.PlaceManeuverNode();
            ascent.UnRegister(this);
            node.Register(this);
            return WaitNode;
        }

        private StateFn WaitNode() {
            if (!node.enabled) {  /* FIXME: standardized "done" method */
                return Prograde;
            }
            return WaitNode;
        }

        private StateFn Prograde() {
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
        double scienceEnd;

        private StateFn CoastStart() {
            coastEnd = Planetarium.GetUniversalTime() + 3600 * 18;
            scienceEnd = Planetarium.GetUniversalTime() + orbit.period * 2;
            autoscience.AutoWarpRate(100);
            return WaitScience;
        }

        private StateFn WaitScience() {
            if (Planetarium.GetUniversalTime() > scienceEnd) {
                return DoneScience;
            }
            return WaitScience;
        }

        private StateFn DoneScience() {
            autoscience.AutoWarpRate(0);
            warp.WarpToUT(this, coastEnd);
            return WaitCoast;
        }

        private StateFn WaitCoast() {
            if (Planetarium.GetUniversalTime() > coastEnd) {
                return Retrograde;
            }
            return WaitCoast;
        }

        private StateFn Retrograde() {
            attitude.attitudeTo(-Vector3d.forward, AttitudeReference.ORBIT);
            return WaitRetrograde;
        }

        private StateFn WaitRetrograde() {
            if (attitude.AngleFromTarget() < 1) {
                return ReentryBurn;
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
            return WarpToAtm;
        }

        private StateFn WarpToAtm() {
            warp.WarpToAtmosphericEntry(this);
            return WaitForAtm;
        }

        private StateFn WaitForAtm() {
            if (!warp.enabled) {
                return WarpToGround;
            }
            return WaitForAtm;
        }

        private StateFn WarpToGround() {
            attitude.attitudeTo(-Vector3d.forward, AttitudeReference.ORBIT);
            warp.WarpAtPhysicsRate(this, 4);
            return WaitForGround;
        }

        private StateFn WaitForGround() {
            if (vessel.situation == Vessel.Situations.LANDED || vessel.situation == Vessel.Situations.SPLASHED)  /* FIXME: optimism */
                return Done;
            return WaitForGround;
        }

        private StateFn Done() {
            UnRegister(this);
            return Done;
        }
    }
}
