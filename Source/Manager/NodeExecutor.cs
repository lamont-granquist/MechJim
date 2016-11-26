using System;
using UnityEngine;

namespace MechJim.Manager {
    public class NodeExecutor: ManagerBase {
        public NodeExecutor(Core core): base(core) { }
        public double tolerance = 0.1;
        public double leadTime = 3.0;
        private bool seeking;

        public override void OnEnable() {
            seeking = true;
        }

        public override void OnDisable() {
            core.throttle.target = 0.0;
            core.attitude.enabled = false;
        }

        public override void OnFixedUpdate() {
            core.throttle.target = 0.0;

            if (vessel.patchedConicSolver.maneuverNodes.Count == 0) {
                enabled = false;
                return;
            }

            ManeuverNode node = vessel.patchedConicSolver.maneuverNodes[0];
            double dVLeft = node.GetBurnVector(orbit).magnitude;

            if (dVLeft < tolerance && core.attitude.AngleFromTarget() > 5) {
                node.RemoveSelf();
                enabled = false;
                return;
            }

            core.attitude.attitudeTo(Vector3d.forward, AttitudeReference.MANEUVER_NODE);

            double BurnUT = node.UT - BurnTime(dVLeft) / 2.0;

            if ( vesselState.time < ( BurnUT - 300 ) ) {
                /* way before the burn */
                if ( core.attitude.AngleFromTarget() < 1 && Math.Abs(vesselState.angularVelocity.x) < 0.001 && Math.Abs(vesselState.angularVelocity.z) < 0.001 )
                    core.warp.WarpToUT(BurnUT - leadTime);

            } else if ( BurnUT > vesselState.time ) {
                /* before the burn */
                if ( core.attitude.AngleFromTarget() < 1 && Math.Abs(vesselState.angularVelocity.x) < 0.001 && Math.Abs(vesselState.angularVelocity.z) < 0.001 )
                    core.warp.WarpToUT(BurnUT - leadTime);
                if ( core.attitude.AngleFromTarget() > 5 )
                    core.warp.MinimumWarp();
            } else {
                /* feeling the burn */
                core.warp.MinimumWarp();
                core.throttle.target = 0.0;

                if ( core.attitude.AngleFromTarget() > 5 ) {
                    seeking = true;
                } else if ( core.attitude.AngleFromTarget() < 1 || !seeking ) {
                    double thrustToMass = vesselState.thrustMaximum / vesselState.mass;
                    core.throttle.target = Utils.Clamp(dVLeft / thrustToMass / 2.0, 0.01, 1.0);
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
