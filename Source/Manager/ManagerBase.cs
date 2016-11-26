namespace MechJim.Manager {
    public class ManagerBase {
        protected Core core;
        protected Vessel vessel { get { return core.vessel; } }
        protected Orbit orbit { get { return core.vessel.orbit; } }
        protected CelestialBody mainBody { get { return core.vessel.mainBody; } }
        protected VesselState vesselState { get { return core.vesselState; } }

        private bool _enabled;
        public bool enabled {
            get { return _enabled; }
            set {
                if (value != _enabled) {
                    _enabled = value;
                    if (_enabled) {
                        OnEnable();
                    } else {
                        OnDisable();
                    }
                }
            }
        }

        public ManagerBase(Core core) {
            this.core = core;
            enabled = false;
        }

        public virtual void OnEnable() { }

        public virtual void OnDisable() { }

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
