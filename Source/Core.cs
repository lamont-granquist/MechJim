using System;
using System.Text;
using UnityEngine;
using MechJim.Manager;
using System.Collections.Generic;
using System.Reflection;

namespace MechJim {
    [KSPAddon(KSPAddon.Startup.Flight, false)]
        public class Core : MonoBehaviour {
            public Window window;
            public Toolbar toolbar;
            public Vessel vessel;
            public SteeringManager steering { get { return (SteeringManager) GetManager<SteeringManager>(); } }
            public AttitudeManager attitude { get { return (AttitudeManager) GetManager<AttitudeManager>(); } }
            public ThrottleManager throttle { get { return (ThrottleManager) GetManager<ThrottleManager>(); } }
            public AscentManager ascent { get { return (AscentManager) GetManager<AscentManager>(); } }
            public WarpManager warp { get { return (WarpManager) GetManager<WarpManager>(); } }
            public NodeExecutor node { get { return (NodeExecutor) GetManager<NodeExecutor>(); } }
            public VesselState vesselState { get { return (VesselState) GetManager<VesselState>(); } }
            public AutoPanel autopanel { get { return (AutoPanel) GetManager<AutoPanel>(); } }
            public AutoStage autostage { get { return (AutoStage) GetManager<AutoStage>(); } }
            public AutoFairing autofairing { get { return (AutoFairing) GetManager<AutoFairing>(); } }
            public Mission mission { get { return (Mission) GetManager<Mission>(); } }

            /* constructor - prefer using awake()/start() */
            public Core() { }

            public List<Type> managerClasses = new List<Type>();
            public Dictionary<Type,ManagerBase> managerDict = new Dictionary<Type,ManagerBase>();

            private void LoadManagers() {
                managerClasses.Clear();
                managerDict.Clear();
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies()) {
                    foreach (var type in asm.GetTypes()) {
                        if (type.IsSubclassOf(typeof(ManagerBase))) {
                            ConstructorInfo constructorInfo = type.GetConstructor(new[] { typeof(Core) });
                            managerClasses.Add(type);
                            managerDict.Add(type, (ManagerBase)(constructorInfo.Invoke(new object[] { this })));
                        }
                    }
                }
            }

            public ManagerBase GetManager(Type t) {
                return managerDict[t];
            }

            public ManagerBase GetManager<T>() where T: ManagerBase {
                return managerDict[typeof(T)];
            }

            public object InvokeManager<T>(string method, object[] parameters) where T: ManagerBase {
                ManagerBase m = GetManager<T>();
                MethodInfo mi = typeof(T).GetMethod(method);
                return mi.Invoke(m, parameters);
            }

            /* entering scene */
            void Awake() {
                toolbar = Toolbar.Instance;
                toolbar.core = this;
                toolbar.Awake();
                window = new Window(this);

                LoadManagers();
                GetManager<VesselState>().Enable();
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
                vessel.OnFlyByWire -= Drive;
                vessel.OnFlyByWire += Drive;

                /* always force this before everyone else */
                GetManager<VesselState>().Enable();
                GetManager<VesselState>().FixedUpdate();

                mission.FixedUpdate();

                ascent.FixedUpdate();

                autopanel.FixedUpdate();
                autofairing.FixedUpdate();
                autostage.FixedUpdate();

                node.FixedUpdate();
            }

            /* leaving scene */
            void OnDestroy() {
                FlightGlobals.ActiveVessel.OnFlyByWire -= Drive;
                window.OnDestroy();
            }

            /* FlyByWire callback */
            private void Drive(FlightCtrlState s) {
                vessel = FlightGlobals.ActiveVessel;
                throttle.Drive(s);
                attitude.Drive(s); /* before steering */
                steering.Drive(s);
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
