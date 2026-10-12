namespace TwinStickShooter
{
  using UnityEngine;
  using UnityEngine.InputSystem;

  public class AudioManager : MonoBehaviour
  {
    public static AudioManager Instance;

    private Vector3 _audioListenerPos;
    public Vector3 AudioListenerPos => _audioListenerPos;

    private AudioSource _bgmAudioSource;
    private PlayerInput _playerInput;

    private bool _isMuted;
    public bool IsMuted => _isMuted;

    void Awake()
    {
      if (Instance == null)
      {
        Instance = this;
      }
      else
      {
        Destroy(gameObject);
      }

      _playerInput = FindObjectOfType<PlayerInput>();

      _audioListenerPos = FindObjectOfType<AudioListener>().transform.position;

      _bgmAudioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
      var muteWasPressed = _playerInput.actions["Mute"].WasPressedThisFrame();
      if (muteWasPressed)
      {
        _isMuted = !_isMuted;

        if (_isMuted)
        {
          _bgmAudioSource.volume = 0;
        }
        else
        {
          _bgmAudioSource.volume = 1;
        }
      }
    }


    public void Play(Sound sound)
    {
      if (_isMuted)
      {
        return;
      }

      if (sound.Clip != null)
      {
        AudioSource.PlayClipAtPoint(sound.Clip, _audioListenerPos, sound.Volume);
      }
    }
  }
}