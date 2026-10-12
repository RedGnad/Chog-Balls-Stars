namespace TwinStickShooter
{
  using UnityEngine;

  public class CharacterMaterialController : MonoBehaviour
  {
    private Material[] _invisibleMaterial;
    private Material[] _originalMaterials;

    private MeshRenderer _meshRenderer;

    // Start is called before the first frame update
    void Start()
    {
      _meshRenderer = GetComponent<MeshRenderer>();
      _originalMaterials = _meshRenderer.materials;
      _invisibleMaterial = new Material[_originalMaterials.Length];

      for (int i = 0; i < _invisibleMaterial.Length; i++)
      {
        _invisibleMaterial[i] = new Material(_originalMaterials[i]);

        _invisibleMaterial[i].SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        _invisibleMaterial[i].SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        _invisibleMaterial[i].SetInt("_ZWrite", 0);
        _invisibleMaterial[i].DisableKeyword("_ALPHATEST_ON");
        _invisibleMaterial[i].DisableKeyword("_ALPHABLEND_ON");
        _invisibleMaterial[i].EnableKeyword("_ALPHAPREMULTIPLY_ON");
        _invisibleMaterial[i].renderQueue = 3000;

        _invisibleMaterial[i].color = new Color(_invisibleMaterial[i].color.r, _invisibleMaterial[i].color.g,
          _invisibleMaterial[i].color.b, .5f);
      }
    }

    public void SetInvisibleMaterial()
    {
      _meshRenderer.materials = _invisibleMaterial;
    }

    public void SetVisible()
    {
      if (_meshRenderer != null)
        _meshRenderer.materials = _originalMaterials;
    }
  }
}
