using UnityEngine;

namespace MechJim.Manager {
    public class ThrottleManager: ManagerBase {
        public double target { get; set; }

        public ThrottleManager(Core core): base(core) {
            target = 0.0;
        }

        protected override void OnDisable() {
            target = 0.0;
            if (vessel != null) {
                FlightInputHandler.state.mainThrottle = 0.0f;
            }
        }

        public override void OnDrive(FlightCtrlState c) {
            c.mainThrottle = (float)target;
        }
    }
}
