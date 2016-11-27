using System;
using System.Collections.Generic;
using MechJim.Extensions;
using UnityEngine;

/* this is a stock launcher, not suitable for RSS/RO/RF */

namespace MechJim.Manager {
    public class AscentManager: ManagerBase {
        public double target_altitude { get; set; }
        public double intermediate_altitude { get; set; }
        public double start_speed { get; set; }
        public double start_altitude { get; set; }
        public double start_turn { get; set; }

        public enum ThrottleState {
            LAUNCH,
            FINAL,
        }

        public enum AttitudeState {
            LAUNCH,
            INITIATE,
            SETTLE,
            SURFACE,
            FLAT,
            PROGRADE,
            EXIT,
        }

        public ThrottleState throttleState;
        public AttitudeState attitudeState;

        private IDictionary<ThrottleState, Func<ThrottleState>> throttleMapping;
        private IDictionary<AttitudeState, Func<AttitudeState>> attitudeMapping;

        public AscentManager(Core core): base(core) {
            throttleMapping = new Dictionary<ThrottleState, Func<ThrottleState>> {
                { ThrottleState.LAUNCH, ThrottleLaunch },
                { ThrottleState.FINAL, ThrottleFinal },
            };
            attitudeMapping = new Dictionary<AttitudeState, Func<AttitudeState>> {
                { AttitudeState.LAUNCH, AttitudeLaunch },
                { AttitudeState.INITIATE, AttitudeInitiate },
                { AttitudeState.SETTLE, AttitudeSettle },
                { AttitudeState.SURFACE, AttitudeSurface },
                { AttitudeState.FLAT, AttitudeFlat },
                { AttitudeState.PROGRADE, AttitudePrograde },
                { AttitudeState.EXIT, AttitudeExit },
            };
            target_altitude = 100000;
            intermediate_altitude = 45000;
            start_altitude = 100;
            start_speed = 50;
            start_turn = 19;
        }

        public override void OnDisable() {
            core.throttle.target = 0.0;
            core.throttle.enabled = false;
            core.attitude.enabled = false;
        }

        public override void OnEnable() {
            throttleState = ThrottleState.LAUNCH;
            attitudeState = AttitudeState.LAUNCH;
            core.throttle.enabled = true;
            core.attitude.enabled = true;
        }

        public override void OnFixedUpdate() {
            ThrottleState lastThrottleState = throttleState;
            AttitudeState lastAttitudeState = attitudeState;

            throttleState = throttleMapping[throttleState]();
            if ( throttleState != lastThrottleState ) {
                Debug.Log("throttleState changed to " + throttleState);
                throttleMapping[throttleState]();
            }
            attitudeState = attitudeMapping[attitudeState]();
            if ( attitudeState != lastAttitudeState ) {
                Debug.Log("attitudeState changed to " + attitudeState);
                attitudeMapping[attitudeState]();
            }
        }

        /* burn to raise and maintain Ap at intermediate altitude */
        private ThrottleState ThrottleLaunch() {
            if (orbit.ApA > intermediate_altitude) {
                core.throttle.target = 0.0;
                if ( orbit.timeToAp < 20 )
                    return ThrottleState.FINAL;
            } else {
                core.throttle.target = 1.0;
            }
            return ThrottleState.LAUNCH;
        }

        /* burn to raise and maintain Ap at target altitude */
        private ThrottleState ThrottleFinal() {
            if (orbit.ApA < target_altitude)
                core.throttle.target = 1.0;
            else
                core.throttle.target = 0.0;
            return ThrottleState.FINAL;
        }

        /* straight up */
        private AttitudeState AttitudeLaunch() {
            if ((vessel.srf_velocity.magnitude > start_speed) && (vessel.altitude > vessel.terrainAltitude + start_altitude))
                return AttitudeState.INITIATE;
            core.attitude.attitudeTo(90, 90, 0);
            return AttitudeState.LAUNCH;
        }

        /* initiate gravity turn */
        private AttitudeState AttitudeInitiate() {
            if (vesselState.rocketAoA > 2)
                return AttitudeState.SETTLE;
            core.attitude.attitudeTo(90, 90 - start_turn, 0);
            return AttitudeState.INITIATE;
        }

        /* wait for settling */
        private AttitudeState AttitudeSettle() {
            if (vesselState.rocketAoA < 1)
                return AttitudeState.SURFACE;
            core.attitude.attitudeTo(90, 90 - start_turn, 0);
            return AttitudeState.SETTLE;
        }

        /* track surface vector (zero AoA) to near intermediate alt */
        private AttitudeState AttitudeSurface() {
            if (vessel.dynamicPressurekPa < 0.1)  /* FIXME: track maxQ */
                return AttitudeState.FLAT;
            core.attitude.attitudeTo(Vector3d.forward, AttitudeReference.SURFACE_VELOCITY);
            return AttitudeState.SURFACE;
        }

        /* burn flat once Q is low near intermediate alt to reduce gravity losses */
        private AttitudeState AttitudeFlat() {
            if (orbit.ApA > 1.1 * intermediate_altitude)
                return AttitudeState.PROGRADE;
            core.attitude.attitudeTo(90, 0, 0);
            return AttitudeState.FLAT;
        }

        /* track prograde out of the atmosphere */
        private AttitudeState AttitudePrograde() {
            if (vessel.altitude > mainBody.RealMaxAtmosphereAltitude())
                return AttitudeState.EXIT;
            core.attitude.attitudeTo(Vector3d.forward, AttitudeReference.ORBIT);
            return AttitudeState.PROGRADE;
        }

        /* done once we're out of the atmosphere */
        private AttitudeState AttitudeExit() {
            enabled = false;
            return AttitudeState.EXIT;
        }
    }
}
