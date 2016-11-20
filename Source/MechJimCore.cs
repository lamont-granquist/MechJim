using System;
using System.Text;
using UnityEngine;

namespace MechJim {
  [KSPAddon(KSPAddon.Startup.Flight, false)]
  public class MechJimCore : MonoBehaviour {
    private static PopupDialog window;

    /* constructor - use awake/start for initialization */
    public MechJimCore() {
    }

    /* entering scene */
    void Awake() {
      Toolbar.Instance.Awake();
    }

    /* starting */
    void Start() {
      GameEvents.onGamePause.Add(onPause);
      GameEvents.onGameUnpause.Add(onUnpause);
    }

    /* every frame */
    void Update() {
    }

    /* every physics step */
    void FixedUpdate() {
    }

    /* leaving scene */
    void OnDestroy() {
      GameEvents.onGamePause.Remove(onPause);
      GameEvents.onGameUnpause.Remove(onUnpause);
    }

    private void onPause() {
      if (window != null)
        window.gameObject.SetActive(false);
    }

    private void onUnpause() {
      if (window != null)
        window.gameObject.SetActive(true);
    }

    public static void spawnWindow() {
      window = PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "Title", "Text", "OK", false, HighLogic.UISkin);
    }

    public static void dismissWindow() {
      if (window != null)
        window.Dismiss();
    }
  }
}
