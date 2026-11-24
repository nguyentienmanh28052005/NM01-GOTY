using HanaMart.Actors;
using NUnit.Framework;
using UnityEngine;

namespace HanaMart.Tests
{
    [Timeout(10000)]
    public class ActorMotionTests
    {
        [Test] public void ChildAlwaysUsesPoint68Scale()
        {
            var o=new ActorOptions{coat="child",scale=1.2f,tall=1.1f};
            Assert.That(Vector3.Distance(o.Scale,new Vector3(.68f,.748f,.68f)),Is.LessThan(.00001f));
        }
        [Test] public void AdultHonoursScaleAndTall()
        {
            var o=new ActorOptions{scale=1.2f,tall=1.1f};
            Assert.That(Vector3.Distance(o.Scale,new Vector3(1.2f,1.32f,1.2f)),Is.LessThan(.00001f));
        }
        [TestCase("raincoat",false,1)]
        [TestCase("manager",false,1)]
        [TestCase("child",false,2)]
        [TestCase("padding",true,0)]
        public void RolesChooseExistingModels(string coat,bool longHair,int model) => Assert.AreEqual(model,new ActorOptions{coat=coat,longHair=longHair}.ModelIndex);
        [Test] public void ExplicitModelOverridesRole() => Assert.AreEqual(3,new ActorOptions{modelIndex=3}.ModelIndex);
        [Test] public void RevealMatchesOriginalRateAndDeformation()
        {
            var m=new ActorMotion{revealTarget=1};m.Tick(.1f,0,new ActorOptions());
            Assert.That(m.reveal,Is.EqualTo(.16f).Within(.00001f));
            Assert.That(m.Pose.headRise,Is.EqualTo(.0544f).Within(.00001f));
            Assert.That(m.Pose.neckScale,Is.EqualTo(2.04f).Within(.00001f));
            Assert.That(m.Pose.armScale,Is.EqualTo(1.088f).Within(.00001f));
            Assert.That(m.Pose.eyeScale,Is.EqualTo(1.256f).Within(.00001f));
            Assert.That(m.Pose.eyeEmission,Is.EqualTo(.96f).Within(.00001f));
        }
        [Test] public void RevealIsAbsoluteAndReturnsToRest()
        {
            var m=new ActorMotion{revealTarget=1};var options=new ActorOptions();
            for(int frame=0;frame<600;frame++)m.Tick(1f/60,0,options);
            Assert.That(m.Pose.headRise,Is.EqualTo(.34f).Within(.0001f));
            m.revealTarget=0;
            for(int frame=0;frame<600;frame++)m.Tick(1f/60,0,options);
            Assert.That(m.Pose.headRise,Is.EqualTo(0).Within(.0001f));Assert.That(m.Pose.neckScale,Is.EqualTo(1).Within(.0001f));
        }
        [Test] public void HeadLookClampsYawAndUsesChildEyeHeight()
        {
            var m=new ActorMotion();m.LookAt(new Vector3(10,1.62f*.68f,0),Vector3.zero,0,1,new ActorOptions{coat="child"});
            Assert.That(m.headYaw,Is.EqualTo(1.3f).Within(.00001f));Assert.That(m.headPitch,Is.EqualTo(0).Within(.00001f));
        }
        [Test] public void WalkingUsesStride13AndBlendRate6()
        {
            var m=new ActorMotion();m.Tick(.1f,1.3f,new ActorOptions());
            Assert.That(m.phase,Is.EqualTo(Mathf.PI*.2f).Within(.00001f));Assert.That(m.blend,Is.EqualTo(.6f).Within(.00001f));
        }
    }
}
