namespace TwinStickShooter
{
  using System.Collections.Generic;
  using UnityEngine;
  using System;

  [Serializable]
  public struct ObjectColors
  {
    public GameObject Object;
    public ColorPalette Palette;
  }

  [Serializable]
  public struct ColorPalette
  {
    public Color Local;
    public Color Friendly;
    public Color Enemy;
  }

  [CreateAssetMenu(fileName = "Game Colors", menuName = "Top Down/Game Colors", order = 1)]
  public class GameColors : ScriptableObject
  {
    [SerializeField] private List<ObjectColors> _objectsColors;
    [NonSerialized] private Dictionary<long, ColorPalette> _objectsColorsDictionary;

    private void Init()
    {
      _objectsColorsDictionary = new Dictionary<long, ColorPalette>(_objectsColors.Count);
      for (int i = 0; i < _objectsColors.Count; i++)
      {
        ObjectColors objectColors = _objectsColors[i];

        ColorSetter colorSetter = objectColors.Object.GetComponent<ColorSetter>();
        long colorId = colorSetter.ColorId;

        _objectsColorsDictionary.Add(colorId, objectColors.Palette);
      }
    }

    public ColorPalette GetPalette(long colorId)
    {
      if (_objectsColorsDictionary == null)
      {
        Init();
      }

      if (_objectsColorsDictionary.TryGetValue(colorId, out var palette) == true)
      {
        return palette;
      }

      //Debug.Log($"[Top Down] Color palette for object with guid {colorId} was not found.");

      return default;
    }
  }
}