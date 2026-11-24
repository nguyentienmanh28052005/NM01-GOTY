using System.Linq;
using HanaMart.Actors;
using UnityEditor;
using UnityEngine;

namespace HanaMart.EditorTools
{
    public static class ActorSetup
    {
        public const string CatalogPath="Assets/HanaMart/Data/ActorCatalog.asset";
        [MenuItem("HanaMart/Build Actor Catalog")]
        public static void BuildCatalog()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<ActorCatalog>(CatalogPath);
            if(!catalog){catalog=ScriptableObject.CreateInstance<ActorCatalog>();AssetDatabase.CreateAsset(catalog,CatalogPath);}
            var ids=new[]{"Ch31","Ch08","Ch01","Ch42"};
            catalog.models=CharacterSetup.ModelPaths.Select((path,index)=>new ActorCatalog.Entry
            {
                id=ids[index],model=AssetDatabase.LoadAssetAtPath<GameObject>(path),avatar=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().First(a=>a.isHuman && a.isValid)
            }).ToArray();
            catalog.walk=AssetDatabase.LoadAllAssetsAtPath(CharacterSetup.ModelPaths[0]).OfType<AnimationClip>().First(c=>c.humanMotion && !c.name.StartsWith("__preview"));
            EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();Debug.Log("Actor catalog ready: four humanoid avatars and Female Walk");
        }
    }
}
