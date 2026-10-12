namespace TwinStickShooter
{
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

    private float _timer = -1;
    private Vector2 _targetOffset;

    public void Activate(int damage, Vector2 directionHint)
    {
      TextElement.text = damage.ToString();
      TextElement.enabled = true;
      enabled = true;

      TextElement.rectTransform.anchoredPosition = Vector3.zero;
      TextElement.alpha = 1;
      _timer = 0;

      if (directionHint.x > 0.0f)
        _targetOffset.x = Random.Range(0.0f, OffsetVariance.x) * OffsetVarianceScale.x + Offset.x;
      else
        _targetOffset.x = Random.Range(-OffsetVariance.x, 0.0f) * OffsetVarianceScale.x - Offset.x;

      _targetOffset.y = Random.Range(-OffsetVariance.y, OffsetVariance.y) * OffsetVarianceScale.y + Offset.y;
    }

    public void Deactivate()
    {
      _timer = -1;
      TextElement.enabled = false;
      enabled = false;

      if (onAnimationFinished != null)
        onAnimationFinished(this);
    }

    private void Update()
    {
      _timer += Time.deltaTime;
      float t = Mathf.Clamp01(_timer / AnimationTime);

      TextElement.alpha = AlphaCurve.Evaluate(t);

      var position = _targetOffset;
      position.x *= XOffsetCurve.Evaluate(t);
      position.y *= ZOffsetCurve.Evaluate(t);
      TextElement.rectTransform.anchoredPosition = position;

      var scale = ScaleCurve.Evaluate(t);
      TextElement.rectTransform.localScale = new Vector3(scale, scale, scale);

      if (_timer > AnimationTime)
        Deactivate();
    }
  }
}