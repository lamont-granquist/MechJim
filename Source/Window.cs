using System;
using System.Text;
using UnityEngine;
using System.Collections.Generic;

namespace MechJim {
  public class Window {
    private PopupDialog window;
    private MechJimCore core;

    private double PeR;
    private double ApR;

    public Window(MechJimCore core) {
      this.core = core;
    }

    /* starting */
    public void Start() {
      GameEvents.onGamePause.Add(onPause);
      GameEvents.onGameUnpause.Add(onUnpause);
    }

    /* leaving scene */
    public void OnDestroy() {
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

    public void SpawnWindow() {
      List<DialogGUIBase> dialog = new List<DialogGUIBase>();

      dialog.Add(new DialogGUIHorizontalLayout(new DialogGUIBase[] {
              new DialogGUITextInput("ApR", false, 10, s => { ApR = Convert.ToDouble(s); return Convert.ToString(ApR); }),
              new DialogGUITextInput("PeR", false, 10, s => { PeR = Convert.ToDouble(s); return Convert.ToString(PeR); }),
              new DialogGUIButton("Ellipticize", Ellipticize),
            }));
      dialog.Add(new DialogGUIButton("Dismiss", core.toolbar.SetFalse, true));

      window = PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
          new MultiOptionDialog("foo", "bar", UISkinManager.defaultSkin, new Rect(0.5f, 0.5f, 250, 120), dialog.ToArray()),
          false, UISkinManager.defaultSkin, false, "");
    }

    void Circularize() {
      var maneuver = new Maneuver.Circularize(FlightGlobals.ActiveVessel, FlightGlobals.ActiveVessel.orbit, Planetarium.GetUniversalTime());
      maneuver.PlaceManeuverNode();
    }

    void Ellipticize() {
      var maneuver = new Maneuver.Ellipticize(FlightGlobals.ActiveVessel, FlightGlobals.ActiveVessel.orbit, Planetarium.GetUniversalTime(), PeR, ApR);
      maneuver.PlaceManeuverNode();
    }

    public void DismissWindow() {
      if (window != null)
        window.Dismiss();
    }
  }
}
