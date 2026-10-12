namespace TwinStickShooter
{
  using Quantum;
  using TMPro;

  public class NumberedBarUI : FilledBarUI
  {
    public TextMeshProUGUI NumberText;

    public override void OnUpdateView()
    {
      base.OnUpdateView();

      NumberText.text = _currentValue.ToString();
    }
  }
}