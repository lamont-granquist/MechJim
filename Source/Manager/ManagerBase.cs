using UnityEngine;
using System;
using System.Reflection;
using System.Collections.Generic;

namespace MechJim.Manager {
    public class ManagerBase {
        protected Core core;
        protected Vessel vessel { get { return core.vessel; } }
        protected Orbit orbit { get { return core.vessel.orbit; } }
        protected CelestialBody mainBody { get { return core.vessel.mainBody; } }
        protected VesselState vesselState { get { return core.vesselState; } }

        public bool enabled { get; private set; }
        public List<ManagerBase> registrants { get; private set; }

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
        protected AutoScience autoscience { get { return core.GetManager<AutoScience>(); } }

        public ManagerBase(Core core) {
            this.registrants = new List<ManagerBase>();
            this.core = core;
            enabled = false;
        }

        public virtual void Run() {
            Register(this);
        }

        public void Register(ManagerBase caller) {
            if (registrants.Count == 0)
                Enable();
            if (!registrants.Contains(caller))
                registrants.Add(caller);
        }

        public void UnRegister(ManagerBase caller) {
            if (registrants.Contains(caller))
                registrants.Remove(caller);
            if (registrants.Count == 0)
                Disable();
        }

        protected void Enable() {
            if (!enabled) {
                Debug.Log("Enabling " + this.GetType().Name);
                foreach( EnableAttribute attr in this.GetType().GetCustomAttributes(typeof(EnableAttribute), true) ) {
                    foreach(Type klass in attr.klasses ) {
                        core.GetManager(klass).Register(this);
                    }
                }
                OnEnable();
            }
            enabled = true;
        }

        protected void Disable() {
            if (enabled) {
                Debug.Log("Disabling " + this.GetType().Name);
                foreach( EnableAttribute attr in this.GetType().GetCustomAttributes(typeof(EnableAttribute), true) ) {
                    foreach(Type klass in attr.klasses ) {
                        core.GetManager(klass).UnRegister(this);
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

        public virtual void Start() {
            /* purposefully does not have "enable" checking */
            OnStart();
        }

        public virtual void OnStart() { }

        public virtual void Awake() {
            /* purposefully does not have "enable" checking */
            OnAwake();
        }

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

        public virtual void OnDestroy() {
            /* deliberately does not check enabled since this is more like a destructor */
            /* and this naming scheme just got goofy */
            OnOnDestroy();
        }

        public virtual void OnOnDestroy() { }
    }
}
