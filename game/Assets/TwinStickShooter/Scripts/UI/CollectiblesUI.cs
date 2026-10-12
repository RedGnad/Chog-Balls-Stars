namespace TwinStickShooter
{
  using Quantum;
  using UnityEngine.UI;
  using TMPro;

  public class CollectiblesUI : QuantumCallbacks
  {
    public TextMeshProUGUI AmountText;
    public Image CoinImage;
    public AssetRef<CollectibleData> CollectableDataRef;

    private CharacterUIFollow _characterFollow;

    private void Awake()
    {
      _characterFollow = GetComponent<CharacterUIFollow>();
    }

    public override void OnUpdateView(QuantumGame game)
    {
      Frame frame = game.Frames.Verified;
      if (frame == null)
      {
        return;
      }

      if (frame.Exists(_characterFollow.EntityView.EntityRef) == false)
      {
        return;
      }

      Inventory inventory = frame.Get<Inventory>(_characterFollow.EntityView.EntityRef);

      for (int i = 0; i < _characterFollow.ContainedObjects.Count; i++)
      {
        bool isActive =
          inventory.CollectibleData != default &&
          CollectableDataRef == inventory.CollectibleData &&
          inventory.CollectiblesAmount > 0;

        AmountText.enabled = isActive;
        CoinImage.enabled = isActive;
      }

      int amount = inventory.CollectiblesAmount;
      AmountText.text = amount.ToString();
    }
  }
}