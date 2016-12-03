using System;
using UnityEngine;

namespace MechJim.Manager {

    [Enable(typeof(ThrottleManager), typeof(AttitudeManager), typeof(WarpManager))]
    public class NodeExecutor: ManagerBase {
        public NodeExecutor(Core core): base(core) { }
        public double tolerance = 0.1;
        public double leadTime = 3.0;
        private bool seeking;

        protected override void OnEnable() {
            seeking = true; /* get good lock before starting burn */
        }

        public override void OnFixedUpdate() {
            throttle.target = 0.0;

            if (vessel.patchedConicSolver.maneuverNodes.Count == 0) {
                Debug.Log("NodeExecutor: no maneuver node to execute");
                Disable();
                return;
            }

            ManeuverNode node = vessel.patchedConicSolver.maneuverNodes[0];
            double dVLeft = node.GetBurnVector(orbit).magnitude;

            if (dVLeft < tolerance && attitude.AngleFromTarget() > 5) {
                Debug.Log("NodeExecutor: done with node, removing it");
                node.RemoveSelf();
                Disable();
                return;
            }

            attitude.attitudeTo(Vector3d.forward, AttitudeReference.MANEUVER_NODE);
            double BurnUT = node.UT - BurnTime(dVLeft) / 2.0;

            if ( vesselState.time < ( BurnUT - 300 ) ) {
                /* way before the burn */
                if ( attitude.AngleFromTarget() < 1 && Math.Abs(vesselState.angularVelocity.x) < 0.001 && Math.Abs(vesselState.angularVelocity.z) < 0.001 )
                    warp.WarpToUT(BurnUT - leadTime);

            } else if ( vesselState.time < ( BurnUT - leadTime ) ) {
                /* before the burn */
                if ( attitude.AngleFromTarget() < 1 && Math.Abs(vesselState.angularVelocity.x) < 0.001 && Math.Abs(vesselState.angularVelocity.z) < 0.001 )
                    warp.WarpToUT(BurnUT - leadTime);
                if ( attitude.AngleFromTarget() > 5 )
                    warp.MinimumWarp();
            } else if ( vesselState.time < BurnUT ) {
                /* settling time */
                warp.MinimumWarp();
            } else {
                /* feeling the burn */
                warp.MinimumWarp();
                throttle.target = 0.0;

                if ( attitude.AngleFromTarget() > 5 ) {
                    seeking = true;
                } else if ( attitude.AngleFromTarget() < 1 || !seeking ) {
                    double thrustToMass = vesselState.thrustMaximum / vesselState.mass;
                    throttle.target = Utils.Clamp(dVLeft / thrustToMass / 2.0, 0.01, 1.0);
                    Debug.Log("throttle.target = " + throttle.target);
                    seeking = false;
                }
            }
        }

        /* FIXME: stage simulation */
        private double BurnTime(double dv) {
            double burntime = vesselState.mass * vesselState.totalVe / vesselState.thrustMaximum * ( 1 - Math.Exp(-dv / vesselState.totalVe) );
            if ( double.IsNaN(burntime) )
                return 0.0;
            return burntime;
        }
    }
}
