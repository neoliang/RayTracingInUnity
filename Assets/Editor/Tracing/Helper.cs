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
    public static class Exten
    {
        public static readonly vec3 zero = new vec3();
        public static float length(this vec3 v)
        {
            return MathF.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
        }
        public static vec3 gamma(this vec3 c)
        {
            return new vec3(MathF.Pow(c.x, 1.0f / 2.2f), MathF.Pow(c.y, 1.0f / 2.2f), MathF.Pow(c.z, 1.0f / 2.2f));
        }

        public static vec3 mul(this vec3 c, vec3 d)
        {
            return new vec3(c.x * d.x, c.y * d.y, c.z * d.z);
        }

        public static vec3 vec(float x, float y, float z)
        {
            return new vec3(x, y, z);
        }
        public static vec3 reflect(vec3 v, vec3 n)
        {
            return v - 2.0f * glm.dot(v, n) * n;
        }
        public static float fmin(float f1,float f2)
        {
            return f1 < f2 ? f1 : f2;
        }
        public static float fmax(float f1,float f2)
        {
            return f1 > f2 ? f1 : f2;
        }
        public static bool refract1(vec3 v, vec3 n, float ni_over_nt, out vec3 refracted)
        {
            vec3 uv = glm.normalize(v);
            float dt = glm.dot(uv, n);
            float discriminat = 1.0f - ni_over_nt * ni_over_nt * (1 - dt * dt);
            if (discriminat > 0)
            {
                refracted = ni_over_nt * (uv - n * dt) - n * MathF.Sqrt(discriminat);
                return true;
            }
            else
            {
                refracted = new vec3();
                return false;
            }
        }
        public static bool refract(vec3 v, vec3 n, float eta, out vec3 refracted)
        {
            var dt = glm.dot(v, n);
            var partb1 = 1.0f - eta * eta * (1 - dt * dt);
            if (partb1 > 0)
            {
                refracted = eta * v - (eta * dt + MathF.Sqrt(partb1)) * n;

                return true;
            }
            else
            {
                refracted = new vec3();
                return false;
            }

        }
        public static vec3 RandomHalfVecInSphere()
        {
            vec3 n = 2.0f * vec(rand01(), rand01(), rand01()) - vec(1.0f, 1.0f, 1.0f);
            n = glm.normalize(n) / 2.0f;
            return n;
        }
        public static vec3 RandomVecInSphere()
        {
            vec3 n;
            do
            {
                n = 2.0f * vec(rand01(), rand01(), rand01()) - vec(1.0f, 1.0f, 1.0f);
            }
            while (n.length() >= 1.0f);
            return n;
        }
        public static float clamp01(float v)
        {
            if (v < 0) return 0;
            if (v > 1) return 1;
            return v;
        }
        public static vec3 clamp01(vec3 color)
        {
            return new vec3(clamp01(color.x), clamp01(color.y), clamp01(color.z));
        }

        public static Random r = new Random();
        public static float rand01()
        {
            const int m = 100000;
            float x = r.Next() % m;
            return x / m;
        }
    }
}
