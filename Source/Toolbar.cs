using KSP.UI.Screens;
using UnityEngine;

namespace MechJim
{

  public class Toolbar
  {
    private ApplicationLauncherButton btnLauncher;
    private MechJimCore core;

    public Toolbar(MechJimCore core) {
      this.core = core;
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
            ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.MAPVIEW,
            GameDatabase.Instance.GetTexture("MechJim/Icons/ToolbarIcon", false)
            );
      }
    }

    public void RemoveButton(GameScenes scene) {
      ApplicationLauncher.Instance.RemoveModApplication(btnLauncher);
    }

    private void ButtonTrue() {
      core.window.SpawnWindow();
    }

    private void ButtonFalse() {
      core.window.DismissWindow();
    }

    public void SetFalse() {
      btnLauncher.SetFalse(false);
    }

    public void SetTrue() {
      btnLauncher.SetTrue(false);
    }
  }
}
