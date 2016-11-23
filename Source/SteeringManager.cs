/* plagiarized from kOS */
using System;
using UnityEngine;

namespace MechJim {
    public class SteeringManager {
        private Core core { get; set; }
        private Vessel vessel { get; set; }

        private Quaternion target { get; set; }

        private TorquePI pitchPI = new TorquePI();
        private TorquePI yawPI = new TorquePI();
        private TorquePI rollPI = new TorquePI();

        private PIDLoop pitchRatePI = new PIDLoop(1, 0.1, 0, extraUnwind: true);
        private PIDLoop yawRatePI = new PIDLoop(1, 0.1, 0, extraUnwind: true);
        private PIDLoop rollRatePI = new PIDLoop(1, 0.1, 0, extraUnwind: true);

        private Vector3d Actuation = Vector3d.zero;
        private Vector3d TargetTorque = Vector3d.zero;
        private Vector3d ControlTorque = Vector3d.zero;

        public SteeringManager(Core core) {
        }

        private const double EPSILON = 1e-16;

        private void ResetIs() {
            pitchPI.ResetI();
            yawPI.ResetI();
            rollPI.ResetI();
            pitchRatePI.ResetI();
            yawRatePI.ResetI();
            rollRatePI.ResetI();
        }

        public void OnFlyByWire(FlightCtrlState c) {
            vessel = core.vessel;
            Update(c);
        }

        private void Update(FlightCtrlState c) {
            /* fixed to prograde for now */
            target = Quaternion.LookRotation(vessel.GetObtVelocity().normalized, vessel.up);
            if (vessel.ActionGroups[KSPActionGroup.SAS]) {
                UpdateControl(c);
            } else {
                UpdatePredictionPI();
                UpdateControl(c);
            }
        }

        private void UpdatePredictionPI() {
            var vesselForward = vessel.ReferenceTransform.rotation * Vector3d.forward;
            var vesselTop = vessel.ReferenceTransform.rotation * Vector3d.up;
            var vesselRight = vessel.ReferenceTransform.rotation * Vector3d.right;
            /* FIXME */
        }

        private void UpdateControl(FlightCtrlState c) {
            Debug.Log("in UpdateControl");
            if (vessel.ActionGroups[KSPActionGroup.SAS]) {
                pitchPI.ResetI();
                yawPI.ResetI();
                rollPI.ResetI();
                pitchRatePI.ResetI();
                yawRatePI.ResetI();
                rollRatePI.ResetI();
                Debug.Log("locking SAS to target");
                vessel.Autopilot.SAS.LockRotation(target);
            } else {
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
                Ts = 2;
                TorqueAdjust = new MovingAverage();
            }

            public double Update(double sampleTime, double input, double setpoint, double momentOfInertia, double maxOutput)
            {
                I = momentOfInertia;

                Loop.Ki = momentOfInertia * Math.Pow(4.0 / ts, 2);
                Loop.Kp = 2 * Math.Pow(momentOfInertia * Loop.Ki, 0.5);
                return Loop.Update(sampleTime, input, setpoint, maxOutput);
            }

            public void ResetI() { Loop.ResetI(); }
        }
    }
}
