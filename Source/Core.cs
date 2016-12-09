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
            public VesselState vesselState { get { return (VesselState) GetManager<VesselState>(); } }

            /* constructor - prefer using awake()/start() */
            public Core() {
                Debug.Log("allocated new Core() object");
            }

            public List<Type> managerClasses = new List<Type>();
            public List<Type> sortedManagerClasses = new List<Type>();
            public Dictionary<Type,ManagerBase> managerDict = new Dictionary<Type,ManagerBase>();

            private void LoadManagers() {
                managerClasses.Clear();
                managerDict.Clear();
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies()) {
                    foreach (var type in asm.GetTypes()) {
                        if (type.IsSubclassOf(typeof(ManagerBase))) {
                            if (!managerDict.ContainsKey(type)) {
                                ConstructorInfo constructorInfo = type.GetConstructor(new[] { typeof(Core) });
                                managerClasses.Add(type);
                                managerDict.Add(type, (ManagerBase)(constructorInfo.Invoke(new object[] { this })));
                            }
                        }
                    }
                }
                sortedManagerClasses.Add(typeof(VesselState));
                while (sortedManagerClasses.Count < managerClasses.Count) {
                    for (int i=0; i<managerClasses.Count; i++) {
                        Type klass = managerClasses[i];
                        if (sortedManagerClasses.Contains(klass))
                            continue;
                        sortedManagerClasses.Add(klass);
                    }
                }
            }

            public ManagerBase GetManager(Type t) {
                return managerDict[t];
            }

            public T GetManager<T>() where T: ManagerBase {
                return (T)managerDict[typeof(T)];
            }

            public object InvokeManager<T>(string method, object[] parameters) where T: ManagerBase {
                ManagerBase m = GetManager<T>();
                MethodInfo mi = typeof(T).GetMethod(method);
                return mi.Invoke(m, parameters);
            }

            /* entering scene */
            void Awake() {
                GameEvents.onCrash.Add(Crash);
                GameEvents.onCrashSplashdown.Add(CrashSplashdown);

                toolbar = Toolbar.Instance;
                toolbar.core = this;
                toolbar.Awake();
                window = new Window(this);

                LoadManagers();

                for(int i=0; i<sortedManagerClasses.Count; i++) {
                    managerDict[sortedManagerClasses[i]].Awake();
                }
            }

            /* starting */
            void Start() {
                window.Start();

                for(int i=0; i<sortedManagerClasses.Count; i++) {
                    managerDict[sortedManagerClasses[i]].Start();
                }
            }

            /* every frame */
            void Update() {
                for(int i=0; i<sortedManagerClasses.Count; i++) {
                    managerDict[sortedManagerClasses[i]].Update();
                }
            }

            /* every physics step */
            void FixedUpdate() {
                vessel = FlightGlobals.ActiveVessel;
                vessel.OnFlyByWire -= Drive;
                vessel.OnFlyByWire += Drive;

                for(int i=0; i<sortedManagerClasses.Count; i++) {
                    managerDict[sortedManagerClasses[i]].FixedUpdate();
                }
            }

            /* leaving scene */
            void OnDestroy() {
                for(int i=0; i<sortedManagerClasses.Count; i++) {
                    managerDict[sortedManagerClasses[i]].OnDestroy();
                }

                FlightGlobals.ActiveVessel.OnFlyByWire -= Drive;
                window.OnDestroy();
            }

            /* FlyByWire callback */
            private void Drive(FlightCtrlState s) {
                vessel = FlightGlobals.ActiveVessel;
                for(int i=0; i<sortedManagerClasses.Count; i++) {
                    managerDict[sortedManagerClasses[i]].Drive(s);
                }
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

            void Crash(EventReport data) {
                for(int i=0; i<sortedManagerClasses.Count; i++) {
                    managerDict[sortedManagerClasses[i]].Crash(data);
                }
            }

            void CrashSplashdown(EventReport data) {
                for(int i=0; i<sortedManagerClasses.Count; i++) {
                    managerDict[sortedManagerClasses[i]].CrashSplashdown(data);
                }
            }
        }
}
