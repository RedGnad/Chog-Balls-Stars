using UnityEngine;

[System.Serializable]
public class Sound
{
  public AudioClip Clip;

  [Range(0f, 1f)]
  public float Volume = 1;
}
