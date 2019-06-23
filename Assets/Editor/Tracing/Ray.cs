using System;
using GlmNet;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
#if UNITY_EDITOR
using vec3 = UnityEngine.Vector3;
#endif
namespace RT1
{
    class Ray
    {
        public vec3 position;
        public vec3 direction;
        public float time;
        public Ray(vec3 pos, vec3 dir, float t)
        {
            position = pos;
            direction = glm.normalize(dir);
            time = t;
        }
        public vec3 at(float t)
        {
            return position + t * direction;
        }
        public Ray()
        {
            position = new vec3(0, 0, 0);
            direction = new vec3(0, 0, 0);
        }
    }
}
