namespace TwinStickShooter
{
  using UnityEngine;
  using UnityEngine.UI;
  using Quantum;

  public enum ECharacter
  {
    Archer,
    Mage,
    Knight
  }

  public class CharacterSelectionDropdown : MonoBehaviour
  {
    public Dropdown CharacerSelectDropdown;
    public ECharacter SelectedCharacter;

    public AssetRef<Quantum.EntityPrototype> ArcherPrototype;
    public AssetRef<Quantum.EntityPrototype> MagePrototype;
    public AssetRef<Quantum.EntityPrototype> KnightPrototype;

    //TODO fix
    // private CustomCallbacks _customCallbacks;
    //
    // void Start()
    // {
    //   _customCallbacks = FindObjectOfType<CustomCallbacks>();
    //
    //   var enumValues = Enum.GetValues(typeof(ECharacter));
    //   List<string> stringValues = new List<string>();
    //   for (int i = 0; i < enumValues.Length; i++)
    //   {
    //     stringValues.Add(enumValues.GetValue(i).ToString());
    //   }
    //   CharacerSelectDropdown.AddOptions(stringValues);
    //
    //   CharacerSelectDropdown.onValueChanged.AddListener(delegate { OnCharacterSelectionChanged(CharacerSelectDropdown); });
    // }
    //
    // private void Update()
    // {
    //   if (UIMain.Client == null || UIMain.Client.InRoom == false) return;
    //   var properties = UIMain.Client.LocalPlayer.CustomProperties;
    //   if (properties["character"] == null)
    //   {
    //     UpdatePlayerProperties(CharacerSelectDropdown.options[CharacerSelectDropdown.value].text);
    //   }
    // }
    //
    // public void OnCharacterSelectionChanged(Dropdown dropdown)
    // {
    //   var selectedCharacter = (ECharacter)Enum.Parse(typeof(ECharacter), CharacerSelectDropdown.options[dropdown.value].text);
    //   switch (selectedCharacter)
    //   {
    //     case ECharacter.Archer:
    //       _customCallbacks.SelectedCharacter = ArcherPrototype;
    //       break;
    //     case ECharacter.Mage:
    //       _customCallbacks.SelectedCharacter = MagePrototype;
    //       break;
    //     case ECharacter.Knight:
    //       _customCallbacks.SelectedCharacter = KnightPrototype;
    //       break;
    //     default:
    //       break;
    //   }
    //   UpdatePlayerProperties(CharacerSelectDropdown.options[dropdown.value].text);
    // }
    //
    // private void UpdatePlayerProperties(string characterName)
    // {
    //   if (UIMain.Client == null || UIMain.Client.InRoom == false) return;
    //
    //   var properties = UIMain.Client.LocalPlayer.CustomProperties;
    //   properties["character"] = characterName;
    //   UIMain.Client.LocalPlayer.SetCustomProperties(properties);
    //
    //   properties["TeamA"] = true;
    // }
  }
}