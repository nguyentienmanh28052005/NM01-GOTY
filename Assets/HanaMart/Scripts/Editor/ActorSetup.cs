using System.Linq;
using System.IO;
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
            var previous=catalog.models;
            catalog.models=CharacterSetup.ModelPaths.Select((path,index)=>new ActorCatalog.Entry
            {
                id=ids[index],model=AssetDatabase.LoadAssetAtPath<GameObject>(path),avatar=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().First(a=>a.isHuman && a.isValid),
                renderers=previous.FirstOrDefault(e=>e.id==ids[index])?.renderers ?? System.Array.Empty<ActorCatalog.RendererMaterials>()
            }).ToArray();
            catalog.walk=AssetDatabase.LoadAllAssetsAtPath(CharacterSetup.ModelPaths[0]).OfType<AnimationClip>().First(c=>c.humanMotion && !c.name.StartsWith("__preview"));
            EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();Debug.Log("Actor catalog ready: four humanoid avatars and Female Walk");
        }

        [MenuItem("HanaMart/Build Actor Materials")]
        public static void BuildMaterials()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<ActorCatalog>(CatalogPath);
            var shader=Shader.Find("HanaMart/Character");
            if(!shader)throw new System.InvalidOperationException("Character shader missing; run tools/build_character_shader.py and refresh");
            foreach(var entry in catalog.models)
            {
                string modelPath=AssetDatabase.GetAssetPath(entry.model);
                byte[] originalMeta=File.ReadAllBytes(modelPath+".meta");
                var importer=(ModelImporter)AssetImporter.GetAtPath(modelPath);
                bool needsReadable=!importer.isReadable;
                GameObject model=null;
                try
                {
                    if(needsReadable){importer.isReadable=true;importer.SaveAndReimport();}
                    model=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(modelPath));model.hideFlags=HideFlags.HideAndDontSave;
                    var renderers=model.GetComponentsInChildren<SkinnedMeshRenderer>(true);var bounds=renderers[0].bounds;
                    foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                    float normalize=1.808f/Mathf.Max(.01f,bounds.size.y);
                    entry.renderers=renderers.Select(r=>
                    {
                        string folder=$"Assets/HanaMart/Art/Characters/Variants/{entry.id}";Directory.CreateDirectory(folder);
                        var mats=r.sharedMaterials;var body=new Material[mats.Length];var rain=new Material[mats.Length];
                        for(int i=0;i<mats.Length;i++)
                        {
                            var original=mats[i];bool hair=original.name.ToLowerInvariant().Contains("hair") || r.name.ToLowerInvariant().Contains("hair");
                            if(hair){body[i]=original;rain[i]=original;continue;}
                            string prefix=$"{folder}/{r.name}_{i}";
                            var mask=ClothingMask.Bake(r,i,original.GetTexture("_BaseMap"),prefix+"_Regions.png");
                            var material=new Material(original){shader=shader,name=entry.id+"_"+original.name};
                            material.SetTexture("_RegionMask",mask);material.SetFloat("_ObjectUnit",Mathf.Abs(r.transform.lossyScale.x)*normalize);
                            material.SetFloat("_TintStrength",0);body[i]=SaveMaterial(material,prefix+".mat");
                            var shell=new Material(body[i]){name=body[i].name+"_Rain"};shell.SetFloat("_Raincoat",1);shell.SetFloat("_ShellDistance",.012f);shell.EnableKeyword("_CLEARCOAT");
                            shell.SetColor("_CoatColor",new Color32(0xf2,0xc2,0x00,0xff));rain[i]=SaveMaterial(shell,prefix+"_Rain.mat");
                        }
                        return new ActorCatalog.RendererMaterials{path=AnimationUtility.CalculateTransformPath(r.transform,model.transform),materials=body,rainMaterials=rain};
                    }).ToArray();
                }
                finally
                {
                    if(model)Object.DestroyImmediate(model);
                    if(needsReadable)
                    {
                        // Preserve the exact user's importer data after the editor-only bake.
                        File.WriteAllBytes(modelPath+".meta",originalMeta);
                        AssetDatabase.ImportAsset(modelPath,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
                    }
                    entry.model=AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                    entry.avatar=AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Avatar>().First(a=>a.isHuman && a.isValid);
                }
            }
            catalog.walk=AssetDatabase.LoadAllAssetsAtPath(CharacterSetup.ModelPaths[0]).OfType<AnimationClip>().First(c=>c.humanMotion && !c.name.StartsWith("__preview"));
            EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();Debug.Log("Character region masks, body materials and rain shells ready");
        }
        static Material SaveMaterial(Material material,string path)
        {
            var previous=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(previous){EditorUtility.CopySerialized(material,previous);Object.DestroyImmediate(material);EditorUtility.SetDirty(previous);return previous;}
            AssetDatabase.CreateAsset(material,path);return material;
        }
    }
}
