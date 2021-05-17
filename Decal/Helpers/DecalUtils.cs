using System.Collections;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;

namespace uSource.Decals
{

    static class DecalUtils
    {
        public static MeshFilter[] GetAffectedObjects(Decal decal)
        {
            var bounds = GetBounds(decal);
            //var isOnlyStatic = decal.gameObject.isStatic;

            return GameObject.FindObjectsOfType<MeshRenderer>()
                .Where(i => i.GetComponent<Decal>() == null) // ignore another decals
                .Where(i => i.gameObject.isStatic == true)//!isOnlyStatic)
                .Where(i => HasLayer(decal.LayerMask, i.gameObject.layer))
                .Where(i => bounds.Intersects(i.bounds))

                .Select(i => i.GetComponent<MeshFilter>())
                .Where(i => i)
                .Where(i => i.sharedMesh)
                .ToArray();
        }

        public static Terrain[] GetAffectedTerrains(Decal decal)
        {
            var bounds = GetBounds(decal);
            var isOnlyStatic = decal.gameObject.isStatic;

            return Terrain.activeTerrains
                .Where(i => i.gameObject.isStatic || !isOnlyStatic)
                .Where(i => HasLayer(decal.LayerMask, i.gameObject.layer))
                .Where(i => bounds.Intersects(i.GetBounds()))
                .ToArray();
        }


        public static Bounds GetBounds(Decal decal)
        {
            var transform = decal.transform;
            var size = transform.lossyScale;
            var min = -size / 2f;
            var max = size / 2f;

            var vts = new Vector3[] {
                new Vector3( min.x, min.y, min.z ),
                new Vector3( max.x, min.y, min.z ),
                new Vector3( min.x, max.y, min.z ),
                new Vector3( max.x, max.y, min.z ),

                new Vector3( min.x, min.y, max.z ),
                new Vector3( max.x, min.y, max.z ),
                new Vector3( min.x, max.y, max.z ),
                new Vector3( max.x, max.y, max.z ),
            };

            vts = vts.Select(transform.TransformDirection).ToArray();
            min = vts.Aggregate(Vector3.Min);
            max = vts.Aggregate(Vector3.Max);

            return new Bounds(transform.position, max - min);
        }

        private static Bounds GetBounds(this Terrain terrain)
        {
            var bounds = terrain.terrainData.bounds;
            bounds.center += terrain.transform.position;
            return bounds;
        }


        public static void SetDirty(Decal decal)
        {
#if UNITY_EDITOR
            if (decal.gameObject.scene.IsValid())
            {
                if (!EditorApplication.isPlaying) EditorSceneManager.MarkSceneDirty(decal.gameObject.scene);
            }
            else
            {
                EditorUtility.SetDirty(decal.gameObject);
            }
#endif
        }


        public static void FixRatio(Decal decal, ref Vector3 oldScale)
        {
            var transform = decal.transform;
            var rect = decal.Sprite.rect;
            var ratio = (rect.width / rect.height);

            var scale = transform.localScale;
            FixRatio(ref scale, ref oldScale, ratio);

            var hasChanged = transform.hasChanged;
            transform.localScale = scale;
            transform.hasChanged = hasChanged;
        }

        public static void FixRatio(ref Vector3 scale, ref Vector3 oldScale, float ratio)
        {
            if (!Mathf.Approximately(oldScale.x, scale.x))
                scale.y = scale.x / ratio; // if scale.x was changed then fix scale.y
            else if (!Mathf.Approximately(oldScale.y, scale.y))
                scale.x = scale.y * ratio;
            else if (!Mathf.Approximately(scale.x / scale.y, ratio))
                scale.x = scale.y * ratio;

            oldScale = scale;
        }


        // Helpers
        private static bool HasLayer(LayerMask mask, int layer)
        {
            return (mask.value & 1 << layer) != 0;
        }


    }
}