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
        PDF _pdf1;
        PDF _pdf2;

        public MixPDF(PDF pdf1,PDF pdf2)
        {
            _pdf1 = pdf1;
            _pdf2 = pdf2;
        }
        public vec3 Generate(vec3 point, vec3 normal)
        {
            if(Exten.rand01() <= 0.5)
            {
                return _pdf1.Generate(point,normal);
            }
            return _pdf2.Generate(point,normal);
        }

        public float Value(Ray ray, vec3 normal)
        {
            return 0.5f * _pdf1.Value(ray, normal) + 0.5f * _pdf2.Value(ray, normal);
        }
        public bool IsConst()
        {
            return false;
        }
    }
}
