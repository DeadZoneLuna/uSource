#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace uSource.Decals
{
    static class GUIUtils
    {
        public static LayerMask LayerMaskField(string label, LayerMask mask)
        {
            var names = Enumerable.Range(0, 32).Select(i => LayerMask.LayerToName(i))/*.Where( i => !string.IsNullOrEmpty( i ) )*/.ToArray(); // TODO: fix bug
            return EditorGUILayout.MaskField(label, mask.value, names);
        }
    }
}
#endif