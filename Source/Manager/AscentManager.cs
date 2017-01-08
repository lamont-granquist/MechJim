using System;
using System.Collections.Generic;
using MechJim.Extensions;
using UnityEngine;

/* this is a stock launcher, not suitable for RSS/RO/RF */
namespace MechJim.Manager {

    [Enable(typeof(ThrottleManager), typeof(AttitudeManager), typeof(WarpManager), typeof(AutoFairing))]
    public class AscentManager: ManagerBase {
        public bool done { get; set; }
        public double target_altitude { get; set; }
        public double intermediate_altitude { get; set; }
        public double start_speed { get; set; }
        public double start_altitude { get; set; }
        public double start_turn { get; set; }
        public double maxQlimit { get; set; } /* kPa */

        public PIDLoop pitchPID = new PIDLoop(2, 0, 0.3);
        public PIDLoop throttlePID = new PIDLoop(.8, 0.15, 0.1);

        override protected void OnDisable() {
        }

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

        private double targetAPtime { get; set; }

        private ThrottleState throttleState;
        private AttitudeState attitudeState;

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
            intermediate_altitude = 55000;
            start_altitude = 50;
            start_speed = 50;
            start_turn = 18;
            maxQlimit = 40;
            done = false;
        }


        protected override void OnEnable() {
            throttleState = ThrottleState.LAUNCH;
            attitudeState = AttitudeState.LAUNCH;
            done = false;
        }

        public override void OnFixedUpdate() {
            /* update targetAPtime */
            double dV = mainBody.CircVelocityAtRadius(orbit.ApR) - orbit.SwappedVelocityAtApoapsis().magnitude;
            targetAPtime = BurnTime(dV/2);

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

        /* FIXME: BAD COPYPASTA ALERT! */
        private double BurnTime(double dv) {
            double burntime = vesselState.mass * vesselState.totalVe / vesselState.thrustMaximum * ( 1 - Math.Exp(-dv / vesselState.totalVe) );
            if ( double.IsNaN(burntime) )
                return 0.0;
            return burntime;
        }

        /*
         * Throttle State Machine
         */

        /* burn to raise and maintain Ap at intermediate altitude */
        private ThrottleState ThrottleLaunch() {
            if (orbit.ApA > intermediate_altitude) {
                warp.WarpAtPhysicsRate(this, 4, true);
                throttle.target = 0.0;
                if ( adjustedTimeToAp() < targetAPtime )
                    return ThrottleState.FINAL;
            } else {
                warp.WarpAtPhysicsRate(this, 0, true);
                if (vessel.dynamicPressurekPa < maxQlimit)
                    throttlePID.ResetI();
                throttle.target = throttlePID.Update(vessel.dynamicPressurekPa, maxQlimit, 0, 1);
            }
            return ThrottleState.LAUNCH;
        }

        /* burn to raise and maintain Ap at target altitude */
        private ThrottleState ThrottleFinal() {
            if (orbit.ApA < target_altitude) {
                warp.WarpAtPhysicsRate(this, 0, true);
                if (orbit.ApA > target_altitude * 0.95)
                    throttle.target = 0.1;
                else
                    throttle.target = 1.0;
            } else {
                warp.WarpAtPhysicsRate(this, 4, true);
                throttle.target = 0.0;
                if (vessel.altitude > mainBody.RealMaxAtmosphereAltitude())
                    return ThrottleState.EXIT;
            }
            return ThrottleState.FINAL;
        }

        /* shut off throttle when we're done */
        private ThrottleState ThrottleExit() {
            warp.MinimumWarp(this);
            throttle.target = 0.0;
            done = true;
            return ThrottleState.EXIT;
        }

        /*
         * Attitude State Machine
         */

        /* straight up */
        private AttitudeState AttitudeLaunch() {
            if ((vessel.srf_velocity.magnitude > start_speed) && (vessel.altitude > vessel.terrainAltitude + start_altitude))
                return AttitudeState.INITIATE;
            attitude.attitudeTo(90, 90, 0);
            return AttitudeState.LAUNCH;
        }

        /* initiate gravity turn */
        private AttitudeState AttitudeInitiate() {
            if (vesselState.rocketAoA > 2)
                return AttitudeState.SETTLE;
            attitude.attitudeTo(90, 90 - start_turn, 0);
            return AttitudeState.INITIATE;
        }

        /* wait for settling */
        private AttitudeState AttitudeSettle() {
            if (vesselState.rocketAoA < 1) {
                return AttitudeState.SURFACE;
            }
            attitude.attitudeTo(90, 90 - start_turn, 0);
            return AttitudeState.SETTLE;
        }

        /* track surface vector (zero AoA) until Q drops off */
        private AttitudeState AttitudeSurface() {
            if ((orbit.ApA > 0.95 * intermediate_altitude) && ( adjustedTimeToAp() < targetAPtime + 5 ))
                return AttitudeState.PROGRADE;
            attitude.attitudeTo(Vector3d.forward, AttitudeReference.SURFACE_VELOCITY);
            return AttitudeState.SURFACE;
        }

        /* track prograde out of the atmosphere */
        private AttitudeState AttitudePrograde() {
            if (vessel.altitude > mainBody.RealMaxAtmosphereAltitude() && orbit.ApA > target_altitude )
                return AttitudeState.EXIT;
            double pitch = pitchPID.Update(adjustedTimeToAp(), 0, 0, 30);
            attitude.attitudeTo(Quaternion.AngleAxis((float)pitch, Vector3.left) * Vector3d.forward, AttitudeReference.SURFACE_HORIZONTAL);
            return AttitudeState.PROGRADE;
        }

        /* done once we're out of the atmosphere */
        private AttitudeState AttitudeExit() {
            done = true;
            return AttitudeState.EXIT;
        }
    }
}
