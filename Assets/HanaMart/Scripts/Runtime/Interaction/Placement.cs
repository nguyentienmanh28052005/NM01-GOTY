using HanaMart.Player;
using UnityEngine;

namespace HanaMart.Interaction
{
    public static class Placement
    {
        public static float SurfaceAt(float x, float z)
        {
            if (x > 1.2f && x < 2 && z > 2.4f && z < 6.2f) return 1.02f;
            if (x > -.05f && x < .5f && z > 1.6f && z < 7.4f) return .92f;
            if (x > 7.35f && x < 11.9f && z > 7.35f && z < 7.85f) return 1.02f;
            return 0;
        }
        public static Vector3 Target(Ray threeRay, Vector2 player, WalkArea area)
        {
            var o = threeRay.origin; var d = threeRay.direction;
            float best = float.PositiveInfinity; Vector3 target = default;
            foreach (float y in new[] { 1.02f, .92f, 0 })
            {
                if (d.y > -.05f) break;
                float t = (y - o.y) / d.y;
                if (t <= 0 || t > 2.6f) continue;
                float x = o.x + d.x * t, z = o.z + d.z * t;
                if (Mathf.Abs(SurfaceAt(x,z)-y) > .001f || (y == 0 && !area.Walkable(x,z))) continue;
                if (t < best) { best = t; target = new Vector3(x,y,z); }
            }
            if (!float.IsPositiveInfinity(best)) return target;
            var forward = new Vector2(d.x,d.z).normalized;
            float range = 1.6f;
            // Bound the original while loop: at most 11 reductions to 0.5 m.
            for (int step=0; step<11 && range>.5001f && !area.Walkable(player.x+forward.x*range,player.y+forward.y*range);step++) range-=.1f;
            float px=player.x+forward.x*range, pz=player.y+forward.y*range;
            return new Vector3(px,SurfaceAt(px,pz),pz);
        }
        public static Vector3 Arc(Vector3 from, Vector3 to, float progress, float height)
        {
            float k=Mathf.Clamp01(progress);
            float ease=k<.5f ? 2*k*k : 1-Mathf.Pow(-2*k+2,2)/2;
            return Vector3.Lerp(from,to,ease)+Vector3.up*(Mathf.Sin(k*Mathf.PI)*height);
        }
    }
    public sealed class ItemBody
    {
        public Vector3 position, velocity, spin;
        public Quaternion rotation = Quaternion.identity;
        public float halfHeight = .06f, bounce = .32f;
        public bool sleeping, upright;
        public int lands;
    }
    public static class ItemPhysics
    {
        public static void Step(ItemBody body, float dt)
        {
            if (body.sleeping) return;
            body.velocity.y-=9.8f*dt;
            body.position+=body.velocity*dt;
            var p=body.position; var v=body.velocity;
            if (p.z>0 && p.z<8)
            {
                if(p.x<.08f) {p.x=.08f;v.x=Mathf.Abs(v.x)*.4f;}
                if(p.x>11.92f) {p.x=11.92f;v.x=-Mathf.Abs(v.x)*.4f;}
                if(p.z>7.9f) {p.z=7.9f;v.z=-Mathf.Abs(v.z)*.4f;}
            }
            float spin=body.spin.magnitude;
            if(spin>.0001f) body.rotation=Quaternion.AngleAxis(spin*dt*Mathf.Rad2Deg,body.spin/spin)*body.rotation;
            float ground=Placement.SurfaceAt(p.x,p.z)+body.halfHeight;
            if(p.y<ground)
            {
                p.y=ground; float impact=-v.y;
                if(impact>.9f)
                {
                    v.y=impact*body.bounce;v.x*=.7f;v.z*=.7f;body.spin*=.6f;
                    body.spin.x+=v.z*4;body.spin.z-=v.x*4;body.lands++;
                }
                else
                {
                    v.y=0;float f=Mathf.Max(0,1-dt*7);v.x*=f;v.z*=f;body.spin*=Mathf.Max(0,1-dt*6);
                    var e=body.rotation.eulerAngles;
                    e.x=Mathf.DeltaAngle(0,e.x);e.z=Mathf.DeltaAngle(0,e.z);
                    float tx=body.upright?0:Mathf.Round(e.x/90)*90, tz=body.upright?0:Mathf.Round(e.z/90)*90;
                    e.x=Mathf.Lerp(e.x,tx,Mathf.Min(1,dt*8));e.z=Mathf.Lerp(e.z,tz,Mathf.Min(1,dt*8));body.rotation=Quaternion.Euler(e);
                    if(body.lands==0)body.lands=1;
                    if(v.sqrMagnitude<.0025f && body.spin.sqrMagnitude<.01f)body.sleeping=true;
                }
            }
            body.position=p;body.velocity=v;
        }
    }
}
