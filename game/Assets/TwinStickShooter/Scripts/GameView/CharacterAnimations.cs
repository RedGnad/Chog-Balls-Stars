namespace TwinStickShooter
{
  using Quantum;
  using UnityEngine;

  public class CharacterAnimations : QuantumEntityViewComponent<CustomViewContext>
  {
    private Animator _animator;
    // Moves the body by code when its parts are known; the clips are then left unused.
    private Chog.Match.ChogBodyAnimator _body;
    private bool _movedByCode;

    // A Genesis can change body after this view woke up (the Chog body takes the place of the
    // class model): the mover of the new body is then looked up again.
    private Chog.Match.ChogBodyAnimator Body
    {
      get
      {
        if (_movedByCode && _body == null)
        {
          _body = GetComponentInChildren<Chog.Match.ChogBodyAnimator>();
        }
        return _body;
      }
    }

    private int _speedBuffer = 0;

    private void Awake()
    {
      _animator = GetComponentInChildren<Animator>();
      _body = Chog.Match.ChogBodyAnimator.Attach(transform);
      _movedByCode = _body != null;
      if (_movedByCode && _animator != null)
      {
        _animator.enabled = false;
      }
    }

    public override void OnActivate(Frame frame)
    {
      base.OnActivate(frame);
      QuantumEvent.Subscribe<EventCharacterSkill>(this, OnCharacterSkill);
      QuantumEvent.Subscribe<EventGameOver>(this, OnGameOver);
    }

    private void OnCharacterSkill(EventCharacterSkill ev)
    {
      if (ev.character == EntityRef)
      {
        if (_movedByCode)
        {
          if (Body != null) Body.Attack();
        }
        else
        {
          _animator.SetTrigger("Attack");
        }
      }
    }

    private void OnGameOver(EventGameOver ev)
    {
      bool isWinner = ev.Game.Frames.Verified.Get<TeamInfo>(EntityRef).Index == ev.WinnerTeam;

      if (_movedByCode)
      {
        if (Body == null) return;
        if (isWinner == true)
        {
          Body.Cheer();
        }
        else
        {
          Body.Defeated();
        }
      }
      else if (isWinner == true)
      {
        _animator.SetTrigger("Cheer");
      }
      else
      {
        _animator.SetTrigger("Defeated");
      }
    }

    public override void OnUpdateView()
    {
      if (PredictedFrame.TryGet<KCC>(EntityRef, out KCC kcc))
      {
        float speed = kcc.Velocity.Magnitude.AsFloat;

        if (_movedByCode)
        {
          if (Body != null) Body.Speed = speed;
        }
        else if (speed == 0)
        {
          _speedBuffer++;
          if (_speedBuffer > 1)
          {
            _animator.SetBool("StopRun", true);
            _animator.SetFloat("Speed", 0);
          }
          else
          {
            _animator.SetFloat("Speed", 10);
          }
        }
        else
        {
          _speedBuffer = 0;
          _animator.SetBool("StopRun", false);
          _animator.SetFloat("Speed", speed);
        }
      }
    }
  }
}