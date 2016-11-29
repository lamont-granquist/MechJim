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
        public double targetAPtime { get; set; }
        public double low_pressure_cutoff { get; set; } /* kPa */

        public PIDLoop pitchPID = new PIDLoop(2, 0.4, 0, extraUnwind: true);

        public enum ThrottleState {
            LAUNCH,
            FINAL,
            EXIT,
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
                { ThrottleState.EXIT, ThrottleExit },
            };
            attitudeMapping = new Dictionary<AttitudeState, Func<AttitudeState>> {
                { AttitudeState.LAUNCH, AttitudeLaunch },
                { AttitudeState.INITIATE, AttitudeInitiate },
                { AttitudeState.SETTLE, AttitudeSettle },
                { AttitudeState.SURFACE, AttitudeSurface },
                { AttitudeState.PROGRADE, AttitudePrograde },
                { AttitudeState.EXIT, AttitudeExit },
            };
            target_altitude = 100000;
            intermediate_altitude = 45000;
            start_altitude = 100;
            start_speed = 100;
            start_turn = 10;
            targetAPtime = 40;
            low_pressure_cutoff = 1.0;
        }

        public override void OnDisable() {
            core.throttle.enabled = false;
            core.attitude.enabled = false;
            core.warp.enabled = false;
        }

        public override void OnEnable() {
            throttleState = ThrottleState.LAUNCH;
            attitudeState = AttitudeState.LAUNCH;
            core.throttle.enabled = true;
            core.attitude.enabled = true;
            core.warp.enabled = true;
        }

        public override void OnFixedUpdate() {
            ThrottleState lastThrottleState = throttleState;
            AttitudeState lastAttitudeState = attitudeState;

            throttleState = throttleMapping[throttleState]();
            if ( throttleState != lastThrottleState ) {
                throttleMapping[throttleState]();
            }
            attitudeState = attitudeMapping[attitudeState]();
            if ( attitudeState != lastAttitudeState ) {
                attitudeMapping[attitudeState]();
            }
        }

        /* fix timeToAp to be negative when we're past the apoapsis and falling */
        private double adjustedTimeToAp() {
            if ( orbit.timeToAp > orbit.timeToPe )
                return orbit.timeToAp - orbit.period;
            return orbit.timeToAp;
        }

        /* burn to raise and maintain Ap at intermediate altitude */
        private ThrottleState ThrottleLaunch() {
            if (orbit.ApA > intermediate_altitude) {
                core.warp.WarpAtPhysicsRate(4, true);
                core.throttle.target = 0.0;
                if ( adjustedTimeToAp() < targetAPtime )
                    return ThrottleState.FINAL;
            } else {
                core.warp.WarpAtPhysicsRate(0, true);
                core.throttle.target = 1.0;
            }
            return ThrottleState.LAUNCH;
        }

        /* burn to raise and maintain Ap at target altitude */
        private ThrottleState ThrottleFinal() {
            if (orbit.ApA < target_altitude) {
                core.warp.WarpAtPhysicsRate(0, true);
                core.throttle.target = 1.0;
            } else {
                core.warp.WarpAtPhysicsRate(4, true);
                core.throttle.target = 0.0;
                if (vessel.altitude > mainBody.RealMaxAtmosphereAltitude())
                    return ThrottleState.EXIT;
            }
            return ThrottleState.FINAL;
        }

        /* shut off throttle when we're done */
        private ThrottleState ThrottleExit() {
            core.warp.MinimumWarp();
            core.throttle.target = 0.0;
            return ThrottleState.EXIT;
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
            if (vesselState.rocketAoA < 1) {
                pitchPID.ResetI();
                return AttitudeState.SURFACE;
            }
            core.attitude.attitudeTo(90, 90 - start_turn, 0);
            return AttitudeState.SETTLE;
        }

        /* track surface vector (zero AoA) until Q drops off */
        private AttitudeState AttitudeSurface() {
            if (vessel.dynamicPressurekPa < low_pressure_cutoff)  /* FIXME: should really track maxQ and make sure we're below it */
                return AttitudeState.PROGRADE;
            core.attitude.attitudeTo(Vector3d.forward, AttitudeReference.SURFACE_VELOCITY);
            return AttitudeState.SURFACE;
        }

        /* track prograde out of the atmosphere */
        private AttitudeState AttitudePrograde() {
            if (vessel.altitude > mainBody.RealMaxAtmosphereAltitude() && orbit.ApA > target_altitude )
                return AttitudeState.EXIT;
            double pitch = pitchPID.Update(adjustedTimeToAp(), targetAPtime, 0, 30);
            core.attitude.attitudeTo(Quaternion.AngleAxis((float)pitch, Vector3.left) * Vector3d.forward, AttitudeReference.ORBIT);
            return AttitudeState.PROGRADE;
        }

        /* done once we're out of the atmosphere */
        private AttitudeState AttitudeExit() {
            enabled = false;
            return AttitudeState.EXIT;
        }
    }
}
