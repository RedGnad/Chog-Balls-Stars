namespace TwinStickShooter
{
  using Chog.Match;
  using TMPro;
  using UnityEngine;

  public class FloatingCombatTextEntry : MonoBehaviour
  {
    [Header("References")] public TMP_Text TextElement;
    public RectTransform RectTransform;

    [Header("Configurations")] public AnimationCurve AlphaCurve;
    public AnimationCurve XOffsetCurve;
    public AnimationCurve ZOffsetCurve;
    public AnimationCurve ScaleCurve;

    public float AnimationTime;
    public Vector2 Offset;
    public Vector2 OffsetVariance;
    public Vector2 OffsetVarianceScale = new Vector2(10.0f, 10.0f);

    public System.Action<FloatingCombatTextEntry> onAnimationFinished;

    /// <summary>Whom this number is about while it shows.</summary>
    public Quantum.EntityRef Target;

    /// <summary>The damage this number shows, all merged blows together.</summary>
    public float Total { get; private set; }

    /// <summary>Its size, from the share of the target's health (ChogDamageNumbers.Size).</summary>
    public float Size { get; private set; } = 1f;

    private float _timer = -1;
    // The time since its last pop: a new number pops from 0.6, a merged blow from the peak of the pop.
    private float _pop;
    private Vector2 _targetOffset;

    public void Activate(int damage, Vector2 directionHint)
    {
      Activate(damage, directionHint, ChogDamageNumbers.EnemyColour, 1f);
    }

    /// <summary>Shows a number of that colour and size, popping (ChogDamageNumbers.Pop).</summary>
    public void Activate(float damage, Vector2 directionHint, Color colour, float size)
    {
      // The numbers of the game's look (Chog.Screens.ChogTheme), in the colour of who was hit.
      Chog.Screens.ChogTheme.AsNumber(TextElement);
      TextElement.color = colour;
      Total = damage;
      Size = size;
      TextElement.text = Shown(Total);
      TextElement.enabled = true;
      enabled = true;

      TextElement.rectTransform.anchoredPosition = Vector3.zero;
      TextElement.alpha = 1;
      _timer = 0;
      _pop = 0;

      if (directionHint.x > 0.0f)
        _targetOffset.x = Random.Range(0.0f, OffsetVariance.x) * OffsetVarianceScale.x + Offset.x;
      else
        _targetOffset.x = Random.Range(-OffsetVariance.x, 0.0f) * OffsetVarianceScale.x - Offset.x;

      _targetOffset.y = Random.Range(-OffsetVariance.y, OffsetVariance.y) * OffsetVarianceScale.y + Offset.y;
      Animate();
    }

    /// <summary>Another blow on the same target (or one taken back, below 0): the number grows and pops again.</summary>
    public void Add(float damage, float size)
    {
      Total += damage;
      Size = Mathf.Max(Size, size);
      TextElement.text = Shown(Total);
      TextElement.alpha = 1;
      _timer = 0;
      _pop = ChogDamageNumbers.PopSeconds * 0.5f;
      Animate();
    }

    public void Deactivate()
    {
      _timer = -1;
      Total = 0f;
      TextElement.enabled = false;
      enabled = false;

      if (onAnimationFinished != null)
        onAnimationFinished(this);
    }

    private void Update()
    {
      _timer += Time.deltaTime;
      _pop += Time.deltaTime;
      Animate();

      if (_timer > AnimationTime)
        Deactivate();
    }

    private void Animate()
    {
      float t = AnimationTime > 0f ? Mathf.Clamp01(_timer / AnimationTime) : 1f;

      TextElement.alpha = AlphaCurve.Evaluate(t);

      var position = _targetOffset;
      position.x *= XOffsetCurve.Evaluate(t);
      position.y *= ZOffsetCurve.Evaluate(t);
      TextElement.rectTransform.anchoredPosition = position;

      // The pop of its first tenth of a second, its size, then the sample's fade-out.
      var scale = ScaleCurve.Evaluate(t) * ChogDamageNumbers.Pop(_pop) * Size;
      TextElement.rectTransform.localScale = new Vector3(scale, scale, scale);
    }

    private static string Shown(float damage) => Mathf.Max(0, Mathf.RoundToInt(damage)).ToString();
  }
}
