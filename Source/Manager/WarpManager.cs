/* recommended mod: WarpEverywhere */
using UnityEngine;

namespace MechJim.Manager {
    public class WarpManager: ManagerBase {
        protected TimeWarp timewarp { get { return TimeWarp.fetch; } }

        private double warpToUT = 0.0;

        public WarpManager(Core core): base(core) {
            enabled = true;
        }

        public void WarpToUT(double UT) {
            if (UT != warpToUT) {
                timewarp.WarpTo(UT, timewarp.warpRates.Length);
                warpToUT = UT;
            }
        }

        private void SetTimeWarpRate(int rateIndex, bool instant) {
            warpToUT = 0.0;
            if (rateIndex != TimeWarp.CurrentRateIndex) {
                timewarp.Mode = TimeWarp.Modes.HIGH;
                timewarp.CancelAutoWarp();
                TimeWarp.SetRate(rateIndex, instant);
            }
        }

        public void MinimumWarp(bool instant = true) {
            SetTimeWarpRate(0, instant);
        }
    }
}
