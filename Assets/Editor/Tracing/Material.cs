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
    interface Material
    {
        bool Scatter(Ray ray, HitRecord hitRecord, out vec3 attenuation, out Ray scattered,out float pdf);
        float Scatter_PDf(Ray ray, HitRecord hitRecord, Ray scattered);
        vec3 Emitted(Ray ray, HitRecord hitRecord, float u, float v, vec3 pos);
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
    class NoiseTexture : Texture
    {
        Noise _noise;
        public NoiseTexture(Noise n)
        {
            _noise = n;
        }
        public vec3 sample(float u, float v, vec3 pos)
        {
            return new vec3(1, 1, 1) * 0.2f* _noise.turb(pos);// MathF.Sin(8.0f*pos.y + 25.0f*_noise.turb(pos));
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
            float r = MathF.Sin(20.0f * pos.x) * MathF.Sin(20.0f*pos.y) * MathF.Sin(pos.z*20.0f);
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
        public Lambertian(vec3 c)
        {
            color = new SolidTexture(c);
        }


        public vec3 Emitted(Ray ray, HitRecord hitRecord, float u, float v, vec3 pos)
        {
            return new vec3(0, 0, 0);
        }

        public bool Scatter(Ray ray, HitRecord hitRecord, out vec3 attenuation, out Ray scattered, out float pdf)
        {
            //half vector
            //scattered = new Ray(hitRecord.point, hitRecord.normal *0.5f + Exten.RandomHalfVecInSphere(), ray.time);
            ONB uvw = new ONB(hitRecord.normal);
            //var nextDir = uvw.Local(Exten.RandomCosineDir());
            var nextDir = uvw.Local(Exten.RandomUniformCosineDir());
            scattered = new Ray(hitRecord.point, nextDir, ray.time);
            attenuation = color.sample(hitRecord.u,hitRecord.v,hitRecord.point);
            pdf =  1.0f / (2.0f* MathF.PI);
            return true;
        }

        public float Scatter_PDf(Ray ray, HitRecord hitRecord, Ray scattered)
        {
            return glm.dot(hitRecord.normal, scattered.direction) /MathF.PI;
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
        public bool Scatter(Ray ray, HitRecord hitRecord, out vec3 attenuation, out Ray scattered,out float pdf)
        {
            pdf = 1;
            var normal = hitRecord.normal;
            var point = hitRecord.point;
            var reflected = Exten.reflect(ray.direction, normal);
            if (fuzz > 0)
            {
                reflected = reflected + fuzz * Exten.RandomVecInSphere();
            }
            scattered = new Ray(point, reflected, ray.time);
            attenuation = color;
            return glm.dot(ray.direction, normal) < 0;
        }
        public vec3 Emitted(Ray ray, HitRecord hitRecord, float u, float v, vec3 pos)
        {
            return new vec3(0, 0, 0);
        }

        public float Scatter_PDf(Ray ray, HitRecord hitRecord, Ray scattered)
        {
            return 1;
        }
    }
    class DiffuseLight : Material
    {
        private Texture _text;
        public DiffuseLight(Texture t)
        {
            _text = t;
        }
        public vec3 Emitted(Ray ray, HitRecord hitRecord, float u, float v, vec3 pos)
        {
            if(glm.dot(ray.direction,hitRecord.normal) > 0)
            {
                return  new vec3(0, 0, 0);
            }
            return _text.sample(u, v, pos);
        }

        public bool Scatter(Ray ray, HitRecord hitRecord, out vec3 attenuation, out Ray scattered,out float pdf)
        {
            scattered = null;
            attenuation = new vec3();
            pdf = 1;
            return false;
        }

        public float Scatter_PDf(Ray ray, HitRecord hitRecord, Ray scattered)
        {
            return 1;
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
        public vec3 Emitted(Ray ray, HitRecord hitRecord, float u, float v, vec3 pos)
        {
            return new vec3(0, 0, 0);
        }
        public bool Scatter(Ray ray, HitRecord hitRecord, out vec3 attenuation, out Ray scattered,out float pdf)
        {
            pdf = 1;
            var normal = hitRecord.normal;
            var point = hitRecord.point;
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

        public float Scatter_PDf(Ray ray, HitRecord hitRecord, Ray scattered)
        {
            return 1;
        }
    }
}
