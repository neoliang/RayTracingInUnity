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
    class AABB
    {
        public vec3 _min;
        public vec3 _max;
        
        public AABB(vec3 min,vec3 max )
        {
            _min = min;
            _max = max;
        }
        public static AABB SurroundingBox(AABB c0,AABB c1)
        {
            if(c0 == null || c1 == null)
            {
                return null;
            }

            vec3 min = new vec3();
            vec3 max = new vec3();
            for(int i = 0;i<3;++i)
            {
                min[i] = Exten.fmin(c0._min[i], c1._min[i]);
                max[i] = Exten.fmax(c0._max[i], c1._max[i]);
            }
            return new AABB(min, max);
        }
        public bool Hit(Ray ray,float min ,float max)
        {
            for(int i =0;i<3;++i)
            {
                float inv = 1.0f / ray.direction[i];
                float t0 = (_min[i] - ray.position[i]) * inv;
                float t1 = (_max[i] - ray.position[i]) * inv;
                if(inv < 0)
                {
                    float temp = t0;
                    t0 = t1;
                    t1 = temp;
                }
                min = Exten.fmax(t0, min);
                max = Exten.fmin(t1, max);
                if(min > max)
                {
                    return false;
                }

            }
            return true;
        }
    }
}
