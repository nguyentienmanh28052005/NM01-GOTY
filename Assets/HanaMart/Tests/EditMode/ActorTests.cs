using System.Linq;
using HanaMart.Actors;
using HanaMart.Store;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HanaMart.Tests
{
    [Timeout(10000)]
    public class ActorTests
    {
        Actor actor;
        static ActorCatalog Catalog()=>AssetDatabase.LoadAssetAtPath<ActorCatalog>("Assets/HanaMart/Data/ActorCatalog.asset");
        [TearDown]public void Cleanup(){if(actor)Object.DestroyImmediate(actor.gameObject);}
        [Test]public void IdleLowersImportedTposeArms()
        {
            actor=Actor.Spawn(Catalog(),new ActorOptions{modelIndex=1},Vector3.zero);
            Assert.IsNotNull(actor);
            actor.StepPose(0,0);
            var shoulder=actor.Animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            var elbow=actor.Animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            Assert.Less(Vector3.Dot((elbow.position-shoulder.position).normalized,Vector3.up),-.85f);
        }
        [Test]public void PoseUsesThreeCoordinatesAndChildHeight()
        {
            actor=Actor.Spawn(Catalog(),new ActorOptions{coat="child"},new Vector3(3,0,4));Assert.IsNotNull(actor);
            actor.yaw=1.2f;actor.StepPose(0,0);
            Assert.That(Vector3.Distance(actor.transform.position,new Vector3(-3,0,4)),Is.LessThan(.0001f));
            Assert.That(Vector3.Distance(actor.transform.forward,ThreeCoords.ToUnity(Mathf.Sin(1.2f),0,Mathf.Cos(1.2f))),Is.LessThan(.0001f));
            var bounds=actor.Model.GetComponentsInChildren<SkinnedMeshRenderer>().Select(r=>r.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});
            Assert.That(bounds.size.y,Is.InRange(1.1f,1.35f));
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void FemaleWalkRetargetsToEveryAvatar(int model)
        {
            actor=Actor.Spawn(Catalog(),new ActorOptions{modelIndex=model},Vector3.zero);Assert.IsNotNull(actor);
            Assert.IsTrue(actor.Animator.isHuman);Assert.IsFalse(actor.Animator.applyRootMotion);
            actor.StepPose(.12f,1.15f);var leg=actor.Animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);var first=leg.localRotation;
            actor.StepPose(.3f,1.15f);Assert.Greater(Quaternion.Angle(first,leg.localRotation),3,"Female Walk must animate "+model);
        }
        [Test]public void RevealDoesNotAccumulateHeadPositionAndCanReset()
        {
            actor=Actor.Spawn(Catalog(),new ActorOptions{modelIndex=0},Vector3.zero);Assert.IsNotNull(actor);
            var head=actor.Animator.GetBoneTransform(HumanBodyBones.Head);actor.StepPose(0,0);var rest=head.position;
            actor.Motion.revealTarget=1;
            for(int frame=0;frame<360;frame++)actor.StepPose(1f/60,0);
            Assert.That(head.position.y-rest.y,Is.InRange(.32f,.36f));var keep=head.position;
            for(int frame=0;frame<60;frame++)actor.StepPose(0,0);
            Assert.That(Vector3.Distance(keep,head.position),Is.LessThan(.0001f));
            actor.Motion.revealTarget=0;
            for(int frame=0;frame<360;frame++)actor.StepPose(1f/60,0);
            Assert.That(Vector3.Distance(rest,head.position),Is.LessThan(.03f));
        }
        [Test]public void SpawnDoesNotMutateCatalogMaterials()
        {
            var shared=Catalog().models[0].renderers.SelectMany(s=>s.materials).First(m=>m.shader.name=="HanaMart/Character");var keep=shared.GetColor("_CoatColor");
            actor=Actor.Spawn(Catalog(),new ActorOptions{modelIndex=0,color=0xf2c200},Vector3.zero);Assert.IsNotNull(actor);actor.StepPose(.2f,0);
            Assert.AreEqual(keep,shared.GetColor("_CoatColor"));Assert.AreEqual(0,shared.GetFloat("_TintStrength"));
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void WalkKeepsAShoeNearTheFloor(int model)
        {
            actor = Actor.Spawn(Catalog(), new ActorOptions { modelIndex = model }, Vector3.zero);
            var shoes = actor.Model.GetComponentsInChildren<SkinnedMeshRenderer>().Single(mesh =>
                mesh.name.Contains("Shoes") || mesh.name.Contains("Sneakers"));
            var baked = new Mesh();
            try
            {
                for (int frame = 0; frame < 8; ++frame)
                {
                    actor.StepPose(.15f, 1.15f);
                    shoes.BakeMesh(baked);
                    float sole = baked.vertices.Min(vertex => shoes.transform.TransformPoint(vertex).y);
                    Assert.That(sole, Is.InRange(-.055f, .065f), $"Model {model}, walk sample {frame}");
                }
            }
            finally { Object.DestroyImmediate(baked); }
        }
    }
}
