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
    class ScatterRecord
    {
        public vec3 attenuation;
        public PDF pdf;
    }
    interface Material
    {
        bool Scatter(Ray ray, HitRecord hitRecord, out ScatterRecord sRecord);
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

        public bool Scatter(Ray ray, HitRecord hitRecord, out ScatterRecord sRecord)
        {
            sRecord = new ScatterRecord();
            //half vector
            //scattered = new Ray(hitRecord.point, hitRecord.normal *0.5f + Exten.RandomHalfVecInSphere(), ray.time);
            //ONB uvw = new ONB(hitRecord.normal);
            //var nextDir = uvw.Local(Exten.RandomCosineDir());
            //var nextDir = uvw.Local(Exten.RandomUniformCosineDir());
            //scattered = new Ray(hitRecord.point, nextDir, ray.time);
            sRecord.attenuation = color.sample(hitRecord.u,hitRecord.v,hitRecord.point);
            sRecord.pdf = CosinePDF.Default;
            return true;
        }

        public float Scatter_PDf(Ray ray, HitRecord hitRecord, Ray scattered)
        {
            return glm.dot(hitRecord.normal, scattered.direction) /MathF.PI;
        }
    }
    class ConstPDF : PDF
    {
        private vec3 _reflected;

        public ConstPDF(vec3 nextDir)
        {
            _reflected = nextDir;
        }
        public vec3 Generate(vec3 point, vec3 normal)
        {
            return _reflected;
        }

        public float Value(Ray ray, vec3 normal)
        {
            return 1;
        }
        public bool IsConst()
        {
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
        public  vec3 RandomCosineDir()
        {
            float r1 = Exten.rand01();
            float r2 = Exten.rand01() / fuzz;
            r2 = (float)Math.Pow(2.72, -r2*r2);
            double z = Math.Sqrt(1 - r2);
            double phi = 2 * Math.PI * r1;
            double x = Math.Cos(phi) * Math.Sqrt(r2);
            double y = Math.Sin(phi) * Math.Sqrt(r2);
            return new vec3((float)x, (float)y, (float)z);
        }
        public bool Scatter(Ray ray, HitRecord hitRecord, out ScatterRecord sRecord)
        {
            sRecord = new ScatterRecord();
            var normal = hitRecord.normal;
            var point = hitRecord.point;
            var reflected = Exten.reflect(ray.direction, normal);
            if (fuzz > 0)
            {
                ONB o = new ONB(reflected);
                var next = RandomCosineDir();

                reflected = o.Local(next);
            }
            sRecord.attenuation = color;
            sRecord.pdf = new ConstPDF(reflected);
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

        public bool Scatter(Ray ray, HitRecord hitRecord, out ScatterRecord sRecord)
        {
            sRecord = null;
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
        public bool Scatter(Ray ray, HitRecord hitRecord, out ScatterRecord sRecord)
        {
            sRecord = new ScatterRecord();
            var normal = hitRecord.normal;
            var point = hitRecord.point;
            sRecord.attenuation = new vec3(1.0f, 1.0f, 1.0f);
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
                sRecord.pdf = new ConstPDF(reflected);
            }
            else
            {
                sRecord.pdf = new ConstPDF(r);
            }

            return true;

        }

        public float Scatter_PDf(Ray ray, HitRecord hitRecord, Ray scattered)
        {
            return 1;
        }
    }
}
