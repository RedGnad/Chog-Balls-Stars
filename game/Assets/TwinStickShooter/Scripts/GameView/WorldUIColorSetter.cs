namespace TwinStickShooter
{
  using System.Collections.Generic;
  using UnityEngine;

  public class WorldUIColorSetter : ColorSetter
  {
    [SerializeField] private List<SpriteRenderer> _spriteRenderers;
    [SerializeField] private long _colorID;

    public override long ColorId => _colorID;

    public override void SetLocal()
    {
      foreach (var renderer in _spriteRenderers)
      {
        renderer.color = GetLocalColor(ColorId);
      }
    }

    public override void SetRemote(bool isFriendly)
    {
      foreach (var renderer in _spriteRenderers)
      {
        renderer.color = GetRemoteColor(ColorId, isFriendly);
      }
    }
  }
}