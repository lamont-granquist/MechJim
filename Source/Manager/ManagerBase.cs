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
    }
}
