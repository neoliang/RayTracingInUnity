using System;
using GlmNet;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
#if UNITY_EDITOR
using vec3 = UnityEngine.Vector3;
#else
using MathF = System.Math;
#endif
namespace RT1
{
    public static class Exten
    {
        public static readonly vec3 zero = new vec3();
        public static float length(this vec3 v)
        {
            return (float)MathF.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
        }
        public static vec3 gamma(this vec3 c)
        {
            return new vec3((float)MathF.Pow(c.x, 1.0f / 2.2f), (float)MathF.Pow(c.y, 1.0f / 2.2f), (float)MathF.Pow(c.z, 1.0f / 2.2f));
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
        public static float fmin(float f1, float f2)
        {
            return f1 < f2 ? f1 : f2;
        }
        public static float fmax(float f1, float f2)
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
                refracted = ni_over_nt * (uv - n * dt) - n * (float)MathF.Sqrt(discriminat);
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
                refracted = eta * v - (eta * dt + (float)MathF.Sqrt(partb1)) * n;

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
            vec3 n = new vec3();
            do
            {
                n = new vec3(randRange(-1.0f, 1.0f), randRange(-1.0f, 1.0f), randRange(-1.0f, 1.0f));
            }
            while (length(n) >= 1.0f);
            return n;
        }
        public static vec3 RandomCosineDir()
        {
            float r1 = rand01();
            float r2 = rand01();
            double z = Math.Sqrt(1 - r2);
            double phi = 2 * Math.PI * r1;
            double x = Math.Cos(phi)* Math.Sqrt(r2);
            double y = Math.Sin(phi)* Math.Sqrt(r2);
            return new vec3((float)x,(float) y, (float)z);
        }
        public static vec3 RandomUniformCosineDir()
        {
            double phi = rand01() * 2 * Math.PI;
            double theta = rand01() * Math.PI * 0.5;
            double z = Math.Cos(theta);
            double t = Math.Sin(theta);
            double x = Math.Cos(phi) * t;
            double y = Math.Sin(phi) * t;
            return new vec3((float)x, (float)y, (float)z);
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
        public static float randRange(float f, float t)
        {
            return rand01() * (t - f) + f;
        }
        public static int rand(int min,int max)
        {
            return r.Next(min, max);
        }
        public static float rand01()
        {
            return (float)r.NextDouble();
        }
    }
}
