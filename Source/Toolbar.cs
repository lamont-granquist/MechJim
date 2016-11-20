using KSP.UI.Screens;
using UnityEngine;

namespace MechJim
{

  public class Toolbar
  {
    private static ApplicationLauncherButton btnLauncher;

    /* singleton */
    private static readonly Toolbar instance = new Toolbar();
    static Toolbar() {
    }
    private Toolbar() {
    }
    public static Toolbar Instance {
      get {
        return instance;
      }
    }

    /* entering scene */
    public void Awake() {
      GameEvents.onGUIApplicationLauncherReady.Add(AddButton);
      GameEvents.onGUIApplicationLauncherUnreadifying.Add(RemoveButton);
    }

    public void AddButton() {
      if (btnLauncher == null) {
        btnLauncher = ApplicationLauncher.Instance.AddModApplication(
            ButtonPressed, ButtonReleased,
            null, null, null, null,
            ApplicationLauncher.AppScenes.FLIGHT,
            GameDatabase.Instance.GetTexture("MechJim/Icons/ToolbarIcon", false)
            );
      }
    }

    public void RemoveButton(GameScenes scene) {
      ApplicationLauncher.Instance.RemoveModApplication(btnLauncher);
    }

    private void ButtonPressed() {
      MechJimCore.spawnWindow();
    }

    private void ButtonReleased() {
      MechJimCore.dismissWindow();
    }

    public static void setBtnState(bool state, bool click = false) {
      if (state)
        btnLauncher.SetTrue(click);
      else
        btnLauncher.SetFalse(click);
    }
  }
}
