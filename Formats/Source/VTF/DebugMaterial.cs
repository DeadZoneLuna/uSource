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

        StringBuilder builder;
        public void Init(VMTFile VMT)
        {
            if (VMT != null && VMT.KeyValues != null)
            {
                if (builder == null)
                    builder = new StringBuilder();

                foreach (var a in (VMT.Include != null ? VMT.Include : VMT).KeyValues)
                {
                    builder.AppendLine(VMT.FileName);
                    builder.AppendLine(a.Key);
                    builder.AppendLine("{");

                    foreach (var b in a.Value)
                    {
                        builder.AppendLine($"    \"{b.Key}\"    \"{b.Value}\"");
                    }

                    builder.AppendLine("}");
                }

                Data += builder.ToString();
            }
        }
    }
}
#endif