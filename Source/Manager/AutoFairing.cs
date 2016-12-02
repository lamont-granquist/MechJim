using UnityEngine;
using KSP.UI.Screens;
using MechJim.Extensions;

namespace MechJim.Manager {
    public class AutoFairing: ManagerBase {
        double max_pressure = 5.0;   /* kPA */
        double min_altitude = 40000; /* m */

        public AutoFairing(Core core): base(core) { }

        public void DeployFairings() {
            for (int i = 0; i < vessel.parts.Count; i++) {
                Part p = vessel.parts[i];
                if (p.IsFairing()) {
                    p.DeployFairing();
                    Debug.Log("called EjectFairing");
                }
            }
        }

        public override void OnFixedUpdate() {
            if (vessel.dynamicPressurekPa < max_pressure && vessel.altitude > min_altitude) {
                Debug.Log("Triggering Fairings");
                DeployFairings();
                Disable();
            }
        }
    }
}
