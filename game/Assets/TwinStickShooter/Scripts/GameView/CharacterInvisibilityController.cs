namespace TwinStickShooter
{
  using Quantum;
  using Photon.Deterministic;

  public unsafe class CharacterInvisibilityController : QuantumCallbacks
  {
    public static System.Action<bool, EntityRef> OnCharacterVisibilityChange;

    private CharacterView _localPlayerView;

    private void Start()
    {
      CharacterView.OnLocalPlayerInstantiated += OnLocalPlayerInstantiated;
    }

    protected override void OnDisable()
    {
      base.OnDisable();
      CharacterView.OnLocalPlayerInstantiated -= OnLocalPlayerInstantiated;
    }

    private void OnLocalPlayerInstantiated(CharacterView playerView)
    {
      if (_localPlayerView == null)
      {
        _localPlayerView = playerView;
      }
    }

    public override void OnUpdateView(QuantumGame game)
    {
      Frame frame = game.Frames.Verified;
      if (_localPlayerView == null || frame.Exists(_localPlayerView.EntityRef) == false)
      {
        return;
      }

      TeamInfo teamInfo = frame.Get<TeamInfo>(_localPlayerView.EntityRef);
      CheckInvisibleEnemies(frame, teamInfo);
    }

    private void CheckInvisibleEnemies(Frame frame, TeamInfo teamInfo)
    {
      var charactersFilter = frame.Filter<Transform2D, Invisibility, TeamInfo>();
      while (charactersFilter.NextUnsafe(out var currentCharacter, out var currentTransform, out var invisibility,
               out var currentTeamInfo))
      {
        if (teamInfo.Index == currentTeamInfo->Index)
        {
          continue;
        }

        if (invisibility->IsInvisible &&
            HasCloseTeammateInvisible(frame, currentTransform->Position, teamInfo) == false)
        {
          OnCharacterVisibilityChange?.Invoke(false, currentCharacter);
        }
        else
        {
          OnCharacterVisibilityChange?.Invoke(true, currentCharacter);
        }
      }
    }

    private bool HasCloseTeammateInvisible(Frame frame, FPVector2 enemyPosition, TeamInfo localCharacterTeamInfo)
    {
      var charactersFilter = frame.Filter<Transform2D, Invisibility, TeamInfo>();
      while (charactersFilter.NextUnsafe(out var currentCharacter, out var currentTransform, out var invisibility,
               out var currentTeamInfo))
      {
        if (localCharacterTeamInfo.Index != currentTeamInfo->Index)
        {
          continue;
        }

        if (invisibility->IsInvisible == false && invisibility->ExposureTimer <= 0)
        {
          continue;
        }

        if (FPVector2.Distance(currentTransform->Position, enemyPosition) < 5)
        {
          return true;
        }
      }

      return false;
    }
  }
}