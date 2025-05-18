using UnityEngine;
using CesiumForUnity;

public class CesiumBuildingOpacity : MonoBehaviour
{
    [Range(0f, 1f)]
    public float opacity = 0.3f;

    public Material transparentMaterialTemplate;

    void Start()
    {
        ApplyOpacityToAllBuildings();
    }

    void ApplyOpacityToAllBuildings()
    {
        Cesium3DTileset[] tilesets = FindObjectsOfType<Cesium3DTileset>();

        foreach (var tileset in tilesets)
        {
            MeshRenderer[] renderers = tileset.GetComponentsInChildren<MeshRenderer>(true);

            foreach (var renderer in renderers)
            {
                if (renderer != null)
                {
                    // Clone the material to avoid modifying the shared one
                    Material newMat = new Material(transparentMaterialTemplate);
                    
                    // Set opacity
                    Color color = newMat.color;
                    color.a = opacity;
                    newMat.color = color;

                    // Apply to renderer
                    renderer.material = newMat;
                }
            }
        }
    }
}
