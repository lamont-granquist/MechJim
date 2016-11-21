using System;
using System.Text;
using UnityEngine;

namespace MechJim {
  [KSPAddon(KSPAddon.Startup.Flight, false)]
  public class MechJimCore : MonoBehaviour {
    public Window window;
    public Toolbar toolbar;

    /* constructor - use awake/start for initialization */
    public MechJimCore() {
    }

    /* entering scene */
    void Awake() {
      window = new Window(this);
      toolbar = new Toolbar(this);
      toolbar.Awake();
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
    }

    /* leaving scene */
    void OnDestroy() {
      window.OnDestroy();
    }
  }
}
