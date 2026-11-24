using System;
using System.Collections.Generic;
using UnityEngine;

namespace HanaMart.Player
{
    /// Đầu vào một khung (đã gom chuột/bàn phím/tay cầm). move.x = phải, move.y = lùi (+z Three.js), như player.js.
    public struct PlayerInputState
    {
        public float lookDX, lookDY; public Vector2 move, padLook; public bool run, crouch, zoom;
    }

    [Serializable]
    public class PlayerSettings
    {
        public float mouseSens = 1f, padSens = 1f; public bool invertY, headBob = true, smoothCam;
    }

    /// Hộp va chạm 2D (x0,z0,x1,z1) trong hệ Three.js; off = tạm tắt (cửa mở…).
    [Serializable]
    public class Box2 { public float x0, z0, x1, z1; public bool off; public Box2(float x0, float z0, float x1, float z1) { this.x0 = x0; this.z0 = z0; this.x1 = x1; this.z1 = z1; } }

    public struct CameraPose { public Vector3 threePos; public float pitch, yaw, roll; }
    public struct StepResult { public bool stepped, run; public float speed; }
}
