using System.Collections.Generic;
using HanaMart.Player;
using HanaMart.Store;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace HanaMart.EditorTools
{
    public static class NavigationSetup
    {
        public const string AssetPath = "Assets/HanaMart/Data/StoreNavigation.asset";

        [MenuItem("HanaMart/Build Store Navigation")]
        public static void Run()
        {
            if (Application.isPlaying) throw new System.InvalidOperationException("Bake navigation in Edit mode.");
            var scene = EditorSceneManager.OpenScene(StoreImporter.ScenePath, OpenSceneMode.Single);
            var player = Camera.main.GetComponent<PlayerController>();
            var grid = new StoreNavigation(player.collisionBoxes);
            var mesh = grid.CreateSurfaceMesh();
            try
            {
                var sources = new List<NavMeshBuildSource>
                {
                    new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Mesh, sourceObject = mesh, transform = Matrix4x4.identity },
                };
                var settings = NavMesh.GetSettingsByIndex(0);
                // The grid already keeps NPC centers .24 m from store obstacles.
                // Only a small contour inset is needed when baking these center positions.
                settings.agentRadius = .01f;
                settings.agentHeight = 1.8f;
                settings.minRegionArea = 0;
                settings.overrideVoxelSize = true;
                settings.voxelSize = .025f;
                var bounds = mesh.bounds;
                bounds.Expand(.2f);
                var baked = NavMeshBuilder.BuildNavMeshData(settings, sources, bounds, Vector3.zero, Quaternion.identity);
                if (!baked) throw new System.InvalidOperationException("Store navigation bake failed.");

                var existing = AssetDatabase.LoadAssetAtPath<NavMeshData>(AssetPath);
                if (existing)
                {
                    EditorUtility.CopySerialized(baked, existing);
                    Object.DestroyImmediate(baked);
                    EditorUtility.SetDirty(existing);
                }
                else
                {
                    existing = baked;
                    AssetDatabase.CreateAsset(existing, AssetPath);
                }
                player.storeNavMesh = existing;
                EditorUtility.SetDirty(player);
                AssetDatabase.SaveAssets();
                EditorSceneManager.SaveScene(scene);
            }
            finally { Object.DestroyImmediate(mesh); }
        }
    }
}
