using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace uSource.Decals
{
    static class PolygonUtils
    {
        private static readonly Plane Right = new Plane(Vector3.right, 0.5f);
        private static readonly Plane Left = new Plane(Vector3.left, 0.5f);

        private static readonly Plane Top = new Plane(Vector3.up, 0.5f);
        private static readonly Plane Bottom = new Plane(Vector3.down, 0.5f);

        private static readonly Plane Front = new Plane(Vector3.forward, 0.5f);
        private static readonly Plane Back = new Plane(Vector3.back, 0.5f);

        public static Vector3[] Clip(params Vector3[] poly)
        {
            poly = Clip(poly, Right).ToArray();
            poly = Clip(poly, Left).ToArray();
            poly = Clip(poly, Top).ToArray();
            poly = Clip(poly, Bottom).ToArray();
            poly = Clip(poly, Front).ToArray();
            poly = Clip(poly, Back).ToArray();
            return poly;
        }

        private static IEnumerable<Vector3> Clip(Vector3[] poly, Plane plane)
        {
            for (var i = 0; i < poly.Length; i++)
            {
                var next = (i + 1) % poly.Length;
                var v1 = poly[i];
                var v2 = poly[next];

                if (plane.GetSide(v1))
                {
                    yield return v1;
                }

                if (plane.GetSide(v1) != plane.GetSide(v2))
                {
                    yield return PlaneLineCast(plane, v1, v2);
                }
            }
        }

        // Helpers
        private static Vector3 PlaneLineCast(Plane plane, Vector3 a, Vector3 b)
        {
            float dis;
            var ray = new Ray(a, b - a);
            plane.Raycast(ray, out dis);
            return ray.GetPoint(dis);
        }
    }
}