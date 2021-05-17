using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System;

namespace uSource.Formats.Source.VBSP
{
    public class EntInfo : MonoBehaviour
    {
        public List<string> Data;

        void OnDrawGizmos()
        {
            Gizmos.DrawCube(transform.position, Vector3.one / 5f);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawCube(transform.position, Vector3.one / 5f);
        }

        public void Configure(List<String> Data)
        {
            this.Data = Data;
            transform.Configure(this.Data);
        }
    }
}