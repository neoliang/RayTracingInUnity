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
    class HitRecord
    {
        public float t;
        public vec3 point;
        public vec3 normal;
        public Material mat;
        public float u, v;

        public HitRecord()
        {
            u = 0;
            v = 0;
        }
    }
    interface Hitable
    {
        bool Hit(Ray ray, float min, float max, out HitRecord r);
        AABB BoundVolume(float t0, float t1);
    }

    class Sphere : Hitable
    {
        private vec3 center;
        private vec3 center1;
        float _time1;
        float _time2;
        bool _isMoving = false;
        public float radius;
        public Material mat;
        public virtual vec3 GetCenter(float time)
        {
            if (!_isMoving)
            {
                return center;
            }
            return center + (center1 - center) * (time - _time1) / (_time2 - _time1);
        }
        public Sphere(vec3 c, float r, Material m)
        {
            mat = m;
            center = c;
            radius = r;
            _isMoving = false;
        }
        public Sphere(vec3 c, vec3 c1, float r, Material m, float time1, float time2)
        {
            mat = m;
            center = c;
            radius = r;
            _isMoving = true;
            _time1 = time1;
            _time2 = time2;
            center1 = c1;
        }
        public AABB BoundVolume(float t0, float t1)
        {
            var offset = new vec3(radius, radius, radius);
            if (!_isMoving)
            {                
                return new AABB(center - offset,center+offset);
            }
            else
            {
                var c0 = GetCenter(t0);
                var b0 = new AABB( c0 - offset, c0 + offset);
                var c1 = GetCenter(t1);
                var b1 = new AABB(c1 - offset, c1 + offset);
                return AABB.SurroundingBox(b0, b1);
            }
        }
        public bool Hit(Ray ray, float min, float max, out HitRecord r)
        {
            //var hitpoint = r.position + r.direction * t;
            //(r.position + r.direction * t - center)*(r.position + r.direction * t - center) = radius*radius

            //(delta + r.dir * t)*(delta+r.dir*t) = r^2
            //delta*delta + 2*delta*r.dir*t+  t^2 *r.dir*r.dir = r^2
            vec3 origin = GetCenter(ray.time);
            r = null;
            var oc = ray.position - origin;
            var a = glm.dot(ray.direction, ray.direction);
            var b = 2.0f * glm.dot(oc, ray.direction);
            var c = glm.dot(oc, oc) - radius * radius;
            var delta = b * b - 4 * a * c;
            if (delta < 0)
            {
                return false;
            }

            var t = (-b - MathF.Sqrt(delta)) / (2 * a);
            if (t < min || t > max)
            {
                return false;
            }
            r = new HitRecord();
            r.t = t;
            r.point = ray.at(t);
            r.normal = (r.point - origin) / radius;
            r.mat = mat;
            return true;
        }
    }
    class XYRect : Hitable
    {
        float _z;
        float _x1, _x2;
        float _y1, _y2;
        Material _mat;
        public XYRect(float z,float x1,float x2,float y1,float y2,Material mat)
        {
            _z = z;
            _x1 = x1;
            _x2 = x2;
            _y1 = y1;
            _y2 = y2;
            _mat = mat;
        }
        public AABB BoundVolume(float t0, float t1)
        {
            return new AABB(new vec3(_x1, _y1, _z - 0.001f), new vec3(_x2, _y2, _z + 0.001f));
        }

        public bool Hit(Ray ray, float min, float max, out HitRecord r)
        {
            r = null;
            float t = (_z - ray.direction.z) / ray.position.z;
            if(t < min || t > max)
            {
                return false;
            }
            float x = ray.position.x + ray.direction.x * t;
            float y = ray.position.y + ray.direction.y * t;
            bool hit = _x1 <= x && x <= _x2 && _y1 <= y && y <= _y2;
            if(!hit)
            {
                return false;
            }
            r = new HitRecord();
            r.t = t;
            r.point = ray.at(t);
            r.normal = new vec3(0, 0, 1);
            r.mat = _mat;
            return true;
        }
    }
    class HitList : Hitable
    {
        Hitable[] _hitables;
        public HitList(Hitable[] ls)
        {
            _hitables = ls;
        }
        public bool Hit(Ray ray, float min, float max, out HitRecord record)
        {
            HitRecord nearestRecord = null;
            foreach (var h in _hitables)
            {
                HitRecord tempr;
                if (h.Hit(ray, min, max, out tempr))
                {
                    if (nearestRecord == null || nearestRecord.t > tempr.t)
                    {
                        nearestRecord = tempr;
                    }
                }
            }
            record = nearestRecord;
            return nearestRecord != null;
        }
        public AABB BoundVolume(float t0,float t1)
        {
            if(_hitables == null || _hitables.Length < 1)
            {
                return null;
            }
            var box = _hitables[0].BoundVolume(t0, t1);
            if(box == null)
            {
                return null;
            }
            for(int i = 1;i<_hitables.Length;++i)
            {
                var tempBox = _hitables[i].BoundVolume(t0, t1);
                if(tempBox == null)
                {
                    return null;
                }
                box = AABB.SurroundingBox(box,tempBox);
            }
            return box;
        }
    }
}
