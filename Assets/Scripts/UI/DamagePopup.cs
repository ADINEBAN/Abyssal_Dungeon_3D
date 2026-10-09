using UnityEngine;
using TMPro;
using System.Collections;

namespace AbyssalDungeon.UI
{
    public enum DamageType
    {
        Normal, Critical, Heal, DoT
    }

    public class DamagePopup : MonoBehaviour
    {
        [SerializeField] private TextMeshPro textMesh;
        [SerializeField] private float floatSpeed = 2f;
        [SerializeField] private float lifeTime = 1f;
        
        [Header("Colors")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color criticalColor = Color.yellow;
        [SerializeField] private Color healColor = Color.green;
        [SerializeField] private Color dotColor = new Color(0.5f, 0f, 0.5f); // Purple

        private float timer;

        private static GameObject popupPrefab;

        public static void SetPrefab(GameObject prefab)
        {
            popupPrefab = prefab;
        }

        public static DamagePopup Create(Vector3 position, int amount, DamageType type)
        {
            if (popupPrefab == null) return null;
            
            GameObject go = Instantiate(popupPrefab, position, Quaternion.identity);
            DamagePopup popup = go.GetComponent<DamagePopup>();
            popup.Setup(amount, type);
            return popup;
        }

        private void Setup(int amount, DamageType type)
        {
            textMesh.text = amount.ToString();
            timer = lifeTime;

            switch(type)
            {
                case DamageType.Normal:
                    textMesh.color = normalColor;
                    break;
                case DamageType.Critical:
                    textMesh.color = criticalColor;
                    textMesh.text = amount.ToString() + "!";
                    StartCoroutine(PunchScale());
                    break;
                case DamageType.Heal:
                    textMesh.color = healColor;
                    textMesh.text = "+" + amount.ToString();
                    break;
                case DamageType.DoT:
                    textMesh.color = dotColor;
                    break;
            }
        }

        private void Update()
        {
            transform.position += Vector3.up * floatSpeed * Time.deltaTime;
            
            // Face camera
            if (Camera.main != null)
            {
                transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward,
                    Camera.main.transform.rotation * Vector3.up);
            }

            timer -= Time.deltaTime;
            if (timer < lifeTime * 0.5f)
            {
                float alpha = timer / (lifeTime * 0.5f);
                Color c = textMesh.color;
                c.a = alpha;
                textMesh.color = c;
            }

            if (timer <= 0)
            {
                Destroy(gameObject);
            }
        }

        private IEnumerator PunchScale()
        {
            Vector3 original = transform.localScale;
            transform.localScale = original * 1.5f;
            float t = 0;
            while(t < 0.2f)
            {
                t += Time.deltaTime;
                transform.localScale = Vector3.Lerp(original * 1.5f, original, t / 0.2f);
                yield return null;
            }
            transform.localScale = original;
        }
    }
}
