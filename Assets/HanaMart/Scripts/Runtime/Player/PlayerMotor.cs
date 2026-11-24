using UnityEngine;

namespace HanaMart.Player
{
    /// Bản dịch Player.update của player.js (hệ tọa độ Three.js: pos = (x, z), yaw quanh Y, nhìn về −Z khi yaw = 0).
    public class PlayerMotor
    {
        public Vector2 pos = new Vector2(0.85f, 4.4f);
        public Vector2 vel;
        public float yaw, pitch, crouch, zoom, speedMul = 1f, eye = 1.62f, bob, stepAcc;
        public bool frozen;
        float? tYaw, tPitch;
        public CameraPose Pose;

        public void SetYawFacing(float dx, float dz) { yaw = Mathf.Atan2(-dx, -dz); tYaw = null; }

        public StepResult Update(float dt, PlayerInputState i, PlayerSettings s, WalkArea a, float roll = 0f)
        {
            float mx = 0, mz = 0; bool run = false;
            if (!frozen)
            {
                float sens = s.mouseSens * 0.0022f, inv = s.invertY ? -1f : 1f;
                if (s.smoothCam)
                {
                    if (tYaw == null) { tYaw = yaw; tPitch = pitch; }
                    tYaw -= i.lookDX * sens; tPitch = Mathf.Clamp(tPitch.Value - i.lookDY * sens * inv, -1.35f, 1.35f);
                    float k = 1f - Mathf.Exp(-dt * 9f); yaw += (tYaw.Value - yaw) * k; pitch += (tPitch.Value - pitch) * k;
                }
                else { yaw -= i.lookDX * sens; pitch -= i.lookDY * sens * inv; tYaw = null; }
                float gs = s.padSens * 2.4f * (1f - zoom * 0.55f);
                yaw -= Curve(i.padLook.x) * gs * dt;
                pitch -= Curve(i.padLook.y) * gs * 0.75f * dt * inv;
                pitch = Mathf.Clamp(pitch, -1.35f, 1.35f);
                mx = i.move.x; mz = i.move.y;
                run = i.run;
                crouch += ((i.crouch ? 1f : 0f) - crouch) * Mathf.Min(1f, dt * 8f);
                if (crouch > 0.5f) run = false;
            }
            bool zoomOn = !frozen && i.zoom;
            zoom += ((zoomOn ? 1f : 0f) - zoom) * Mathf.Min(1f, dt * 8f);
            float m = Mathf.Sqrt(mx * mx + mz * mz); if (m > 1f) { mx /= m; mz /= m; }
            float sin = Mathf.Sin(yaw), cos = Mathf.Cos(yaw);
            float wx = mx * cos + mz * sin, wz = -mx * sin + mz * cos;
            float sp0 = (run ? 3.0f : 1.6f) * (1f - crouch * 0.55f) * speedMul;
            float acc = Mathf.Min(1f, dt * 9f);
            vel.x += (wx * sp0 - vel.x) * acc; vel.y += (wz * sp0 - vel.y) * acc;
            float nx = pos.x + vel.x * dt, nz = pos.y + vel.y * dt;
            if (a.Walkable(nx, nz)) { pos.x = nx; pos.y = nz; }
            else if (a.Walkable(nx, pos.y)) { pos.x = nx; vel.y = 0; }
            else if (a.Walkable(pos.x, nz)) { pos.y = nz; vel.x = 0; }
            else vel = Vector2.zero;
            float sp = vel.magnitude;
            bool stepped = false;
            if (sp > 0.15f)
            {
                bob += dt * sp * 4f; stepAcc += sp * dt;
                float stride = run ? 0.82f : 0.68f;
                if (stepAcc > stride) { stepAcc -= stride; stepped = true; }
            }
            else bob += (Mathf.Round(bob / Mathf.PI) * Mathf.PI - bob) * Mathf.Min(1f, dt * 5f);
            float bobAmt = s.headBob ? 1f : 0.15f, ks = Mathf.Min(1f, sp / 1.6f);
            float by = Mathf.Abs(Mathf.Sin(bob)) * 0.035f * bobAmt * ks;
            float bx = Mathf.Cos(bob) * 0.018f * bobAmt * ks;
            Pose.threePos = new Vector3(pos.x + bx * cos, eye - crouch * 0.72f + by - 0.02f, pos.y - bx * sin);
            Pose.pitch = pitch + Mathf.Sin(bob * 2f) * 0.004f * bobAmt;
            Pose.yaw = yaw;
            Pose.roll = roll + Mathf.Cos(bob) * 0.003f * bobAmt;
            return new StepResult { stepped = stepped, run = run, speed = sp };
        }

        static float Curve(float v) => Mathf.Sign(v) * Mathf.Pow(Mathf.Abs(v), 1.7f);
    }
}
