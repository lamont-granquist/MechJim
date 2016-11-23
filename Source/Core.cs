using System;
using System.Text;
using UnityEngine;

namespace MechJim {
    [KSPAddon(KSPAddon.Startup.Flight, false)]
        public class Core : MonoBehaviour {
            public Window window;
            public Toolbar toolbar;
            public Vessel vessel;
            public SteeringManager steeringManager;

            /* constructor - prefer using awake()/start() */
            public Core() {
            }

            /* entering scene */
            void Awake() {
                toolbar = Toolbar.Instance;
                toolbar.core = this;
                toolbar.Awake();
                window = new Window(this);
                steeringManager = new SteeringManager(this);
            }

            /* starting */
            void Start() {
                window.Start();
            }

            /* every frame */
            void Update() {
            }

            /* every physics step */
            void FixedUpdate() {
                vessel = FlightGlobals.ActiveVessel;
                vessel.OnFlyByWire -= OnFlyByWire;
                vessel.OnFlyByWire += OnFlyByWire;
            }

            /* leaving scene */
            void OnDestroy() {
                FlightGlobals.ActiveVessel.OnFlyByWire -= OnFlyByWire;
                window.OnDestroy();
            }

            /* FlyByWire callback */
            private void OnFlyByWire(FlightCtrlState s) {
                steeringManager.OnFlyByWire(s);
                CheckFlightCtrlState(s);
            }

            private static void CheckFlightCtrlState(FlightCtrlState s)
            {
                if (float.IsNaN(s.mainThrottle)) s.mainThrottle = 0;
                if (float.IsNaN(s.yaw)) s.yaw = 0;
                if (float.IsNaN(s.pitch)) s.pitch = 0;
                if (float.IsNaN(s.roll)) s.roll = 0;
                if (float.IsNaN(s.X)) s.X = 0;
                if (float.IsNaN(s.Y)) s.Y = 0;
                if (float.IsNaN(s.Z)) s.Z = 0;

                s.mainThrottle = Mathf.Clamp01(s.mainThrottle);
                s.yaw = Mathf.Clamp(s.yaw, -1, 1);
                s.pitch = Mathf.Clamp(s.pitch, -1, 1);
                s.roll = Mathf.Clamp(s.roll, -1, 1);
                s.X = Mathf.Clamp(s.X, -1, 1);
                s.Y = Mathf.Clamp(s.Y, -1, 1);
                s.Z = Mathf.Clamp(s.Z, -1, 1);
            }
        }
}
