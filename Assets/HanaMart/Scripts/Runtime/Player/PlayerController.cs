using System;
using System.Collections.Generic;
using HanaMart.Store;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace HanaMart.Player
{
    [RequireComponent(typeof(Camera))]
    public sealed class PlayerController : MonoBehaviour
    {
        public InputActionAsset controls;
        public NavMeshData storeNavMesh;
        public List<Box2> collisionBoxes = new List<Box2>();
        public PlayerSettings settings = new PlayerSettings();
        public PlayerMotor Motor { get; } = new PlayerMotor();
        public WalkArea Area { get; private set; }
        public StoreNavigation Navigation { get; private set; }
        public InputActionAsset Input { get; private set; }
        public event Action<StepResult> Stepped;
        public bool GameplayEnabled { get; set; } = true;
        public bool ExternalView { get; set; }
        public bool AutoManageCursor { get; set; } = true;
        public bool IsFocused => focused;
        public float CurrentFov => 72f * (1f - Motor.zoom * .4f);
        public float roll;
        Camera view;
        NavMeshDataInstance navMeshInstance;
        InputAction move, look, padLook, run, crouch, zoom;
        bool focused = true;

        void Awake()
        {
            view = GetComponent<Camera>();
            Area = new WalkArea(collisionBoxes);
            Navigation = new StoreNavigation(collisionBoxes);
            if (controls)
            {
                Input = Instantiate(controls);
                move = Input.FindAction("Move", true); look = Input.FindAction("Look", true);
                padLook = Input.FindAction("PadLook", true); run = Input.FindAction("Run", true);
                crouch = Input.FindAction("Crouch", true); zoom = Input.FindAction("Zoom", true);
            }
            Motor.Update(0, default, settings, Area);
            ApplyPose();
        }

        void OnEnable()
        {
            if (storeNavMesh) navMeshInstance = NavMesh.AddNavMeshData(storeNavMesh);
            if (Input) Input.Enable();
            if (AutoManageCursor && GameplayEnabled) LockCursor(true);
        }
        void OnDisable()
        {
            if (navMeshInstance.valid) navMeshInstance.Remove();
            if (Input) Input.Disable();
            LockCursor(false);
            Gamepad.current?.ResetHaptics();
        }
        void OnDestroy() { if (Input) Destroy(Input); }
        void OnApplicationFocus(bool value)
        {
            focused = value;
            if (!value) { LockCursor(false); Gamepad.current?.ResetHaptics(); }
        }
        public void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
        public void Rumble(float strong, float weak, float seconds) => StartCoroutine(RumbleFor(strong, weak, seconds));
        System.Collections.IEnumerator RumbleFor(float strong, float weak, float seconds)
        {
            var pad = Gamepad.current;
            if (pad == null) yield break;
            pad.SetMotorSpeeds(Mathf.Clamp01(weak), Mathf.Clamp01(strong));
            yield return new WaitForSecondsRealtime(Mathf.Max(0, seconds));
            if (pad.added) pad.ResetHaptics();
        }
        void Update()
        {
            if (AutoManageCursor)
            {
                if (Keyboard.current?.escapeKey.wasPressedThisFrame == true) LockCursor(false);
                if (GameplayEnabled && focused && Mouse.current?.leftButton.wasPressedThisFrame == true) LockCursor(true);
            }
            var state = default(PlayerInputState);
            if (GameplayEnabled && focused && Input)
            {
                Vector2 motion = move.ReadValue<Vector2>();
                Vector2 mouse = Cursor.lockState == CursorLockMode.Locked ? look.ReadValue<Vector2>() : Vector2.zero;
                Vector2 stick = padLook.ReadValue<Vector2>();
                state = new PlayerInputState
                {
                    move = new Vector2(motion.x, -motion.y), lookDX = mouse.x, lookDY = -mouse.y,
                    padLook = new Vector2(stick.x, -stick.y), run = run.IsPressed(),
                    crouch = crouch.IsPressed(), zoom = zoom.IsPressed()
                };
            }
            StepResult result = Motor.Update(Mathf.Min(Time.deltaTime, .1f), state, settings, Area, roll);
            if (!ExternalView) ApplyPose();
            if (result.stepped) Stepped?.Invoke(result);
        }
        public void ApplyPose()
        {
            var pose = Motor.Pose;
            transform.SetPositionAndRotation(ThreeCoords.ToUnity(pose.threePos), ThreeCoords.CameraRotation(pose.pitch, pose.yaw, pose.roll));
            view.fieldOfView = CurrentFov;
        }
    }
}
