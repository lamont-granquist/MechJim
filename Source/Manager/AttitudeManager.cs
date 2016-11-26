/* portions probably plagiarized from MJ */
using System;
using UnityEngine;
using MechJim.Extensions;

namespace MechJim.Manager {
    public enum AttitudeReference {
        INERTIAL,           // world coordinate system.
        ORBIT,              // forward = prograde, left = normal plus, up = radial plus
        ORBIT_HORIZONTAL,   // forward = surface projection of orbit velocity, up = surface normal
        SURFACE_NORTH,      // forward = north, left = west, up = surface normal
        SURFACE_VELOCITY,   // forward = surface frame vessel velocity, up = perpendicular component of surface normal
        TARGET,             // forward = toward target, up = perpendicular component of vessel heading
        RELATIVE_VELOCITY,  // forward = toward relative velocity direction, up = tbd
        TARGET_ORIENTATION, // forward = direction target is facing, up = target up
        MANEUVER_NODE,      // forward = next maneuver node direction, up = tbd
        SUN,                // forward = orbit velocity of the parent body orbiting the sun, up = radial plus of that orbit
        SURFACE_HORIZONTAL, // forward = surface velocity horizontal component, up = surface normal
    }

    public class AttitudeManager: ManagerBase {
        private Quaternion attitude;
        private AttitudeReference reference;

        public AttitudeManager(Core core) : base(core) { }

        public override void OnEnable() {
            core.steering.enabled = true;
        }

        public override void OnDisable() {
            core.steering.enabled = false;
        }

        public Quaternion attitudeGetReferenceRotation(AttitudeReference reference) {
            Vector3d fwd, up;

            switch (reference) {
                case AttitudeReference.INERTIAL:
                    return Quaternion.identity;
                case AttitudeReference.ORBIT:
                    return Quaternion.LookRotation(vessel.GetObtVelocity(), vessel.up);
                case AttitudeReference.ORBIT_HORIZONTAL:
                    return Quaternion.LookRotation(Vector3d.Exclude(vessel.up, vessel.GetObtVelocity()), vessel.up);
                case AttitudeReference.SURFACE_NORTH:
                    throw new Exception("SURFACE_NORTH unimplemented");
                case AttitudeReference.SURFACE_VELOCITY:
                    throw new Exception("SURFACE_VELOCITY unimplemented");
                case AttitudeReference.TARGET:
                    fwd = FlightGlobals.fetch.VesselTarget.GetTransform().position - vessel.GetTransform().position;
                    up = Vector3.Cross(fwd, vesselState.normalPlus);
                    return Quaternion.LookRotation(fwd, up);
                case AttitudeReference.RELATIVE_VELOCITY:
                    throw new Exception("RELATIVE_VELOCTY unimplemented");
                case AttitudeReference.TARGET_ORIENTATION:
                    throw new Exception("TARGET_ORIENTATION unimplemented");
                case AttitudeReference.MANEUVER_NODE:
                    fwd = vessel.patchedConicSolver.maneuverNodes[0].GetBurnVector(orbit);
                    up = Vector3.Cross(fwd, vesselState.normalPlus);
                    return Quaternion.LookRotation(fwd, up);
                case AttitudeReference.SUN:
                    throw new Exception("SUN unimplemented");
                case AttitudeReference.SURFACE_HORIZONTAL:
                    throw new Exception("SURFACE_HORIZONTAL unimplemented");
                default:
                    return Quaternion.identity;
            }
        }

        public Vector3d attitudeWorldToReference(Vector3d vector, AttitudeReference reference)
        {
            return Quaternion.Inverse(attitudeGetReferenceRotation(reference)) * vector;
        }

        public Vector3d attitudeReferenceToWorld(Vector3d vector, AttitudeReference reference)
        {
            return attitudeGetReferenceRotation(reference) * vector;
        }

        public void attitudeTo(Quaternion attitude, AttitudeReference reference) {
            enabled = true;
            this.attitude = attitude;
            this.reference = reference;
        }

        public void attitudeTo(Vector3d direction, AttitudeReference reference) {
            Vector3d up;
            up = attitudeWorldToReference(core.vessel.ReferenceTransform.rotation * Quaternion.Euler(-90, 0, 0) * Vector3d.up, reference);
            attitudeTo(Quaternion.LookRotation(direction, up), reference);
        }

        public void attitudeTo(double heading, double pitch, double roll) {
            Quaternion attitude = Quaternion.AngleAxis((float)heading, Vector3.up) * Quaternion.AngleAxis(-(float)pitch, Vector3.right) * Quaternion.AngleAxis(-(float)roll, Vector3.forward);
            attitudeTo(attitude, AttitudeReference.SURFACE_NORTH);
        }

        public override void OnDrive(FlightCtrlState c) {
            if (FlightGlobals.fetch.VesselTarget == null && (reference == AttitudeReference.TARGET || reference == AttitudeReference.TARGET_ORIENTATION || reference == AttitudeReference.RELATIVE_VELOCITY)) {
                enabled = false;
                return;
            }

            if ((reference == AttitudeReference.MANEUVER_NODE) && (vessel.patchedConicSolver.maneuverNodes.Count == 0)) {
                enabled = false;
                return;
            }

            core.steering.target = attitudeGetReferenceRotation(reference) * attitude;
        }

        /*
         * helper routines
         */

        // angle in degrees between the vessel's current pointing direction and the attitude target, ignoring roll
        public double AngleFromTarget() {
            return enabled ? Math.Abs(Vector3d.Angle(attitudeGetReferenceRotation(reference) * attitude * Vector3d.forward, vessel.forward())) : 0;
        }

    }
}
