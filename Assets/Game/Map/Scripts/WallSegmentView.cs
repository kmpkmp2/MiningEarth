using UnityEngine;

namespace DeepEarth.Map
{
    public class WallSegmentView : MonoBehaviour
    {
        [SerializeField] private MeshRenderer floorRenderer;
        [SerializeField] private MeshRenderer ceilingRenderer;
        [SerializeField] private MeshRenderer leftWallRenderer;
        [SerializeField] private MeshRenderer rightWallRenderer;

        public void SetMaterial(Material material)
        {
            if (material == null) return;

            if (floorRenderer != null) floorRenderer.sharedMaterial = material;
            if (ceilingRenderer != null) ceilingRenderer.sharedMaterial = material;
            if (leftWallRenderer != null) leftWallRenderer.sharedMaterial = material;
            if (rightWallRenderer != null) rightWallRenderer.sharedMaterial = material;
        }
    }
}
