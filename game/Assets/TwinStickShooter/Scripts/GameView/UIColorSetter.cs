namespace TwinStickShooter
{
  using UnityEngine;
  using UnityEngine.UI;

  public class UIColorSetter : ColorSetter
  {
    [SerializeField] private Image _healthBarImage;
    [SerializeField] private long _colorID;

    public override long ColorId => _colorID;

    public override void SetLocal()
    {
      _healthBarImage.color = GetLocalColor(ColorId);
    }

    public override void SetRemote(bool isFriendly)
    {
      _healthBarImage.color = GetRemoteColor(ColorId, isFriendly);
    }
  }
}