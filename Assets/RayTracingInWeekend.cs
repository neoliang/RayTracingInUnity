

using System;
using System.Linq;
using UnityEngine.Profiling;
namespace RTold
{

    class Ray
    {
        public UnityEngine.Vector3 position;
        public UnityEngine.Vector3 direction;
        public Ray(UnityEngine.Vector3 pos, UnityEngine.Vector3 dir)
        {
            position = pos;
            direction = dir.normalized;
        }
        public UnityEngine.Vector3 at(float t)
        {
            return position + t * direction;
        }
        public Ray()
        {
            position = new UnityEngine.Vector3(0, 0, 0);
            direction = new UnityEngine.Vector3(0, 0, 0);
        }
    }
    interface Material
    {
        bool Scatter(Ray ray, UnityEngine.Vector3 point, UnityEngine.Vector3 normal, out UnityEngine.Vector3 attenuation, out Ray scattered);
    }
    class Lambertian : Material
    {
        public UnityEngine.Vector3 color;
        public Lambertian(UnityEngine.Vector3 c)
        {
            color = c;
        }
        public bool Scatter(Ray ray, UnityEngine.Vector3 point, UnityEngine.Vector3 normal, out UnityEngine.Vector3 attenuation, out Ray scattered)
        {

            scattered = new Ray(point, normal + Exten.RandomVecInSphere());
            attenuation = color;
            return true;
        }
    }
    class Metal : Material
    {
        public UnityEngine.Vector3 color;
        private float fuzz;
        public Metal(UnityEngine.Vector3 c, float f = -1.0f)
        {
            color = c;
            fuzz = f;
        }
        public bool Scatter(Ray ray, UnityEngine.Vector3 point, UnityEngine.Vector3 normal, out UnityEngine.Vector3 attenuation, out Ray scattered)
        {
            var reflected = Exten.reflect(ray.direction, normal);
            if (fuzz > 0)
            {
                reflected = reflected + fuzz * Exten.RandomVecInSphere();
            }
            scattered = new Ray(point, reflected);
            attenuation = color;
            return UnityEngine.Vector3.Dot (ray.direction, normal) < 0;
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
        public bool Scatter(Ray ray, UnityEngine.Vector3 point, UnityEngine.Vector3 normal, out UnityEngine.Vector3 attenuation, out Ray scattered)
        {
            attenuation = new UnityEngine.Vector3(1.0f, 1.0f, 1.0f);
            float eta = ref_idx;
            var n = normal;


            if (UnityEngine.Vector3.Dot(ray.direction, normal) > 0)//从内到外
            {
                n = new UnityEngine.Vector3(-normal.x, -normal.y, -normal.z);
            }
            else
            {
                eta = 1.0f / eta;
            }

            UnityEngine.Vector3 r;
            float prob = 1.0f;
            if (Exten.refract(ray.direction, n, eta, out r))
            {
                float cosi = -UnityEngine.Vector3.Dot(ray.direction, n);
                float cost = -UnityEngine.Vector3.Dot(r, n);
                prob = fresnel(cosi, cost, eta);
            }
            if (Exten.rand01() < prob)
            {
                UnityEngine.Vector3 reflected = Exten.reflect(ray.direction, normal);
                scattered = new Ray(point, reflected);
            }
            else
            {
                scattered = new Ray(point, r);
            }
            return true;

        }
    }
    class HitRecord
    {
        public float t;
        public UnityEngine.Vector3 point;
        public UnityEngine.Vector3 normal;
        public Material mat;
    }
    interface Hitable
    {
        bool Hit(Ray ray, float min, float max, out HitRecord r);
    }
    class Sphere : Hitable
    {
        public UnityEngine.Vector3 center;
        public float radius;
        public Material mat;
        public Sphere(UnityEngine.Vector3 c, float r, Material m)
        {
            mat = m;
            center = c;
            radius = r;
        }
        public bool Hit(Ray ray, float min, float max, out HitRecord r)
        {
            //var hitpoint = r.position + r.direction * t;
            //(r.position + r.direction * t - center)*(r.position + r.direction * t - center) = radius*radius

            //(delta + r.dir * t)*(delta+r.dir*t) = r^2
            //delta*delta + 2*delta*r.dir*t+  t^2 *r.dir*r.dir = r^2
            r = null;
            var oc = ray.position - center;
            var a = UnityEngine.Vector3.Dot(ray.direction, ray.direction);
            var b = 2.0f * UnityEngine.Vector3.Dot(oc, ray.direction);
            var c = UnityEngine.Vector3.Dot(oc, oc) - radius * radius;
            var delta = b * b - 4 * a * c;
            if (delta < 0)
            {
                return false;
            }

            var t = (-b - UnityEngine.Mathf.Sqrt(delta)) / (2 * a);
            if (t < min || t > max)
            {
                return false;
            }
            r = new HitRecord();
            r.t = t;
            r.point = ray.at(t);
            r.normal = (r.point - center) / radius;
            r.mat = mat;
            return true;
        }
    }
    class HitList : Hitable
    {
        public Hitable[] hitables;
        public HitList(Hitable[] ls)
        {
            hitables = ls;
        }
        public bool Hit(Ray ray, float min, float max, out HitRecord record)
        {
            HitRecord nearestRecord = null;
            foreach (var h in hitables)
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
    }
    class Camera
    {


        UnityEngine.Vector3 origin;
        UnityEngine.Vector3 lower_left_corner;
        UnityEngine.Vector3 horizontal;
        UnityEngine.Vector3 vertical;
        float lenRadius;
        public Camera(UnityEngine.Vector3 lookfrom, UnityEngine.Vector3 lookat, UnityEngine.Vector3 head, float vfov, float aspect, float aperture, float focus_dist)
        {
            float theta = vfov * UnityEngine.Mathf.PI / 180;
            float half_height = focus_dist * UnityEngine.Mathf.Tan(theta / 2);
            float half_width = aspect * half_height;

            lenRadius = aperture / 2.0f;
            origin = lookfrom;

            UnityEngine.Vector3 view = lookat - lookfrom;
            view.Normalize();
            UnityEngine.Vector3 right = UnityEngine.Vector3.Cross(view, head);
            right.Normalize(); 
            UnityEngine.Vector3 up = UnityEngine.Vector3.Cross(right, view);
            up.Normalize();

            lower_left_corner = origin - half_width * right - half_height * up + focus_dist * view;

            horizontal = 2.0f * half_width * right;
            vertical = 2.0f * half_height * up;
        }
        UnityEngine.Vector3 random_in_unit_disk()
        {
            UnityEngine.Vector3 p;
            do
            {
                p = 2.0f * new UnityEngine.Vector3(Exten.rand01(), Exten.rand01(), 0) - new UnityEngine.Vector3(1, 1, 0);
            } while (UnityEngine.Vector3.Dot(p, p) >= 1.0f);
            return p;
        }
        public Ray GenRay(float s, float t)
        {
            UnityEngine.Vector3 offset = lenRadius * random_in_unit_disk();
            var point = origin + offset;
            return new Ray(point, lower_left_corner + s * horizontal + t * vertical - point);
        }

    }
    public static class Exten
    {
        public static float length(this UnityEngine.Vector3 v)
        {
            return UnityEngine.Mathf.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
        }
        public static UnityEngine.Vector3 gamma(this UnityEngine.Vector3 c)
        {
            return new UnityEngine.Vector3(UnityEngine.Mathf.Pow(c.x, 1.0f / 2.2f), UnityEngine.Mathf.Pow(c.y, 1.0f / 2.2f), UnityEngine.Mathf.Pow(c.z, 1.0f / 2.2f));
        }

        public static UnityEngine.Vector3 vec(float x, float y, float z)
        {
            return new UnityEngine.Vector3(x, y, z);
        }
        public static UnityEngine.Vector3 reflect(UnityEngine.Vector3 v, UnityEngine.Vector3 n)
        {
            return v - 2.0f * UnityEngine.Vector3.Dot(v, n) * n;
        }
        public static bool refract1(UnityEngine.Vector3 v, UnityEngine.Vector3 n, float ni_over_nt, out UnityEngine.Vector3 refracted)
        {
            UnityEngine.Vector3 uv = v.normalized;
            float dt = UnityEngine.Vector3.Dot(uv, n);
            float discriminat = 1.0f - ni_over_nt * ni_over_nt * (1 - dt * dt);
            if (discriminat > 0)
            {
                refracted = ni_over_nt * (uv - n * dt) - n * UnityEngine.Mathf.Sqrt(discriminat);
                return true;
            }
            else
            {
                refracted = new UnityEngine.Vector3();
                return false;
            }
        }
        public static bool refract(UnityEngine.Vector3 v, UnityEngine.Vector3 n, float eta, out UnityEngine.Vector3 refracted)
        {
            var dt = UnityEngine.Vector3.Dot(v, n);
            var partb1 = 1.0f - eta * eta * (1 - dt * dt);
            if (partb1 > 0)
            {
                refracted = eta * v - (eta * dt + UnityEngine.Mathf.Sqrt(partb1)) * n;

                return true;
            }
            else
            {
                refracted = new UnityEngine.Vector3();
                return false;
            }

        }
        public static UnityEngine.Vector3 RandomVecInSphere()
        {
            UnityEngine.Vector3 n;
            do
            {
                n = 2.0f * vec(rand01(), rand01(), rand01()) - vec(1.0f, 1.0f, 1.0f);
            }
            while (n.length() >= 1.0f);
            return n;
        }
        public static float clamp01(float v)
        {
            if (v < 0) return 0;
            if (v > 1) return 1;
            return v;
        }
        public static UnityEngine.Vector3 clamp01(UnityEngine.Vector3 color)
        {
            return new UnityEngine.Vector3(clamp01(color.x), clamp01(color.y), clamp01(color.z));
        }

        public static Random r = new Random();
        public static float rand01()
        {
            const int m = 100000;
            float x = r.Next() % m;
            return x / m;
        }
    }
    public class Program
    {
        static void print(string format, params object[] o)
        {
            Console.Write(string.Format(format, o));
        }


        static void printColor(UnityEngine.Vector3 color)
        {
            color = Exten.clamp01(color);
            int r = (int)(color.x * 255.99f);
            int g = (int)(color.y * 255.99f);
            int b = (int)(color.z * 255.99f);
            print("{0} {1} {2} ", r, g, b);
        }

        static Sphere s1 = new Sphere(new UnityEngine.Vector3(0, 0, -1), 0.5f, new Lambertian(new UnityEngine.Vector3(0.1f, .2f, .5f)));
        static Sphere s2 = new Sphere(new UnityEngine.Vector3(0, -100.5f, -1), 100.0f, new Lambertian(new UnityEngine.Vector3(0.8f, 0.8f, 0)));
        static Sphere s3 = new Sphere(new UnityEngine.Vector3(1, 0, -1), 0.5f, new Metal(new UnityEngine.Vector3(0.8f, .6f, .2f)));
        static Sphere s4 = new Sphere(new UnityEngine.Vector3(-1, 0, -1), 0.5f, new Dieletric(2.4f));
        static Hitable scene = new HitList(new Hitable[] { s1, s2, s3, s4 });
        static UnityEngine.Vector3 RayTracing(Ray r, int depth)
        {
            HitRecord record;
            Profiler.BeginSample("HitTest");
            bool hited = scene.Hit(r, 0.001f, 100000.0f, out record);
            Profiler.EndSample();
            if (hited)
            {
                Ray nextRay;
                UnityEngine.Vector3 color;
                Profiler.BeginSample("mat.Scatter");
                bool isScattered = record.mat.Scatter(r, record.point, record.normal, out color, out nextRay);
                Profiler.EndSample();
                if (depth < 50 && isScattered)
                {
                    var nextColor = RayTracing(nextRay, depth + 1);
                    Profiler.BeginSample("color multiply");
                    var c = new  UnityEngine.Vector3(color.x * nextColor.x,color.y *nextColor.y,color.z * nextColor.z);
                    Profiler.EndSample();
                    return c;
                }
                else
                {
                    Profiler.BeginSample("BlackColor");
                    var c=  new UnityEngine.Vector3();
                    Profiler.EndSample();
                    return c;
                }
            }
            Profiler.BeginSample("background");
            UnityEngine.Vector3 d = r.direction;
            float f = 0.5f * (d.y + 1.0f);
            var backgroundColor = (1 - f) * new UnityEngine.Vector3(1, 1, 1) + f * new UnityEngine.Vector3(0.5f, 0.7f, 1.0f);
            Profiler.EndSample();
            return backgroundColor;
        }

        static float drand48()
        {
            return Exten.rand01();
        }
        static Hitable random_scene()
        {
            int n = 500;
            Hitable[] list = new Hitable[n + 1];
            list[0] = new Sphere(new UnityEngine.Vector3(0, -1000, 0), 1000, new Metal(new UnityEngine.Vector3(0.5f, 0.5f, 0.5f), 0.1f));
            int i = 1;
            for (int a = -11; a < 11; a++)
            {
                for (int b = -11; b < 11; b++)
                {
                    float choose_mat = Exten.rand01();
                    var center = new UnityEngine.Vector3(a + 0.9f * Exten.rand01(), 0.2f, b + 0.9f * Exten.rand01());
                    if ((center - new UnityEngine.Vector3(4f, 0.2f, 0f)).length() > 0.9f)
                    {
                        if (choose_mat < 0.8)
                        {  // diffuse
                            list[i++] = new Sphere(center, 0.2f, new Lambertian(new UnityEngine.Vector3(Exten.rand01() * Exten.rand01(), Exten.rand01() * Exten.rand01(), Exten.rand01() * Exten.rand01())));
                        }
                        else if (choose_mat < 0.95f)
                        { // metal
                            list[i++] = new Sphere(center, 0.2f,
                                    new Metal(new UnityEngine.Vector3(0.5f * (1 + drand48()), 0.5f * (1 + drand48()), 0.5f * (1 + drand48())), 0.5f * drand48()));
                        }
                        else
                        {  // glass
                            list[i++] = new Sphere(center, 0.2f, new Dieletric(1.5f));
                        }
                    }
                }
            }

            list[i++] = new Sphere(new UnityEngine.Vector3(2, 1, 0), 1.0f, new Dieletric(1.3f));
            //list[i++] = new Sphere(new vec3(-3, 1, 2), 1.0f, new Metal(new vec3(0.4f, 0.2f, 0.1f),0.2f));
            //list[i++] = new Sphere(new vec3(4, 1, 0), 1.0f, new Metal(new vec3(0.7f, 0.6f, 0.5f), 0.0f));
            //list[i++] = new Sphere(new vec3(4, 1, -0.5f), 1.0f, new Metal(new vec3(0.7f, 0.6f, 0.5f), 0.0f));
            return new HitList(list.Where(p => p != null).ToArray());
        }

        public static void Main(UnityEngine.Texture2D texture, int sampleCount = 5)
        {
            int width = texture.width;
            int height = texture.height;
            Exten.r = new Random(0);
            //scene = random_scene();

            UnityEngine.Vector3 lookfrom = new UnityEngine.Vector3(3, 3, 2);
            UnityEngine.Vector3 lookat = new UnityEngine.Vector3(0, 0, -1);
            float dist_to_focus = (lookat - lookfrom).length();
            float aperture = 2.0f;


            Camera camera = new Camera(lookfrom, lookat, new UnityEngine.Vector3(0, 1, 0), 60, (float)(width) / (float)(height), aperture, dist_to_focus);
            //print("P3\n{0} {1}\n255\n", width, height);

            for (int i = height - 1; i >= 0; --i)
            {
                for (int j = 0; j < width; ++j)
                {
                    var c = new UnityEngine.Vector3(0, 0, 0);
                    for (int k = 0; k < sampleCount; ++k)
                    {
                        float x = (j + Exten.rand01()) / (float)width;
                        float y = (i + Exten.rand01()) / (float)height;
                        var r = camera.GenRay(x, y);
                        Profiler.BeginSample("RayingRay");
                        c += RayTracing(r, 0);
                        Profiler.EndSample();
                    }
                    c /= sampleCount;
                    var c1 = c.gamma();
                    UnityEngine.Color outColor = new UnityEngine.Color(c1.x,c1.y,c1.z,1);
                    texture.SetPixel(j, i, outColor);
                }
            }
        }
    }
}


public class RayTracingInWeekend : UnityEngine.MonoBehaviour {

    public UnityEngine.UI.RawImage Img;
    public int Height = 300;
    public int SampleCount = 5;
    [UnityEngine.ContextMenu("Run")]
    public void Run()
    {
        var text = new UnityEngine.Texture2D(Height * 2, Height, UnityEngine.TextureFormat.ARGB32, false);

        RTold.Program.Main(text, SampleCount);
        text.Apply();
        Img.texture = text;
    }
}

