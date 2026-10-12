namespace TwinStickShooter
{
  using Quantum;
  using UnityEngine;

  public class CharacterSelectionButtonDelegate : MonoBehaviour
  {
    public UnityEngine.UI.Button SelectButton;
    public AssetRef<EntityPrototype> CharacterPrototype;
    private CharacterSelectionUI _characterSelectionUI;

    private void Start()
    {
      _characterSelectionUI = GetComponentInParent<CharacterSelectionUI>();
      SelectButton.onClick.AddListener(SelectCharacterClicked);
    }

    public void SelectCharacterClicked()
    {
      _characterSelectionUI.SelectCharacterClicked(CharacterPrototype, SelectButton);
    }
  }
}