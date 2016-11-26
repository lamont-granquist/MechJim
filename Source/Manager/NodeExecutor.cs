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
                core.attitude.enabled = false;
                enabled = false;
                return;
            }

            core.attitude.attitudeTo(Vector3d.forward, AttitudeReference.MANEUVER_NODE);

            double BurnUT = node.UT - BurnTime(dVLeft) / 2.0;

            if ( BurnUT > vesselState.time ) {
                /* before the burn */
                if ( core.attitude.AngleFromTarget() < 1 && Math.Abs(vesselState.angularVelocity.x) < 0.002 && Math.Abs(vesselState.angularVelocity.z) < 0.002 )
                    core.warp.WarpToUT(BurnUT - leadTime);
                else
                    core.warp.MinimumWarp();
            } else {
                /* feeling the burn */
                core.warp.MinimumWarp();
                if ( seeking ) {
                    if ( core.attitude.AngleFromTarget() < 1 ) {
                        seeking = false;
                        core.throttle.target = 1.0;
                    } else {
                        core.throttle.target = 0.0;
                    }
                } else {
                    if ( core.attitude.AngleFromTarget() < 5 ) {
                        core.throttle.target = 1.0;
                    } else {
                        seeking = true;
                        core.throttle.target = 0.0;
                    }
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
