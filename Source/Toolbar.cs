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
            ButtonTrue, ButtonFalse,
            null, null, null, null,
            ApplicationLauncher.AppScenes.FLIGHT,
            GameDatabase.Instance.GetTexture("MechJim/Icons/ToolbarIcon", false)
            );
      }
    }

    public void RemoveButton(GameScenes scene) {
      ApplicationLauncher.Instance.RemoveModApplication(btnLauncher);
    }

    private void ButtonTrue() {
      MechJimCore.spawnWindow();
    }

    private void ButtonFalse() {
      MechJimCore.dismissWindow();
    }

    public static void SetFalse() {
      btnLauncher.SetFalse(false);
    }

    public static void SetTrue() {
      btnLauncher.SetTrue(false);
    }
  }
}
