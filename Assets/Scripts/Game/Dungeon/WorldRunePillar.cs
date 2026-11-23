using System;
using UnityEngine;

namespace AbyssGame
{
    public class WorldRunePillar : MonoBehaviour, IWorldInteractable
    {
        public Vector3 Position => transform.position;
        public float InteractRadius => 3.0f;
        public bool Activated { get; private set; }
        public bool TriggeredRemotely { get; private set; }
        public int PillarIndex => _index;
        [SerializeField] private int _index;
        [SerializeField] private Renderer _crystal;
        private Action<WorldRunePillar> _onActivated;

        private void OnEnable()
        {
            if (!HeroPlayerInput.Interactables.Contains(this))
                HeroPlayerInput.Interactables.Add(this);
        }

        private void OnDisable()
        {
            HeroPlayerInput.Interactables.Remove(this);
        }

        public void BindCallback(Action<WorldRunePillar> cb)
        {
            _onActivated = cb;
            if (!HeroPlayerInput.Interactables.Contains(this))
                HeroPlayerInput.Interactables.Add(this);
        }

        public static WorldRunePillar Create(Transform parent, Vector3 pos, int idx, Action<WorldRunePillar> cb)
        {
            return BakedAssetRegistry.InstantiateOrBuildPrefab<WorldRunePillar>(
                BakedAssetRegistry.PropsPrefabFolder,
                "PF_RunePillar",
                parent,
                pos,
                Quaternion.identity,
                () =>
                {
                    var go = new GameObject("PF_RunePillar");
                    var stone = PbrMaterialLibrary.GothicStone(new Color(0.68f, 0.66f, 0.80f));
                    var unlitRune = PbrMaterialLibrary.AbyssalCrystal(new Color(0.20f, 0.48f, 0.95f), new Color(0.38f, 0.68f, 1.0f), 2.4f);

                    ObjMeshLoader.SpawnPiece("RunicPillarAltar", go.transform, Vector3.zero, Quaternion.identity, Vector3.one, stone, addBoxCollider: true);
                    var crys = ObjMeshLoader.SpawnPiece("AbyssalCrystalCluster", go.transform, new Vector3(0f, 1.92f, 0f), Quaternion.identity, Vector3.one * 0.36f, unlitRune, addBoxCollider: false);

                    var rp = go.AddComponent<WorldRunePillar>();
                    rp._index = idx;
                    rp._crystal = crys.GetComponentInChildren<Renderer>();
                    return go;
                },
                rp =>
                {
                    if (rp == null) return;
                    rp.gameObject.name = "RunePillar_" + idx;
                    rp._index = idx;
                    rp._onActivated = cb;
                    if (!HeroPlayerInput.Interactables.Contains(rp))
                        HeroPlayerInput.Interactables.Add(rp);
                });
        }

        private void Update()
        {
            if (_crystal != null) _crystal.transform.Rotate(0f, 65f * Time.deltaTime, 0f, Space.World);
        }

        public bool CanInteract() => !Activated;
        public string GetPrompt() => $"[F] Thắp sáng Trụ Ấn Phong #{_index}";

        public void ActivateFromNetwork()
        {
            if (Activated) return;
            TriggeredRemotely = true;
            Interact(null);
        }

        public void Interact(ActorUnit user)
        {
            if (Activated) return;
            Activated = true;
            if (_crystal != null)
            {
                var litMat = PbrMaterialLibrary.AbyssalCrystal(new Color(1f, 0.78f, 0.22f), new Color(1f, 0.90f, 0.35f), 3.6f);
                foreach (var r in _crystal.transform.root == transform ? GetComponentsInChildren<Renderer>() : new[] { _crystal })
                {
                    if (r.gameObject.name != "RunicPillarAltar" && r.transform.parent != transform)
                        r.sharedMaterial = litMat;
                }
                _crystal.sharedMaterial = litMat;
            }
            if (user != null) user.AddShield(user.MaxHp * 0.15f);
            CharacterVisualFactory.SpawnBurst(transform.position + Vector3.up * 1.8f, new Color(1f, 0.85f, 0.25f), 2.5f, 0.5f);
            _onActivated?.Invoke(this);
        }
    }
}
