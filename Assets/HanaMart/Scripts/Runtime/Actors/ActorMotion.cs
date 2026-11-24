using UnityEngine;

namespace HanaMart.Actors
{
    public struct ActorPose
    {
        public float headRise,neckScale,armScale,eyeScale,eyeEmission,headYaw,headPitch,headRoll,hipY,hipX,hipRoll,torsoYaw,torsoPitch,bodyScaleX,bodyScaleZ;
    }
    public sealed class ActorMotion
    {
        public float phase,blend,time,reveal,revealTarget,headYaw,headPitch,headRoll,headSpin,armUp;
        public ActorPose Pose;
        float snapTimer=2,snapRoll,snapYaw,sr,sy;
        public void Tick(float dt,float speed,ActorOptions options)
        {
            float target=Mathf.Min(1,speed/1.2f);blend+=(target-blend)*Mathf.Min(1,dt*6);
            if(speed>.01f)phase+=dt*speed/1.3f*Mathf.PI*2;
            else phase+=dt*.8f*(1-blend)+dt*blend*3;
            time+=dt;
            float p=phase,a=blend,t=time;
            float breath=Mathf.Sin(t*1.6f+p*.1f);
            reveal+=(revealTarget-reveal)*Mathf.Min(1,dt*1.6f);
            float r=reveal;
            float hr=options.tilt+headRoll+r*.9f*Mathf.Sin(t*.7f),hp=headPitch,hy=headYaw+headSpin;
            if(options.uncanny!=0)
            {
                snapTimer-=dt;
                if(snapTimer<0){snapTimer=1.2f+Random.value*3.2f;snapRoll=(Random.value-.5f)*1.3f;snapYaw=(Random.value-.5f)*.5f;}
                sr+=(snapRoll-sr)*Mathf.Min(1,dt*22);sy+=(snapYaw-sy)*Mathf.Min(1,dt*22);
                hr+=sr;hy+=sy;hp+=(Random.value-.5f)*.02f*options.uncanny;hr+=(Random.value-.5f)*.03f*options.uncanny;
            }
            else hy+=Mathf.Sin(t*.23f+p)*.05f*(1-a);
            Pose=new ActorPose
            {
                headRise=r*.34f,neckScale=1+r*6.5f,armScale=1+r*.55f,eyeScale=1+r*1.6f,eyeEmission=r*6,
                headYaw=hy,headPitch=hp,headRoll=hr,
                hipY=.9f+Mathf.Abs(Mathf.Cos(p))*.028f*a+breath*.004f*(1-a),
                hipX=Mathf.Sin(p)*.018f*a+Mathf.Sin(t*.37f)*.012f*(1-a),
                hipRoll=Mathf.Sin(p)*.03f*a,torsoYaw=Mathf.Sin(p)*.07f*a,torsoPitch=.05f*a+(options.uncanny!=0?.08f:0),
                bodyScaleX=1+breath*.012f*(1-a),bodyScaleZ=.72f*(1+breath*.018f*(1-a))
            };
        }
        public void LookAt(Vector3 target,Vector3 position,float yaw,float dt,ActorOptions options)
        {
            float angle=Mathf.Atan2(target.x-position.x,target.z-position.z)-yaw-Pose.torsoYaw;
            angle=Mathf.Atan2(Mathf.Sin(angle),Mathf.Cos(angle));angle=Mathf.Clamp(angle,-1.3f,1.3f);
            float dy=target.y-(position.y+1.62f*(options.coat=="child"?.68f:1));
            float pitch=-Mathf.Atan2(dy,new Vector2(target.x-position.x,target.z-position.z).magnitude)*.7f;
            float k=Mathf.Min(1,dt*4);headYaw+=(angle-headYaw)*k;headPitch+=(pitch-headPitch)*k;
        }
    }
}
