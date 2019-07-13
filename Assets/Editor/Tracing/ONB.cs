using System;
using GlmNet;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
#if UNITY_EDITOR
using UnityEngine;
using vec3 = UnityEngine.Vector3;
#endif
namespace RT1
{

    public class ONB
    {
        public vec3 u;
        public vec3 v;
        public vec3 w;
        public ONB(vec3 normal)
        {
            vec3 a = Math.Abs(normal.x) > 0.9f ? new vec3(0, 1, 0) : new vec3(1,0,0);
            w = glm.normalize(normal);
            v = glm.normalize( glm.cross(w, a));
            u = glm.cross(w,v);
        }
        public vec3 Local(vec3 i)
        {
            return u * i.x + v * i.y + w * i.z;
        }
        public vec3 Local(float a,float b ,float c)
        {
            return u * a + v * b + w * c;
        }
    }
}