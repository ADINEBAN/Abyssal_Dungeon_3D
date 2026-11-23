using UnityEngine;

namespace AbyssGame
{
    /// <summary>
    /// Ensures the character's 3D SkinnedMeshRenderers and weapon MeshRenderers cast real-time
    /// URP directional human shadows onto the floor without any artificial quad/box frames at the feet.
    /// </summary>
    public class CharacterHumanShadowCaster : MonoBehaviour
    {
        public void Bind(Transform visualRoot, Animator anim, float scale = 1f)
        {
            Transform oldGroup = transform.Find("HumanGroundShadowGroup");
            if (oldGroup != null) BakedAssetRegistry.DestroySafe(oldGroup.gameObject);

            if (visualRoot == null) return;
            foreach (var smr in visualRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (smr == null) continue;
                smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                smr.receiveShadows = true;
                smr.updateWhenOffscreen = true;
            }
            foreach (var mr in visualRoot.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (mr == null) continue;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                mr.receiveShadows = true;
            }
        }
    }
}
