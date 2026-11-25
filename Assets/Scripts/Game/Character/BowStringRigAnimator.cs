using System;
using UnityEngine;

namespace AbyssGame
{
    /// <summary>
    /// Riggable straight-segment bowstring controller for Ranger_RecurveBow (in its own matching .cs file
    /// so Ranger_RecurveBow.prefab & Hero_Ranger.prefab serialize with zero missing-script errors).
    /// Keeps BowString_Upper and BowString_Lower taut between UpperLimbTip/LowerLimbTip and ArrowNock,
    /// animating ArrowNock pullback during attacks/skills and toggling a live nocked arrow visual.
    /// </summary>
    [DefaultExecutionOrder(55)]
    public class BowStringRigAnimator : MonoBehaviour
    {
        public Transform UpperLimbTip;
        public Transform LowerLimbTip;
        public Transform ArrowNock;
        public Transform UpperString;
        public Transform LowerString;
        public GameObject NockedArrowVisual;

        private Vector3 _restNockLocalPos;
        private float _restUpperLen = 0.80f;
        private float _restLowerLen = 0.88f;
        private float _drawWeight;
        private float _drawTimer;
        private float _drawDuration;
        private bool _initialized;

        public void InitializeSockets()
        {
            if (_initialized) return;
            UpperLimbTip = FindChildDeep(transform, "UpperLimbTip");
            LowerLimbTip = FindChildDeep(transform, "LowerLimbTip");
            ArrowNock = FindChildDeep(transform, "ArrowNock");
            UpperString = FindChildDeep(transform, "BowString_Upper");
            LowerString = FindChildDeep(transform, "BowString_Lower");

            if (ArrowNock != null)
            {
                _restNockLocalPos = ArrowNock.localPosition;
                if (UpperLimbTip != null)
                    _restUpperLen = Mathf.Max(0.15f, Vector3.Distance(UpperLimbTip.localPosition, ArrowNock.localPosition));
                if (LowerLimbTip != null)
                    _restLowerLen = Mathf.Max(0.15f, Vector3.Distance(LowerLimbTip.localPosition, ArrowNock.localPosition));
            }

            if (NockedArrowVisual == null && ArrowNock != null && Application.isPlaying)
            {
                GameObject arrowPrefab = Resources.Load<GameObject>("Weapons/Prefabs/Ranger_Arrow");
                if (arrowPrefab != null)
                {
                    NockedArrowVisual = Instantiate(arrowPrefab, ArrowNock, false);
                    NockedArrowVisual.name = "NockedArrowVisual";
                    NockedArrowVisual.transform.localPosition = Vector3.zero;
                    NockedArrowVisual.transform.localRotation = Quaternion.identity;
                    NockedArrowVisual.transform.localScale = Vector3.one;
                    foreach (var c in NockedArrowVisual.GetComponentsInChildren<Collider>(true))
                        Destroy(c);
                    NockedArrowVisual.SetActive(false);
                }
            }

            _initialized = true;
        }

        public void TriggerDraw(float windupDuration)
        {
            InitializeSockets();
            _drawDuration = Mathf.Clamp(windupDuration, 0.08f, 0.45f);
            _drawTimer = _drawDuration;
            if (NockedArrowVisual != null) NockedArrowVisual.SetActive(true);
        }

        private void LateUpdate()
        {
            if (!_initialized) InitializeSockets();
            if (ArrowNock == null || UpperLimbTip == null || LowerLimbTip == null) return;

            float dt = Time.deltaTime;
            if (_drawTimer > 0f)
            {
                _drawTimer -= dt;
                float u = 1f - Mathf.Clamp01(_drawTimer / Mathf.Max(0.01f, _drawDuration));
                _drawWeight = Mathf.SmoothStep(0f, 1f, u);
                if (_drawTimer <= 0f && NockedArrowVisual != null)
                    NockedArrowVisual.SetActive(false);
            }
            else
            {
                _drawWeight = Mathf.MoveTowards(_drawWeight, 0f, dt * 11f);
            }

            // Pull ArrowNock straight back along local -Z (0.34m full draw)
            ArrowNock.localPosition = _restNockLocalPos + new Vector3(0f, 0f, -0.34f * _drawWeight);

            if (UpperString != null)
            {
                Vector3 toNock = ArrowNock.position - UpperLimbTip.position;
                float len = toNock.magnitude;
                if (len > 0.02f)
                {
                    UpperString.position = UpperLimbTip.position;
                    UpperString.rotation = Quaternion.FromToRotation(-Vector3.up, toNock.normalized);
                    UpperString.localScale = new Vector3(1f, len / _restUpperLen, 1f);
                }
            }

            if (LowerString != null)
            {
                Vector3 toNock = ArrowNock.position - LowerLimbTip.position;
                float len = toNock.magnitude;
                if (len > 0.02f)
                {
                    LowerString.position = LowerLimbTip.position;
                    LowerString.rotation = Quaternion.FromToRotation(Vector3.up, toNock.normalized);
                    LowerString.localScale = new Vector3(1f, len / _restLowerLen, 1f);
                }
            }
        }

        private static Transform FindChildDeep(Transform root, string target)
        {
            var all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
                if (string.Equals(all[i].name, target, StringComparison.OrdinalIgnoreCase))
                    return all[i];
            return null;
        }
    }
}
