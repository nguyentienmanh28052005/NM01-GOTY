using System;
using HanaMart.Player;
using HanaMart.Store;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HanaMart.Interaction
{
    public sealed class Interactor : MonoBehaviour
    {
        public const float Reach = 2.1f;
        const float LookGrace = .12f;
        [Serializable] class HitInfo { public string id = "", label = ""; }
        public StoreMarker Current { get; private set; }
        public string CurrentId { get; private set; }
        public string Label { get; private set; }
        public float Progress { get; private set; }
        public event Action<string> Pressed;
        public event Action<string, float> Holding;
        public Func<string, string, string> LabelFor;
        public Func<string, float> HoldSeconds;
        public bool Enabled { get; set; } = true;
        float elapsed, lostFor;
        PlayerController player;
        Camera view;

        void Awake() { player = GetComponent<PlayerController>(); view = GetComponent<Camera>(); }
        void LateUpdate()
        {
            if (!view || !player || !player.Input) return;
            var use = player.Input.FindAction("Use");
            bool active = Enabled && player.GameplayEnabled;
            Tick(Time.deltaTime, view.ViewportPointToRay(new Vector3(.5f, .5f, 0)), active && use.WasPressedThisFrame(), active && use.IsPressed());
        }
        public StoreMarker Trace(Ray ray)
        {
            if (!Enabled) return null;
            if (!Physics.Raycast(ray, out var hit, Reach, LayerMask.GetMask("Interact"), QueryTriggerInteraction.Collide)) return null;
            var marker = hit.collider.GetComponentInParent<StoreMarker>();
            return marker && marker.kind == StoreMarker.Kind.Hit ? marker : null;
        }
        public void Tick(float dt, Ray ray, bool pressed, bool held)
        {
            var target = Trace(ray);
            if (!target && Current && held && Enabled)
            {
                lostFor += dt;
                if (lostFor <= LookGrace) return;
            }
            else lostFor = 0;
            if (Current != target)
            {
                Current = target; elapsed = 0; Progress = 0;
                var info = Current && !string.IsNullOrEmpty(Current.json) ? JsonUtility.FromJson<HitInfo>(Current.json) : null;
                CurrentId = Current ? (string.IsNullOrEmpty(info?.id) ? Current.key.Substring(2) : info.id) : null;
                Label = info?.label;
            }
            if (Current)
            {
                var info = string.IsNullOrEmpty(Current.json) ? null : JsonUtility.FromJson<HitInfo>(Current.json);
                Label = LabelFor != null ? LabelFor(CurrentId, info?.label) : info?.label;
            }
            if (!Current || !Enabled || string.IsNullOrEmpty(Label)) { elapsed = 0; Progress = 0; return; }
            if (pressed) Pressed?.Invoke(CurrentId);
            if (held)
            {
                elapsed += dt;
                Progress = Mathf.Clamp01(elapsed / Mathf.Max(.001f, HoldSeconds?.Invoke(CurrentId) ?? 1f));
                Holding?.Invoke(CurrentId, Progress);
            }
            else { elapsed = 0; Progress = 0; }
        }
    }
}
