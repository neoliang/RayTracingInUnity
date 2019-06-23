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
    interface Material
    {
        bool Scatter(Ray ray, vec3 point, vec3 normal, out vec3 attenuation, out Ray scattered);
    }
    interface Texture
    {
        vec3 sample(float u, float v, vec3 pos);
    }
    class SolidTexture : Texture
    {
        vec3 color;
        public SolidTexture(vec3 c)
        {
            color = c;
        }
        public vec3 sample(float u, float v, vec3 pos)
        {
            return color;
        }
    }
    public class CheckTexture : Texture
    {
        vec3 color1;
        vec3 color2;
        public CheckTexture(vec3 c1,vec3 c2)
        {
            color1 = c1;
            color2 = c2;
        }
        public vec3 sample(float u, float v, vec3 pos)
        {
            float r = MathF.Sin(10.0f * pos.x) * MathF.Sin(10.0f * pos.y) * MathF.Sin(10.0f * pos.z);
            if(r <0)
            {
                return color1;
            }
            return color2;
        }
    }
    class Lambertian : Material
    {
        public Texture color;
        public Lambertian(Texture c)
        {
            color = c;
        }
        public bool Scatter(Ray ray, vec3 point, vec3 normal, out vec3 attenuation, out Ray scattered)
        {

            scattered = new Ray(point, normal + Exten.RandomVecInSphere(), ray.time);
            attenuation = color.sample(0,0,point);
            return true;
        }
    }
    class Metal : Material
    {
        public vec3 color;
        private float fuzz;
        public Metal(vec3 c, float f = -1.0f)
        {
            color = c;
            fuzz = f;
        }
        public bool Scatter(Ray ray, vec3 point, vec3 normal, out vec3 attenuation, out Ray scattered)
        {
            var reflected = Exten.reflect(ray.direction, normal);
            if (fuzz > 0)
            {
                reflected = reflected + fuzz * Exten.RandomVecInSphere();
            }
            scattered = new Ray(point, reflected, ray.time);
            attenuation = color;
            return glm.dot(ray.direction, normal) < 0;
        }
    }
    class Dieletric : Material
    {
        float ref_idx;
        public Dieletric(float ri)
        {
            ref_idx = ri;
        }

        float schlick(float cosine, float ref_idx)
        {
            float r0 = (1 - ref_idx) / (1 + ref_idx);
            r0 = r0 * r0;
            float c2 = cosine * cosine;
            return r0 + (1 - r0) * c2 * c2 * cosine;
        }
        float fresnel(float cosi, float cost, float eta)
        {
            float rs = (cosi - eta * cost) / (cosi + eta * cost);
            float rp = (eta * cosi - cost) / (eta * cosi + cost);
            return (rs * rs + rp * rp) * 0.5f;
        }
        public bool Scatter(Ray ray, vec3 point, vec3 normal, out vec3 attenuation, out Ray scattered)
        {
            attenuation = new vec3(1.0f, 1.0f, 1.0f);
            float eta = ref_idx;
            var n = normal;


            if (glm.dot(ray.direction, normal) > 0)//从内到外
            {
                n = new vec3(-normal.x, -normal.y, -normal.z);
            }
            else
            {
                eta = 1.0f / eta;
            }

            vec3 r;
            float prob = 1.0f;
            if (Exten.refract(ray.direction, n, eta, out r))
            {
                float cosi = -glm.dot(ray.direction, n);
                float cost = -glm.dot(r, n);
                prob = fresnel(cosi, cost, eta);
            }
            if (Exten.rand01() < prob)
            {
                vec3 reflected = Exten.reflect(ray.direction, normal);
                scattered = new Ray(point, reflected, ray.time);
            }
            else
            {
                scattered = new Ray(point, r, ray.time);
            }
            return true;

        }
    }
}
