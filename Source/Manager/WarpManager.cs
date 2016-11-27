/* recommended mod: WarpEverywhere */
using UnityEngine;
using MechJim.Extensions;

namespace MechJim.Manager {
    public class WarpManager: ManagerBase {
        protected TimeWarp timewarp { get { return TimeWarp.fetch; } }

        private double warpToUT = 0.0;

        public WarpManager(Core core): base(core) { }

        public override void OnDisable() {
            MinimumWarp();
        }

        public void WarpToUT(double UT) {
            if (UT != warpToUT) {
                timewarp.WarpTo(UT, timewarp.warpRates.Length);
                warpToUT = UT;
            }

            if (!vessel.LandedOrSplashed && vessel.altitude < timewarp.GetAltitudeLimit(1, mainBody))
                SetRegularMode();
            else
                SetPhysicsMode();

            double desiredRate = UT - vesselState.time + Time.fixedDeltaTime * TimeWarp.CurrentRateIndex;
            desiredRate = Utils.Clamp(desiredRate, 1, maxRate());

            WarpAtRate(desiredRate);
        }

        public void WarpAtRegularRate(double rate, bool instant = false) {
            SetRegularMode();

            WarpAtRate(rate, instant);
        }

        public void WarpAtPhysicsRate(double rate, bool instant = false) {
            SetPhysicsMode();

            WarpAtRate(rate, instant);
        }

        public void MinimumWarp(bool instant = true) {
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
                if ( timewarp.physicsWarpRates[i] < rate && timewarp.physicsWarpRates[i] > r ) {
                    rate = timewarp.physicsWarpRates[i];
                    index = i;
                }
            }
            return index;
        }

        private int IndexForRegularRate(double rate) {
            double r = 0.0;
            int index = 0;
            for(int i = 0; i < timewarp.warpRates.Length; i++) {
                if ( timewarp.warpRates[i] < rate && timewarp.warpRates[i] > r ) {
                    rate = timewarp.warpRates[i];
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
            warpToUT = 0.0;
            if (rateIndex != TimeWarp.CurrentRateIndex) {
                timewarp.CancelAutoWarp();
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
