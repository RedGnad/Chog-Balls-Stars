using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class MobileButtonsManager : MonoBehaviour
{
  public bool ShowOnEditor = false;
  private static readonly HashSet<MobileButtonsManager> VisibleLayouts = new HashSet<MobileButtonsManager>();
  public static bool TouchControlsVisible => VisibleLayouts.Count > 0;

  void Awake()
  {
    InputSystem.onDeviceChange += OnDeviceChange;
  }

  void OnEnable() => VisibleLayouts.Add(this);
  void OnDisable() => VisibleLayouts.Remove(this);

  void OnDestroy()
  {
    InputSystem.onDeviceChange -= OnDeviceChange;
    VisibleLayouts.Remove(this);
  }

  void Start()
  {
    RefreshVisibility();
  }

  void OnDeviceChange(InputDevice device, InputDeviceChange change)
  {
    if (device is Touchscreen) RefreshVisibility();
  }

  void RefreshVisibility()
  {
    bool visible = Application.isEditor ? ShowOnEditor :
      Application.isMobilePlatform || Touchscreen.current != null;
    gameObject.SetActive(visible);
  }
}
