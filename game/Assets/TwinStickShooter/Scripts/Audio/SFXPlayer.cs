namespace TwinStickShooter
{
  using UnityEngine;

  public class SFXPlayer : MonoBehaviour
  {
    public Sound Sound;

    public void PlaySound()
    {
      if (AudioManager.Instance.IsMuted == false && Sound.Clip != null)
      {
        AudioSource.PlayClipAtPoint(Sound.Clip, AudioManager.Instance.AudioListenerPos, Sound.Volume);
      }
    }
  }
}