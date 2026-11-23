using System;
using UnityEngine;

namespace AbyssGame
{
    public class WorldExitPortal : MonoBehaviour, IWorldInteractable
    {
        public Vector3 Position => transform.position;
        public float InteractRadius => 3.5f;
        [SerializeField] private string _prompt;
        private Action _onEnter;
        [SerializeField] private Transform _ring;

        private void OnEnable()
        {
            if (!HeroPlayerInput.Interactables.Contains(this))
                HeroPlayerInput.Interactables.Add(this);
        }

        private void OnDisable()
        {
            HeroPlayerInput.Interactables.Remove(this);
        }

        public void BindCallback(string prompt, Action onEnter)
        {
            if (!string.IsNullOrEmpty(prompt))
                _prompt = prompt;
            _onEnter = onEnter;
            if (!HeroPlayerInput.Interactables.Contains(this))
                HeroPlayerInput.Interactables.Add(this);
        }

        public static WorldExitPortal Create(Transform parent, Vector3 pos, string prompt, Action onEnter)
        {
            var go = new GameObject("VictoryPortal");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;

            Color col = new Color(0.55f, 0.25f, 1f);
            ObjMeshLoader.SpawnPiece("AbyssalPortal", go.transform, Vector3.zero, Quaternion.identity, Vector3.one, PbrMaterialLibrary.GothicStone(), addBoxCollider: false);
            var ring = ObjMeshLoader.SpawnPiece("EscapeFloorPortal", go.transform, new Vector3(0f, 0.02f, 0f), Quaternion.identity, Vector3.one * 1.05f,
                PbrMaterialLibrary.AbyssalCrystal(col * 0.85f, col * 1.5f, 2.8f), addBoxCollider: false);

            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = col;
            l.range = 12f;
            l.intensity = 2.5f;

            var p = go.AddComponent<WorldExitPortal>();
            p._prompt = prompt;
            p._onEnter = onEnter;
            p._ring = ring.transform;
            if (!HeroPlayerInput.Interactables.Contains(p))
                HeroPlayerInput.Interactables.Add(p);
            return p;
        }

        private void Update()
        {
            if (_ring != null) _ring.Rotate(0f, 80f * Time.deltaTime, 0f, Space.Self);
        }

        public bool CanInteract() => true;
        public string GetPrompt() => _prompt;
        public void Interact(ActorUnit user) => _onEnter?.Invoke();
    }
}
