namespace TwinStickShooter
{
  using UnityEngine;

  public class SFXPlayer : MonoBehaviour
  {
    public Sound Sound;

    // Through the game's voices (Chog.Match.ChogSounds), not a new object per sound.
    public void PlaySound()
    {
      if (Sound != null && Sound.Clip != null)
      {
        Chog.Match.ChogSounds.PlayClip(Sound.Clip, Sound.Volume);
      }
    }
  }
}
