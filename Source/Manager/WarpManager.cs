/* recommended mod: WarpEverywhere */
using UnityEngine;
using MechJim.Extensions;

namespace MechJim.Manager {
    public class WarpManager: ManagerBase {
        protected TimeWarp timewarp { get { return TimeWarp.fetch; } }

        private double warpToUT = -1;

        public WarpManager(Core core): base(core) { }

        protected override void OnDisable() {
            MinimumWarp();
        }

        public override void OnFixedUpdate() {
            if(warpToUT > 0)
                WarpToUT(warpToUT);
        }

        public void WarpToUT(double UT) {
            warpToUT = UT;

            if (!vessel.LandedOrSplashed && vessel.altitude > timewarp.GetAltitudeLimit(1, mainBody))
                SetRegularMode();
            else
                SetPhysicsMode();

            double desiredRate = UT - vesselState.time + Time.fixedDeltaTime * TimeWarp.CurrentRateIndex;
            desiredRate = Utils.Clamp(desiredRate, 1, maxRate());

            WarpAtRate(desiredRate);
        }

        public void WarpAtRegularRate(double rate, bool instant = false) {
            warpToUT = -1;

            SetRegularMode();

            WarpAtRate(rate, instant);
        }

        public void WarpAtPhysicsRate(double rate, bool instant = false) {
            warpToUT = -1;

            SetPhysicsMode();

            WarpAtRate(rate, instant);
        }

        public void MinimumWarp(bool instant = true) {
            warpToUT = -1;

            WarpAtIndex(0, instant);
        }

        private double maxRate() {
            if (timewarp.Mode == TimeWarp.Modes.HIGH)
                return timewarp.warpRates[timewarp.warpRates.Length - 1];
            else
                return timewarp.physicsWarpRates[timewarp.physicsWarpRates.Length - 1];
        }

        private int IndexForPhysicsRate(double rate) {
            double r = 0.0;
            int index = 0;
            for(int i = 0; i < timewarp.physicsWarpRates.Length; i++) {
                if ( timewarp.physicsWarpRates[i] <= rate && timewarp.physicsWarpRates[i] > r ) {
                    r = timewarp.physicsWarpRates[i];
                    index = i;
                }
            }
            return index;
        }

        private int IndexForRegularRate(double rate) {
            double r = 0.0;
            int index = 0;
            for(int i = 0; i < timewarp.warpRates.Length; i++) {
                if ( timewarp.warpRates[i] <= rate && timewarp.warpRates[i] > r ) {
                    r = timewarp.warpRates[i];
                    index = i;
                }
            }
            return index;
        }

        private void WarpAtRate(double rate, bool instant = false) {
            int index;

            if (timewarp.Mode == TimeWarp.Modes.HIGH)
                index = IndexForRegularRate(rate);
            else
                index = IndexForPhysicsRate(rate);

            WarpAtIndex(index, instant);
        }

        private void WarpAtIndex(int rateIndex, bool instant = false) {
            if (rateIndex != TimeWarp.CurrentRateIndex) {
                TimeWarp.SetRate(rateIndex, instant);
            }
        }

        // returns false if the warp switches -- also returns false if it can't switch
        private bool SetRegularMode() {
            if (TimeWarp.WarpMode != TimeWarp.Modes.HIGH) {
                double instantAltitudeASL = (vessel.CoMD - mainBody.position).magnitude - mainBody.Radius;
                if (instantAltitudeASL > mainBody.RealMaxAtmosphereAltitude()) {
                    timewarp.Mode = TimeWarp.Modes.HIGH;
                    WarpAtIndex(0, true);
                }
                return false;
            }
            return true;
        }

        // returns false if the warp switches
        private bool SetPhysicsMode() {
            if (TimeWarp.WarpMode != TimeWarp.Modes.LOW) {
                timewarp.Mode = TimeWarp.Modes.LOW;
                WarpAtIndex(0, true);
                return false;
            }
            return true;
        }
    }
}
