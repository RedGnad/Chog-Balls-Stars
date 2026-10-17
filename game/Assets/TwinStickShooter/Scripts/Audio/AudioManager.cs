namespace TwinStickShooter
{
  using Chog.Match;
  using UnityEngine;
  using UnityEngine.InputSystem;

  /// <summary>
  /// The sample's audio manager, kept for the sample's callers (GameplaySFX, SFXPlayer). Its sounds now
  /// go through the game's voices (Chog.Match.ChogSounds: a few audio sources taken in turn, a pitch that
  /// varies a little, a clip never stacked more than twice at once) instead of a new object per sound; the
  /// music of its arena is handed to ChogSounds, which plays it as the match's loop at the player's music
  /// volume. Its mute key mutes everything for the session.
  /// </summary>
  public class AudioManager : MonoBehaviour
  {
    public static AudioManager Instance;

    private Vector3 _audioListenerPos;
    public Vector3 AudioListenerPos => _audioListenerPos;

    private AudioSource _bgmAudioSource;
    private PlayerInput _playerInput;

    public bool IsMuted => ChogSounds.Muted;

    void Awake()
    {
      if (Instance == null)
      {
        Instance = this;
      }
      else
      {
        Destroy(gameObject);
        return;
      }

      _playerInput = FindFirstObjectByType<PlayerInput>();

      var listener = FindFirstObjectByType<AudioListener>();
      _audioListenerPos = listener != null ? listener.transform.position : Vector3.zero;

      // The arena's music is played by ChogSounds, looped and at the player's volume.
      _bgmAudioSource = GetComponent<AudioSource>();
      if (_bgmAudioSource != null)
      {
        _bgmAudioSource.playOnAwake = false;
        _bgmAudioSource.Stop();
        ChogSounds.OfferMatchMusic(_bgmAudioSource.clip);
      }
    }

    private void OnDestroy()
    {
      if (Instance == this)
      {
        Instance = null;
      }
    }

    private void Update()
    {
      if (_playerInput == null || _playerInput.actions == null)
      {
        return;
      }

      var mute = _playerInput.actions.FindAction("Mute");
      if (mute != null && mute.WasPressedThisFrame())
      {
        ChogSounds.Muted = !ChogSounds.Muted;
      }
    }

    public void Play(Sound sound)
    {
      if (sound != null && sound.Clip != null)
      {
        ChogSounds.PlayClip(sound.Clip, sound.Volume);
      }
    }
  }
}
