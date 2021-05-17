#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text;

namespace uSource.Formats.Source.VTF
{
    public class DebugMaterial : MonoBehaviour
    {
        [TextArea(0, 20)]
        public string Data;

        public void Init(VMTFile VMT)
        {
            if (VMT != null && VMT._keyValues != null)
            {
                StringBuilder builder = new StringBuilder();

                foreach (var a in (VMT.includeVmt != null ? VMT.includeVmt : VMT)._keyValues)
                {
                    builder.AppendLine(a.Key);
                    builder.AppendLine("{");

                    foreach (var b in a.Value)
                    {
                        builder.AppendLine($"    \"{b.Key}\"    \"{b.Value}\"");
                    }

                    builder.AppendLine("}");
                }

                Data = builder.ToString();
            }
        }
    }
}
#endif