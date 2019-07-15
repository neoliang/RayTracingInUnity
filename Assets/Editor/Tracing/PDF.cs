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
    interface PDF    
    {
        float Value(Ray ray, vec3 normal);
        vec3 Generate(vec3 point,vec3 normal);
        bool IsConst();
    }
    class CosinePDF : PDF
    {
        public vec3 Generate(vec3 point, vec3 normal)
        {
            var rd = Exten.RandomCosineDir();
            var oNB = new ONB(normal);
            return oNB.Local(rd);
        }

        public float Value(Ray ray, vec3 normal)
        {
            var cos = glm.dot(normal, ray.direction);
            if (cos <= 0)
            {
                return 0;
            }
            return cos /MathF.PI;
        }

        public bool IsConst()
        {
            return false;
        }

        public static PDF Default = new CosinePDF();
    }
    class SpherePDF : PDF
    {
        Sphere _sphere;
        public SpherePDF(Sphere s)
        {
            _sphere = s;
        }
        public vec3 Generate(vec3 point, vec3 normal)
        {
            float r1 = Exten.rand01();
            float r2 = Exten.rand01();

            var pc = _sphere.GetCenter(0) - point;
            float cosMax = 1 - 2.0f * (_sphere.radius * _sphere.radius) / glm.dot(pc, pc);
            float z = 1 - r2 * (1 - cosMax);
            float phi = r1 * 2.0f * MathF.PI;
            float x = MathF.Cos(phi) * MathF.Sqrt(1 - z * z);
            float y = MathF.Sin(phi) * MathF.Sqrt(1 - z * z);
            ONB oNB = new ONB(glm.normalize(pc));
            return oNB.Local(x, y, z);
        }

        public bool IsConst()
        {
            return false;
        }

        public float Value(Ray ray, vec3 normal)
        {
            var pc = _sphere.GetCenter(0) - ray.position;
            float cosMax = 1 - 2.0f * (_sphere.radius * _sphere.radius) / glm.dot(pc, pc);
            return 1.0f / ((1 - cosMax * cosMax) * MathF.PI);
        }
    }
    class LightPDF : PDF
    {
        //hard code 
        XZRect _lightArea;
        public LightPDF(XZRect rc)
        {
            _lightArea = rc;
        }
        public vec3 Generate(vec3 point, vec3 normal)
        {
            var x = Exten.randRange( _lightArea._x1 ,_lightArea._x2);
            var z = Exten.randRange(_lightArea._z1, _lightArea._z2);
            var onLight = new vec3(x, _lightArea._y, z);
            return glm.normalize(onLight - point);
        }

        public bool IsConst()
        {
            return false;
        }

        public float Value(Ray ray, vec3 normal)
        {
            HitRecord record;
            if(_lightArea.Hit(ray,0.0001f,10000,out record))
            {
                float cos = ray.direction.y;
                float area = (_lightArea._x2 - _lightArea._x1) * (_lightArea._z2 - _lightArea._z1);
                float distance = (record.point - ray.position).length();
                return distance * distance / (area * cos);
            }
            return 0;


        }
    }
    class MixPDF : PDF
    {
        System.Collections.Generic.List<PDF> _pdfs;

        public MixPDF(params PDF[] pDFs)
        {
            _pdfs = new System.Collections.Generic.List<PDF>(pDFs);
        }
        public vec3 Generate(vec3 point, vec3 normal)
        {
            var idx = Exten.rand(0, _pdfs.Count);
            return _pdfs[idx].Generate(point, normal);
        }

        public float Value(Ray ray, vec3 normal)
        {
            float d = 1.0f / _pdfs.Count;
            float v = 0;
            for(int i = 0;i<_pdfs.Count;++i)
            {
                v += _pdfs[i].Value(ray, normal) * d;
            }
            return v;
        }
        public bool IsConst()
        {
            return false;
        }
    }
}
