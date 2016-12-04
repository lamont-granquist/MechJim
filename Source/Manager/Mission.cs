using System;
using System.Collections.Generic;
using MechJim.Extensions;
using UnityEngine;

namespace MechJim.Manager {
    public class MissionState {
        /* singleton */
        private static readonly MissionState instance = new MissionState();
        static MissionState() {
        }
        private MissionState() {
        }
        public static MissionState Instance {
            get {
                return instance;
            }
        }

        public double best_mass = -1;
        public double best_intermediate_altitude = 64262.0608998752;
        public double best_start_altitude = 45.4472485070337;
        public double best_start_turn = 20.2655437389694;
        public double best_maxQlimit = 28.5327588131338;
    }

    [Enable(typeof(AutoPanel))]
    public class Mission: ManagerBase {
        public delegate StateFn StateFn(bool f);

        public StateFn missionState;

        private System.Random rnd;

        public Mission(Core core): base(core) {
            rnd = new System.Random();
        }

        private void GuessNextParams() {
            ascent.intermediate_altitude = MissionState.Instance.best_intermediate_altitude;
            ascent.start_altitude = MissionState.Instance.best_start_altitude;
            ascent.start_turn = MissionState.Instance.best_start_turn;
            ascent.maxQlimit = MissionState.Instance.best_maxQlimit;
            if (rnd.NextDouble() > 0.5)
                ascent.intermediate_altitude += ( rnd.NextDouble() - 0.5 ) * 10000;  /* +/- 5,000m */
            if (rnd.NextDouble() > 0.5)
                ascent.start_altitude += ( rnd.NextDouble() - 0.5 ) * 20; /* +/- 10m */
            if (rnd.NextDouble() > 0.5)
                ascent.start_turn += ( rnd.NextDouble() - 0.5 ) * 2; /* +/- 1 degree */
            if (rnd.NextDouble() > 0.5)
                ascent.maxQlimit += ( rnd.NextDouble() - 0.5 ) * 10; /* +/- 5 kPa */
            Debug.Log("NEXT GUESS int: " + ascent.intermediate_altitude + " start_alt: " + ascent.start_altitude + " start_turn: " + ascent.start_turn + " maxQ: " + ascent.maxQlimit);
        }

        protected override void OnDisable() {
            ascent.Disable();
            node.Disable();
            autostage.Disable();
        }

        protected override void OnEnable() {
            /* starting params */
            ascent.target_altitude = 100000;
            ascent.start_speed = 0;
            GuessNextParams();
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

        /* FIXME: AutoStage is *NOT* enabled for this stage */
        private StateFn MissionPrelaunch(bool stateChanged) {
            if ( FlightInputHandler.state.mainThrottle > 0.0f )
                return MissionLaunch;
            return MissionPrelaunch;
        }

        /* FIXME: AscentManager is only enabled for this state */
        private StateFn MissionLaunch(bool stateChanged) {
            if (stateChanged) {
                autostage.Enable();
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
            if (!node.enabled) {
                /* success */
                if (vesselState.mass > MissionState.Instance.best_mass) {
                    Debug.Log("NEW BEST mass: " + vesselState.mass + "int: " + ascent.intermediate_altitude + " start_alt: " + ascent.start_altitude + " start_turn: " + ascent.start_turn + " maxQ: " + ascent.maxQlimit);
                    MissionState.Instance.best_intermediate_altitude = ascent.intermediate_altitude;
                    MissionState.Instance.best_start_altitude = ascent.start_altitude;
                    MissionState.Instance.best_start_turn = ascent.start_turn;
                    MissionState.Instance.best_maxQlimit = ascent.maxQlimit;
                    MissionState.Instance.best_mass = vesselState.mass;
                } else {
                    Debug.Log("mass: " + vesselState.mass + "int: " + ascent.intermediate_altitude + " start_alt: " + ascent.start_altitude + " start_turn: " + ascent.start_turn + " maxQ: " + ascent.maxQlimit);
                }
                return MissionReset;
            }
            if (vesselState.thrustMaximum == 0) {
                /* ran out of gas */
                return MissionReset;
            }
            if (vessel.situation == Vessel.Situations.LANDED || vessel.situation == Vessel.Situations.SPLASHED || vessel.state == Vessel.State.DEAD) {
                /* boom */
                return MissionReset;
            }
            return MissionCircularize;
        }

        private StateFn MissionReset(bool stateChanged) {
            FlightDriver.RevertToLaunch();
            return MissionPrelaunch;
        }

        public override void OnCrash(EventReport data) {
            missionState = MissionReset;
        }

        public override void OnCrashSplashdown(EventReport data) {
            missionState = MissionReset;
        }
    }
}
