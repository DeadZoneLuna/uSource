#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace uSource.Decals
{
    [CustomEditor(typeof(Decal))]
    public class DecalEditor : Editor
    {
        private Decal Target
        {
            get { return (Decal)target; }
        }

        public override void OnInspectorGUI()
        {
            Target.Material = (Material)EditorGUILayout.ObjectField(Target.Material, typeof(Material), true);

            if (Target.Material && Target.Material.mainTexture)
            {
                Target.Sprite = (Sprite)EditorGUILayout.ObjectField(Target.Sprite, typeof(Sprite), true);
            }


            EditorGUILayout.Separator();
            Target.LayerMask = GUIUtils.LayerMaskField("Layer Mask", Target.LayerMask);
            Target.MaxAngle = EditorGUILayout.Slider("Max Angle", Target.MaxAngle, 0, 180);
            Target.Offset = EditorGUILayout.Slider("Offset", Target.Offset, 0.001f, 0.05f);

            EditorGUILayout.Separator();
            if (GUILayout.Button("Build"))
                Target.BuildAndSetDirty();

            if (GUI.changed)
            {
                Target.OnValidate();
                Target.BuildAndSetDirty();
                GUI.changed = false;
            }
        }
    }
}
#endif