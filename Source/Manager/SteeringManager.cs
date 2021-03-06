/* portion plagiarized from kOS */
using System;
using UnityEngine;
using MechJim.Extensions;

namespace MechJim.Manager {
    public class SteeringManager: ManagerBase {
        /* target = Quaternion.LookRotation(vessel.GetObtVelocity().normalized, vessel.up); */
        public Quaternion target { get; set; }

        public double MaxStoppingTime { get; set; }

        private double rollControlRange;
        public double RollControlRange {
            get { return this.rollControlRange; }
            set { this.rollControlRange = Math.Max(EPSILON, Math.Min(Math.PI, value)); }
        }

        public TorquePI pitchPI = new TorquePI();
        public TorquePI yawPI = new TorquePI();
        public TorquePI rollPI = new TorquePI();

        public PIDLoop pitchRatePI = new PIDLoop(1, 0.1, 0, extraUnwind: true);
        public PIDLoop yawRatePI = new PIDLoop(1, 0.1, 0, extraUnwind: true);
        public PIDLoop rollRatePI = new PIDLoop(1, 0.1, 0, extraUnwind: true);

        private Vector3d Actuation = Vector3d.zero;
        private Vector3d TargetTorque = Vector3d.zero;
        private Vector3d Omega = Vector3d.zero;

        /* error */
        private double phiTotal;
        /* error in pitch, roll, yaw */
        private Vector3d phiVector = Vector3d.zero;
        private Vector3d TargetOmega = Vector3d.zero;

        /* max angular rotation */
        private Vector3d MaxOmega = Vector3d.zero;

        private Vector3d ControlTorque { get { return vesselState.torqueAvailable; } }

        public SteeringManager(Core core): base(core) {
            MaxStoppingTime = 2;
            RollControlRange = 5 * Mathf.Deg2Rad;
        }

        private const double EPSILON = 1e-16;

        public override void OnDrive(FlightCtrlState c) {
            if (vessel.ActionGroups[KSPActionGroup.SAS]) {
                /* SAS seems to be busted in 1.2.1? */
                vessel.ActionGroups.SetGroup(KSPActionGroup.SAS, false);
            }
            UpdatePredictionPI();
            UpdateControl(c);
        }

        /* temporary state vectors */
        private Quaternion vesselRotation;
        private Vector3d vesselForward;
        private Vector3d vesselTop;
        private Vector3d vesselStarboard;
        private Vector3d targetForward;
        private Vector3d targetTop;
        /* private Vector3d targetStarboard; */

        private void UpdateStateVectors() {
            /* FIXME: may get called more than once per tick */
            vesselRotation = vessel.ReferenceTransform.rotation * Quaternion.Euler(-90, 0, 0);
            vesselForward = vesselRotation * Vector3d.forward;
            vesselTop = vesselRotation * Vector3d.up;
            vesselStarboard = vesselRotation * Vector3d.right;

            targetForward = target * Vector3d.forward;
            targetTop = target * Vector3d.up;
            /* targetStarboard = target * Vector3d.right; */

            Omega = - vessel.angularVelocity;
        }

        public double PhiTotal() {
            UpdateStateVectors();

            double PhiTotal = Vector3d.Angle(vesselForward, targetForward) * Mathf.Deg2Rad;
            if (Vector3d.Angle(vesselTop, targetForward) > 90)
                PhiTotal *= -1;

            return PhiTotal;
        }

        public Vector3d PhiVector() {
            Vector3d Phi = Vector3d.zero;

            Phi[0] = Vector3d.Angle(vesselForward, Vector3d.Exclude(vesselStarboard, targetForward)) * Mathf.Deg2Rad;
            if (Vector3d.Angle(vesselTop, Vector3d.Exclude(vesselStarboard, targetForward)) > 90)
                Phi[0] *= -1;
            Phi[1] = Vector3d.Angle(vesselTop, Vector3d.Exclude(vesselForward, targetTop)) * Mathf.Deg2Rad;
            if (Vector3d.Angle(vesselStarboard, Vector3d.Exclude(vesselForward, targetTop)) > 90)
                Phi[1] *= -1;
            Phi[2] = Vector3d.Angle(vesselForward, Vector3d.Exclude(vesselTop, targetForward)) * Mathf.Deg2Rad;
            if (Vector3d.Angle(vesselStarboard, Vector3d.Exclude(vesselTop, targetForward)) > 90)
                Phi[2] *= -1;

            return Phi;
        }

        private void UpdatePredictionPI() {
            phiTotal = PhiTotal();

            phiVector = PhiVector();

            for(int i = 0; i < 3; i++) {
                MaxOmega[i] = ControlTorque[i] * MaxStoppingTime / vessel.MOI[i];
            }

            TargetOmega[0] = pitchRatePI.Update(-phiVector[0], 0, MaxOmega[0]);
            TargetOmega[1] = rollRatePI.Update(-phiVector[1], 0, MaxOmega[1]);
            TargetOmega[2] = yawRatePI.Update(-phiVector[2], 0, MaxOmega[2]);

            if (Math.Abs(phiTotal) > RollControlRange) {
                TargetOmega[1] = 0;
                rollRatePI.ResetI();
            }

            TargetTorque[0] = pitchPI.Update(Omega[0], TargetOmega[0], vessel.MOI[0], ControlTorque[0]);
            TargetTorque[1] = rollPI.Update(Omega[1], TargetOmega[1], vessel.MOI[1], ControlTorque[1]);
            TargetTorque[2] = yawPI.Update(Omega[2], TargetOmega[2], vessel.MOI[2], ControlTorque[2]);
        }

        public void Reset() {
            pitchPI.ResetI();
            yawPI.ResetI();
            rollPI.ResetI();
            pitchRatePI.ResetI();
            yawRatePI.ResetI();
            rollRatePI.ResetI();
        }

        private void UpdateControl(FlightCtrlState c) {
            /* TODO: static engine torque and/or differential throttle */

            for(int i = 0; i < 3; i++) {
                double clamp = Math.Max(Math.Abs(Actuation[i]), 0.005) * 2;
                Actuation[i] = TargetTorque[i] / ControlTorque[i];
                if (Math.Abs(Actuation[i]) < EPSILON)
                    Actuation[i] = 0;
                Actuation[i] = Math.Max(Math.Min(Actuation[i], clamp), -clamp);
            }


            c.pitch = (float)Actuation.x;
            c.roll  = (float)Actuation.y;
            c.yaw   = (float)Actuation.z;
        }

        public class TorquePI
        {
            public PIDLoop Loop { get; set; }

            public double I { get; private set; }

            public MovingAverage TorqueAdjust { get; set; }

            private double tr;

            public double Tr {
                get { return tr; }
                set {
                    tr = value;
                    ts = 4.0 * tr / 2.76;
                }
            }

            private double ts;

            public double Ts {
                get { return ts; }
                set {
                    ts = value;
                    tr = 2.76 * ts / 4.0;
                }
            }

            public TorquePI() {
                Loop = new PIDLoop();
                Ts = 2;  /* FIXME: use high pass filter to measure output noise and decrease this value when no noise, and increase when it oscillates */
                TorqueAdjust = new MovingAverage();
            }

            public double Update(double input, double setpoint, double MomentOfInertia, double maxOutput)
            {
                I = MomentOfInertia;

                Loop.Ki = MomentOfInertia * Math.Pow(4.0 / ts, 2);
                Loop.Kp = 2 * Math.Pow(MomentOfInertia * Loop.Ki, 0.5);
                return Loop.Update(input, setpoint, maxOutput);
            }

            public void ResetI() { Loop.ResetI(); }
        }
    }
}
