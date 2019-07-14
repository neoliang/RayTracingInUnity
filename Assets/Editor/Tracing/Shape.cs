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
        static void CalUV(vec3 point,out float u ,out float v)
        {
            float theta = (float)Math.Acos(point.z);
            float phi = (float)Math.Atan2(point.y, point.x);
            u = (theta + MathF.PI/2.0f) /  MathF.PI;
            v = (phi + MathF.PI) / (2.0f * MathF.PI);
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
            CalUV(r.normal, out r.u, out r.v);
            return true;
        }
    }
    class XZRect : Hitable
    {
        public float _y;
        public float _x1, _x2;
        public float _z1, _z2;
        Material _mat;
        bool _flipNormal;
        public XZRect(float y, float x1, float x2, float z1, float z2, Material mat,bool flipNormal=false)
        {
            _y = y;
            _x1 = x1;
            _x2 = x2;
            _z1 = z1;
            _z2 = z2;
            _mat = mat;
            _flipNormal = flipNormal;
        }
        public AABB BoundVolume(float t0, float t1)
        {
            return new AABB(new vec3(_x1, _y-0.001f, _z1), new vec3(_x2, _y+0.001f, _z2));
        }

        public bool Hit(Ray ray, float min, float max, out HitRecord r)
        {
            r = null;
            float t = (_y - ray.position.y) / ray.direction.y;
            if (t < min || t > max)
            {
                return false;
            }
            float x = ray.position.x + ray.direction.x * t;
            float z = ray.position.z + ray.direction.z * t;
            bool hit = _x1 <= x && x <= _x2 && _z1 <= z && z <= _z2;
            if (!hit)
            {
                return false;
            }
            r = new HitRecord();
            r.t = t;
            r.point = ray.at(t);
            r.normal = new vec3(0, _flipNormal ? -1: 1, 0);
            r.mat = _mat;
            r.u = (x - _x1) / (_x2 - _x1);
            r.v = (z - _z1) / (_z2 - _z1);
            return true;
        }
    }
    class XYRect : Hitable
    {
        float _z;
        float _x1, _x2;
        float _y1, _y2;
        Material _mat;
        bool _flipNormal;
        public XYRect(float z,float x1,float x2,float y1,float y2,Material mat,bool flipNormal = false)
        {
            _z = z;
            _x1 = x1;
            _x2 = x2;
            _y1 = y1;
            _y2 = y2;
            _mat = mat;
            _flipNormal = flipNormal;
        }
        public AABB BoundVolume(float t0, float t1)
        {
            return new AABB(new vec3(_x1, _y1, _z - 0.001f), new vec3(_x2, _y2, _z + 0.001f));
        }

        public bool Hit(Ray ray, float min, float max, out HitRecord r)
        {
            r = null;
            float t = (_z - ray.position.z) / ray.direction.z;
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
            r.normal = new vec3(0, 0, _flipNormal ? -1:1);
            r.mat = _mat;
            r.u = (x - _x1) / (_x2 - _x1);
            r.v = (y - _y1) / (_y2 - _y1);
            return true;
        }
    }

    class YZRect : Hitable
    {
        float _x;
        float _z1, _z2;
        float _y1, _y2;
        Material _mat;
        bool _flipNormal;
        public YZRect(float x, float y1, float y2, float z1, float z2, Material mat, bool flipNormal = false)
        {
            _x = x;
            _z1 = z1;
            _z2 = z2;
            _y1 = y1;
            _y2 = y2;
            _mat = mat;
            _flipNormal = flipNormal;
        }
        public AABB BoundVolume(float t0, float t1)
        {
            return new AABB(new vec3(_x-0.001f, _y1, _z1), new vec3(_x+0.001f, _y2, _z2));
        }

        public bool Hit(Ray ray, float min, float max, out HitRecord r)
        {
            r = null;
            float t = (_x - ray.position.x) / ray.direction.x;
            if (t < min || t > max)
            {
                return false;
            }
            float z = ray.position.z + ray.direction.z * t;
            float y = ray.position.y + ray.direction.y * t;
            bool hit = _z1 <= z && z <= _z2 && _y1 <= y && y <= _y2;
            if (!hit)
            {
                return false;
            }
            r = new HitRecord();
            r.t = t;
            r.point = ray.at(t);
            r.normal = new vec3(_flipNormal ? -1 : 1, 0,0 );
            r.mat = _mat;
            r.u = (y - _y1) / (_y2 - _y1);
            r.v = (z - _z1) / (_z2 - _z1);
            return true;
        }
    }
    class Box : Hitable
    {
        AABB _aabb;
        HitList _sideList;
        public Box(vec3 min,vec3 max,Material mat)
        {
            _aabb = new AABB(min, max);
            var sides = new Hitable[6];
            sides[0] = new XZRect(max.y, min.x, max.x, min.z, max.z, mat);
            sides[1] = new XZRect(min.y, min.x, max.x, min.z, max.z, mat,true);
            sides[2] = new YZRect(max.x, min.y, max.y, min.z, max.z, mat);
            sides[3] = new YZRect(min.x, min.y, max.y, min.z, max.z, mat, true);
            sides[4] = new XYRect(max.z, min.x, max.x, min.y, max.y, mat);
            sides[5] = new XYRect(min.z, min.x, max.x, min.y, max.y, mat, true);
            _sideList = new HitList(sides);

        }
        public AABB BoundVolume(float t0, float t1)
        {
            return _aabb;
        }

        public bool Hit(Ray ray, float min, float max, out HitRecord r)
        {
            return _sideList.Hit(ray, min, max, out r);
        }
    }
    class Tranlate : Hitable
    {
        vec3 _offset;
        Hitable _inner;
        public Tranlate(Hitable hitable, vec3 o)
        {
            _inner = hitable;
            _offset = o;
        }
        public AABB BoundVolume(float t0, float t1)
        {
            var b = _inner.BoundVolume(t0,t1);
            return new AABB(b._min + _offset, b._max + _offset);
        }

        public bool Hit(Ray ray, float min, float max, out HitRecord r)
        {
            var newRay = new Ray(ray.position - _offset, ray.direction, ray.time);
            if (_inner.Hit(newRay, min, max, out r))
            {
                r.point += _offset;
                return true;
            }
            return false;
        }
    }
    class RotateY : Hitable
    {
        Hitable _inner;
        float cos_theta;
        float sin_theta;
        public RotateY(Hitable hitable,float angle)
        {
            _inner = hitable;
            float radians = (MathF.PI  / 180.0f) * angle;
            sin_theta = MathF.Sin(radians);
            cos_theta = MathF.Cos(radians);
        }
        public AABB BoundVolume(float t0, float t1)
        {
            return _inner.BoundVolume(t0,t1);
        }

        public bool Hit(Ray ray, float min, float max, out HitRecord r)
        {
            vec3 origin = ray.position;
            vec3 direction = ray.direction;
            origin[0] = cos_theta * ray.position[0] - sin_theta * ray.position[2];
            origin[2] = sin_theta * ray.position[0] + cos_theta * ray.position[2];
            direction[0] = cos_theta * ray.direction[0] - sin_theta * ray.direction[2];
            direction[2] = sin_theta * ray.direction[0] + cos_theta * ray.direction[2];
            var rotated_r = new Ray(origin, direction, ray.time);
            if (_inner.Hit(rotated_r, min, max, out r))
            {
                vec3 normal = r.normal;
                var x = cos_theta * r.point[0] + sin_theta * r.point[2];
                var z = -sin_theta * r.point[0] + cos_theta * r.point[2];
                r.point = new vec3(x, r.point.y, z);
                x = cos_theta * r.normal[0] + sin_theta * r.normal[2];
                z = -sin_theta * r.normal[0] + cos_theta * r.normal[2];
                r.normal = glm.normalize(new vec3(x, r.normal.y, z));

                return true;
            }
            else
                return false;
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
