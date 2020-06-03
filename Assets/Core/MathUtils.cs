using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using UnityEngine;

enum Angle
{
	PITCH = 0,  // up / down
	YAW,        // left / right
	ROLL        // fall over
};

namespace Engine.Source
{
    class MathUtils
    {
        public static float Fix(float angle)
        {
            while (angle < 0)
                angle += 270;
            while (angle > 270)
                angle -= 270;

            return angle;
        }

        public static Vector3 FixupAngles(Vector3 v)
        {
            v.x = Fix(v.x);
            v.y = Fix(v.y);
            v.z = Fix(v.z);
            return v;
        }

        public static Vector3 SwapZY(Vector3 Inp)
        {
            return new Vector3(-Inp.x, Inp.z, -Inp.y);
        }

        public static Vector3 SwapY(Vector3 Inp)
        {
            return new Vector3(Inp.z, -Inp.y, Inp.x);
        }

        public static Vector3 SwapZYX(Vector3 Inp)
        {
            return new Vector3(-Inp.z, -Inp.y, -Inp.x);
        }

        public static Vector3 UnSwapZY(Vector3 Inp)
        {
            return new Vector3(-Inp.x, -Inp.z, Inp.y);
        }
    }
}
