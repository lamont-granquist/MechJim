/* portions probably plagiarized from MJ */

namespace MechJim.Manager {
    public class ThrottleManager: ManagerBase {
        public double target { get; set; }

        public ThrottleManager(Core core): base(core) {
            enabled = true;
        }

        public override void OnDisable() {
            if (vessel)
                vessel.ctrlState.mainThrottle = 0.0f;
        }

        public override void OnDrive(FlightCtrlState c) {
            c.mainThrottle = (float)target;
        }
    }
}
