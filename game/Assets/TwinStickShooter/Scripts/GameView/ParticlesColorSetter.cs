namespace TwinStickShooter
{
  using System.Collections.Generic;
  using UnityEngine;

  public class ParticlesColorSetter : ColorSetter
  {
    [SerializeField] private List<ParticleSystem> _particleSystems;
    [SerializeField] private long _colorID;

    public override long ColorId => _colorID;

    public override void SetLocal()
    {
      foreach (var particles in _particleSystems)
      {
        var main = particles.main;
        main.startColor = GetLocalColor(ColorId);
      }
    }

    public override void SetRemote(bool isFriendly)
    {
      foreach (var particles in _particleSystems)
      {
        var main = particles.main;
        main.startColor = GetRemoteColor(ColorId, isFriendly);
      }
    }
  }
}