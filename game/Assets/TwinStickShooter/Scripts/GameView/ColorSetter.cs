namespace TwinStickShooter
{
  using UnityEngine;

  public abstract class ColorSetter : MonoBehaviour
  {
    public abstract void SetLocal();
    public abstract void SetRemote(bool isFriendly);

    public abstract long ColorId { get; }

    protected Color GetLocalColor(long colorId)
    {
      return GameSettings.Instance.GameColors.GetPalette(colorId).Local;
    }

    protected Color GetRemoteColor(long viewGuid, bool isFriendly)
    {
      return isFriendly == true
        ? GameSettings.Instance.GameColors.GetPalette(viewGuid).Friendly
        : GameSettings.Instance.GameColors.GetPalette(viewGuid).Enemy;
    }
  }
}