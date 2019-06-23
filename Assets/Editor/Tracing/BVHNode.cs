using System;
using GlmNet;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
#if UNITY_EDITOR
using vec3 = UnityEngine.Vector3;
#endif
namespace RT1 {

    class BVHNode : Hitable
    {
        AABB _box;
        Hitable _left;
        Hitable _right;


        public class BVHComparer : System.Collections.IComparer
        {
            static int CmpBVH(Hitable left, Hitable right, int idx)
            {
                var ab1 = left.BoundVolume(0, 1);
                var ab2 = right.BoundVolume(0, 1);
                return (int)(ab1._max[idx] - ab2._min[idx]);
            }
            Func<Hitable, Hitable, int> _cmp = null;
            public BVHComparer(int idx)
            {
                _cmp = (l, r) => CmpBVH(l, r, idx);
            }
            public int Compare(object x, object y)
            {
                return _cmp((Hitable) x, (Hitable) y);
            }
        }
        public BVHNode(Hitable[] list,int low,int high,float t0,float t1)
        {
            if(low <0 || high >list.Length)
            {
                throw new Exception("BVHNode Contruction error low <0 || high >list.Length");
            }
            int length = high - low;
            if (length <=0)
            {
                throw new Exception("BVHNode Contruction error low >= high");
            }
            
            if(length == 1)
            {
                _left = _right = list[low];
                _box = _left.BoundVolume(t0,t1);
            }
            else if(length == 2)
            {
                _left = list[low];
                _right = list[low + 1];
                _box = AABB.SurroundingBox(_left.BoundVolume(t0, t1), _right.BoundVolume(t0, t1));
            }
            else
            {
                Array.Sort(list,low,high-low, new BVHComparer((int)(3 * Exten.rand01())));
                int mid = (low + high) / 2;
                _left = new BVHNode(list, low, mid,t0,t1);
                _right = new BVHNode(list, mid, high,t0,t1);
                _box = AABB.SurroundingBox(_left.BoundVolume(t0, t1), _right.BoundVolume(t0, t1));
            }

        }
        public AABB BoundVolume(float t0, float t1)
        {
            return _box;
        }

        public bool Hit(Ray ray, float min, float max, out HitRecord r)
        {
            r = null;
            if(!_box.Hit(ray,min,max))
            {
                return false;
            }
            HitRecord leftHitRec = null;
            bool hitLeft = _left.Hit(ray, min, max, out leftHitRec);
            HitRecord rightHitRec = null;
            bool hitRight = _right.Hit(ray, min, max, out rightHitRec);
            if(hitLeft && hitRight)
            {
                r = leftHitRec.t < rightHitRec.t ? leftHitRec : rightHitRec;
                return true;
            }
            else if(hitLeft)
            {
                r = leftHitRec;
                return true;
            }
            else if(hitRight)
            {
                r = rightHitRec;
                return true;
            }
            return false;
        }
    }
}
