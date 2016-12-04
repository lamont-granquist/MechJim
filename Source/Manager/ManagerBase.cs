using UnityEngine;
using System;
using System.Reflection;

namespace MechJim.Manager {
    public class ManagerBase {
        protected Core core;
        protected Vessel vessel { get { return core.vessel; } }
        protected Orbit orbit { get { return core.vessel.orbit; } }
        protected CelestialBody mainBody { get { return core.vessel.mainBody; } }
        protected VesselState vesselState { get { return core.vesselState; } }

        public bool enabled { get; private set; }

        protected T GetManager<T>() where T: ManagerBase { return core.GetManager<T>(); }
        protected ThrottleManager throttle { get { return core.GetManager<ThrottleManager>(); } }
        protected AttitudeManager attitude { get { return core.GetManager<AttitudeManager>(); } }
        protected AscentManager ascent { get { return core.GetManager<AscentManager>(); } }
        protected AutoFairing autofairing { get { return core.GetManager<AutoFairing>(); } }
        protected AutoPanel autopanel { get { return core.GetManager<AutoPanel>(); } }
        protected AutoStage autostage { get { return core.GetManager<AutoStage>(); } }
        protected NodeExecutor node { get { return core.GetManager<NodeExecutor>(); } }
        protected SteeringManager steering { get { return core.GetManager<SteeringManager>(); } }
        protected WarpManager warp { get { return core.GetManager<WarpManager>(); } }

        public ManagerBase(Core core) {
            this.core = core;
            enabled = false;
        }

        public void Enable() {
            if (!enabled) {
                Debug.Log("Enabling " + this.GetType().Name);
                foreach( EnableAttribute attr in this.GetType().GetCustomAttributes(typeof(EnableAttribute), true) ) {
                    foreach(Type klass in attr.klasses ) {
                        core.GetManager(klass).Enable();
                    }
                }
                OnEnable();
            }
            enabled = true;
        }

        public void Disable() {
            if (enabled) {
                Debug.Log("Disabling " + this.GetType().Name);
                foreach( EnableAttribute attr in this.GetType().GetCustomAttributes(typeof(EnableAttribute), true) ) {
                    foreach(Type klass in attr.klasses ) {
                        core.GetManager(klass).Disable();
                    }
                }
                OnDisable();
            }
            enabled = false;
        }

        protected virtual void OnEnable() { }

        protected virtual void OnDisable() { }

        public virtual void Drive(FlightCtrlState s) {
            if (enabled)
                OnDrive(s);
        }

        public virtual void OnDrive(FlightCtrlState s) { }

        public virtual void OnStart() { }

        public virtual void OnAwake() { }

        public virtual void FixedUpdate() {
            if (enabled)
                OnFixedUpdate();
        }

        public virtual void OnFixedUpdate() { }

        public virtual void Update() {
            if (enabled)
                OnUpdate();
        }

        public virtual void OnUpdate() { }

        public virtual void Crash(EventReport data) {
            if (enabled)
                OnCrash(data);
        }

        public virtual void OnCrash(EventReport data) { }

        public virtual void CrashSplashdown(EventReport data) {
            if (enabled)
                OnCrashSplashdown(data);
        }

        public virtual void OnCrashSplashdown(EventReport data) { }
    }
}
