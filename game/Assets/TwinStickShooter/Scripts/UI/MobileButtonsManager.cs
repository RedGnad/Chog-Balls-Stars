using UnityEngine;
using System.Collections.Generic;
using Chog.Match;
using Chog.Screens;

public class MobileButtonsManager : MonoBehaviour
{
  public bool ShowOnEditor = false;
  private static readonly HashSet<MobileButtonsManager> VisibleLayouts = new HashSet<MobileButtonsManager>();
  public static bool TouchControlsVisible => VisibleLayouts.Count > 0;

  // The sticks show while the player uses a finger, not whenever the browser reports a touch
  // screen: Unity's web player reports one on a computer too (ControlModeWatcher).
  void Awake()
  {
    ControlModeWatcher.Changed += RefreshVisibility;
  }

  void OnEnable() => VisibleLayouts.Add(this);
  void OnDisable() => VisibleLayouts.Remove(this);

  void OnDestroy()
  {
    ControlModeWatcher.Changed -= RefreshVisibility;
    VisibleLayouts.Remove(this);
  }

  void Start()
  {
    RefreshVisibility();
  }

  void RefreshVisibility()
  {
    bool visible = Application.isEditor ? ShowOnEditor : ControlModeWatcher.Mode == ControlMode.Touch;
    gameObject.SetActive(visible);
  }
}
