using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HanaMart.Interaction;
using HanaMart.Player;
using UnityEngine;

namespace HanaMart.Store
{
    public sealed class StoreDoors : MonoBehaviour
    {
        public sealed class Leaf
        {
            public Transform transform;
            public HingeMotor motor;
            public Quaternion closed;
        }
        [Serializable] class Metadata { public float sign = 1; }
        public Leaf Front { get; private set; }
        public Leaf Back { get; private set; }
        public Leaf Freezer { get; private set; }
        public readonly List<Leaf> Fridges = new List<Leaf>();
        public readonly List<Leaf> Microwaves = new List<Leaf>();
        public bool defaultInteractions = true;
        readonly List<Leaf> leaves = new List<Leaf>();
        readonly Dictionary<StoreMarker, Leaf> fridgeHits = new Dictionary<StoreMarker, Leaf>();
        PlayerController player;
        Interactor interactor;
        Box2 backCollision;
        BoxCollider backPhysics;

        void Awake() => Bind();
        void Start()
        {
            player = Camera.main ? Camera.main.GetComponent<PlayerController>() : null;
            interactor = player ? player.GetComponent<Interactor>() : null;
            if (interactor) interactor.Pressed += OnPressed;
            backCollision = player?.collisionBoxes.FirstOrDefault(c => Mathf.Abs(c.x0 - 1.6f) < .001f && Mathf.Abs(c.z0 + 4.68f) < .001f);
            var col = GetComponentsInChildren<StoreMarker>(true).FirstOrDefault(m => m.kind == StoreMarker.Kind.Collider && m.GetComponent<BoxCollider>() && Mathf.Abs(m.transform.position.z + 4.6f) < .01f && Mathf.Abs(m.transform.position.x + 2.1f) < .01f);
            backPhysics = col ? col.GetComponent<BoxCollider>() : null;
        }
        void OnDestroy() { if (interactor) interactor.Pressed -= OnPressed; }
        void OnPressed(string id) { if (defaultInteractions) Use(id, interactor.Current); }
        public void Bind()
        {
            leaves.Clear(); Fridges.Clear(); Microwaves.Clear(); fridgeHits.Clear();
            foreach (var marker in GetComponentsInChildren<StoreMarker>(true))
            {
                if (marker.key == "R:door") Front = Add(marker.transform, 1.35f, 5);
                else if (marker.key == "R:backDoor") Back = Add(marker.transform, -1.6f, 4);
                else if (marker.key == "R:freezerLid") { Freezer = Add(marker.transform, -1.2f, 1000); Freezer.motor.axis = Vector3.right; }
                else if (marker.key.StartsWith("S:fridgeDoors["))
                {
                    float sign = JsonUtility.FromJson<Metadata>(marker.json).sign;
                    Fridges.Add(Add(marker.transform, -sign * 1.45f, 5));
                }
                else if (marker.key.StartsWith("S:microwaves[") && marker.key.EndsWith(".door")) Microwaves.Add(Add(marker.transform, 1.75f, 7));
            }
            foreach (var hit in GetComponentsInChildren<StoreMarker>(true).Where(m => m.key == "H:fridgeDoor"))
                fridgeHits[hit] = Fridges.OrderBy(l => Vector3.Distance(l.transform.TransformPoint(new Vector3(Mathf.Sign(l.motor.radians) * -.39f,1.12f,0)), hit.transform.position)).First();
        }
        Leaf Add(Transform t, float radians, float rate)
        {
            var leaf = new Leaf { transform = t, motor = new HingeMotor(radians, rate), closed = t.localRotation };
            leaves.Add(leaf); return leaf;
        }
        void Update() => Tick(Time.deltaTime);
        public void Tick(float dt)
        {
            foreach (var leaf in leaves) { leaf.motor.Tick(dt); leaf.transform.localRotation = leaf.closed * leaf.motor.Rotation; }
            // The front door collision is intentionally retained in game.js (rule 1).
            bool backOpen = Back != null && Back.motor.target > 0;
            if (backCollision != null) backCollision.off = backOpen;
            if (backPhysics) backPhysics.enabled = !backOpen;
        }
        public void Use(string id, StoreMarker hit)
        {
            if (id == "backDoor" && Back != null) Back.motor.target = Back.motor.target > .5f ? 0 : 1;
            else if (id == "fridgeDoor" && hit)
            {
                // Bind once at the closed pose: the hit stays fixed while its leaf swings.
                if (fridgeHits.TryGetValue(hit, out var leaf)) leaf.motor.target = leaf.motor.target > .5f ? 0 : 1;
            }
            else if (id == "micro1" || id == "micro2")
            {
                int index = id == "micro1" ? 0 : 1;
                if (index < Microwaves.Count) Microwaves[index].motor.target = Microwaves[index].motor.target > .5f ? 0 : 1;
            }
            else if (id == "freezer" && Freezer != null) StartCoroutine(OpenFreezer());
        }

        public bool FridgeIsOpen(StoreMarker hit) => hit && fridgeHits.TryGetValue(hit, out var leaf) && leaf.motor.target > .5f;
        IEnumerator OpenFreezer()
        {
            Freezer.motor.target = 1;
            yield return new WaitForSeconds(1.5f);
            Freezer.motor.target = 0;
        }
    }
}
