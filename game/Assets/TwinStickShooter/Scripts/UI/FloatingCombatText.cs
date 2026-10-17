namespace TwinStickShooter
{
  using Chog.Match;
  using Quantum;
  using System.Collections.Generic;
  using UnityEngine;

  /// <summary>
  /// The damage numbers. Each pops (Chog.Match.ChogDamageNumbers: 0.6, 1.3, then 1 in a tenth of a
  /// second), takes the colour of who was hit (red for the local hero, orange for its team, a warm white for
  /// the other team) and a size that grows with the share of the target's health the blow took; blows on one
  /// target within a fifth of a second add up in one number. A blow raised by a predicted frame that the
  /// verified frames cancel is taken back from its number.
  /// </summary>
  public class FloatingCombatText :  QuantumSceneViewComponent<CustomViewContext>
  {
    [Header("References")] public Canvas Canvas;
    public RectTransform CanvasTransform;
    public FloatingCombatTextEntry EntryPrefab;

    // Above the feet of the target, where its number starts.
    private const float NumberHeight = 1.6f;

    private Queue<FloatingCombatTextEntry> _entryPool = new Queue<FloatingCombatTextEntry>();

    private sealed class Shown
    {
      public FloatingCombatTextEntry Entry;
      public float LastHit;
    }

    // The number each target shows now, and the blow behind each part of a number.
    private readonly Dictionary<EntityRef, Shown> _shown = new Dictionary<EntityRef, Shown>();
    private readonly Dictionary<EventKey, (FloatingCombatTextEntry Entry, EntityRef Target, float Damage)> _blows =
      new Dictionary<EventKey, (FloatingCombatTextEntry, EntityRef, float)>();
    private readonly List<EventKey> _settled = new List<EventKey>();

    private void Start()
    {
      SpawnEntries(16);
      QuantumEvent.Subscribe<EventCharacterDamaged>(this, OnCharacterDamaged);
      QuantumCallback.Subscribe<CallbackEventCanceled>(this, OnEventCanceled);
      QuantumCallback.Subscribe<CallbackEventConfirmed>(this, OnEventConfirmed);
    }

    private void OnDestroy()
    {
      QuantumEvent.UnsubscribeListener(this);
      QuantumCallback.UnsubscribeListener(this);
    }

    private unsafe void OnCharacterDamaged(EventCharacterDamaged eventData)
    {
      if (PredictedFrame.Exists(eventData.target) == false)
      {
        return;
      }

      var damage = eventData.damage.AsFloat;
      var now = Time.time;
      var characterPosition = PredictedFrame.Get<Transform2D>(eventData.target).Position.ToUnityVector3();
      var anchor = ScreenAnchor(characterPosition + Vector3.up * NumberHeight);

      FloatingCombatTextEntry entry;
      if (_shown.TryGetValue(eventData.target, out var shown) && shown.Entry != null && shown.Entry.Target == eventData.target
          && shown.Entry.enabled && ChogDamageNumbers.Merges(shown.LastHit, now))
      {
        // A blow right after another on the same target: one number that grows.
        entry = shown.Entry;
        entry.RectTransform.anchoredPosition = anchor;
        entry.Add(damage, Size(eventData.target, entry.Total + damage));
        shown.LastHit = now;
      }
      else
      {
        if (_entryPool.Count == 0)
        {
          SpawnEntries(16);
        }

        entry = _entryPool.Dequeue();
        entry.RectTransform.anchoredPosition = anchor;
        entry.Activate(damage, Vector2.up, Colour(eventData.target), Size(eventData.target, damage));
        entry.Target = eventData.target;
        if (shown == null)
        {
          shown = new Shown();
          _shown[eventData.target] = shown;
        }
        shown.Entry = entry;
        shown.LastHit = now;
      }

      _blows[new EventKey(eventData.Tick, eventData.Id, eventData.GetHashCode())] = (entry, eventData.target, damage);
    }

    // A blow that only a predicted frame saw: taken back from its number, which goes when nothing is left.
    private void OnEventCanceled(CallbackEventCanceled canceled)
    {
      if (!_blows.TryGetValue(canceled.EventKey, out var blow))
      {
        return;
      }

      _blows.Remove(canceled.EventKey);
      // The number may have gone and shown another target since.
      if (blow.Entry != null && blow.Entry.enabled && blow.Entry.Target == blow.Target)
      {
        blow.Entry.Add(-blow.Damage, blow.Entry.Size);
        if (blow.Entry.Total < 0.5f)
        {
          blow.Entry.Deactivate();
        }
      }
    }

    private void OnEventConfirmed(CallbackEventConfirmed confirmed)
    {
      _blows.Remove(confirmed.EventKey);
      // Blows whose number is long gone are forgotten even if no answer came.
      if (_blows.Count > 64)
      {
        _settled.Clear();
        foreach (var pair in _blows)
        {
          if (pair.Value.Entry == null || !pair.Value.Entry.enabled)
          {
            _settled.Add(pair.Key);
          }
        }
        foreach (var key in _settled)
        {
          _blows.Remove(key);
        }
      }
    }

    private Vector2 ScreenAnchor(Vector3 world)
    {
      Vector3 viewportPos = Canvas.worldCamera.WorldToViewportPoint(world);
      var canvasSize = CanvasTransform.sizeDelta;
      float width = canvasSize.x;
      float height = canvasSize.y;
      return new Vector2(Mathf.FloorToInt(width * viewportPos.x - width / 2), Mathf.FloorToInt(height * viewportPos.y - height / 2));
    }

    // Red on the local hero, orange on its team, a warm white on the other team.
    private Color Colour(EntityRef target)
    {
      var localEntity = ViewContext != null && ViewContext.LocalView != null ? ViewContext.LocalView.EntityRef : EntityRef.None;
      var localTeam = localEntity != EntityRef.None && PredictedFrame.TryGet<TeamInfo>(localEntity, out var mine) ? mine.Index : -1;
      var targetTeam = PredictedFrame.TryGet<TeamInfo>(target, out var theirs) ? theirs.Index : -1;
      return ChogDamageNumbers.Colour(ChogDamageNumbers.SideOf(target == localEntity, targetTeam, localTeam));
    }

    private float Size(EntityRef target, float damage)
    {
      return ChogDamageNumbers.Size(ChogHitFeedback.ShareOfHealth(PredictedFrame, target, damage));
    }

    private void SpawnEntries(int amount)
    {
      for (int i = 0; i < amount; i++)
      {
        var entry = Instantiate(EntryPrefab, transform);
        entry.onAnimationFinished += ReturnObjectToPool;
        entry.Deactivate();
      }
    }

    private void ReturnObjectToPool(FloatingCombatTextEntry entry)
    {
      entry.Target = EntityRef.None;
      _entryPool.Enqueue(entry);
    }
  }
}
