using HanaMart.Actors;
using NUnit.Framework;
using UnityEditor;

namespace HanaMart.Tests
{
    [Timeout(10000)]
    public class ActorCatalogTests
    {
        [Test] public void FourModelsAndHumanoidWalkAreReady()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<ActorCatalog>("Assets/HanaMart/Data/ActorCatalog.asset");
            Assert.IsNotNull(catalog);Assert.AreEqual(4,catalog.models.Length);Assert.IsNotNull(catalog.walk);Assert.IsTrue(catalog.walk.humanMotion);
            foreach(var model in catalog.models)
            {Assert.IsNotNull(model.model,model.id);Assert.IsNotNull(model.avatar,model.id);Assert.IsTrue(model.avatar.isValid && model.avatar.isHuman,model.id);}
        }
    }
}
