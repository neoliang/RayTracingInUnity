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
    class Program
    {
        static void print(string format, params object[] o)
        {
            Console.Write(string.Format(format, o));
        }

        static void printColor(vec3 color)
        {
            color = Exten.clamp01(color);
            int r = (int)(color.x * 255.99f);
            int g = (int)(color.y * 255.99f);
            int b = (int)(color.z * 255.99f);
            print("{0} {1} {2} ", r, g, b);
        }

        static Texture tex = new SolidTexture(new vec3(0.1f, .2f, .5f));
        static Sphere s1 = new Sphere(new vec3(0, 0, -1), 0.5f, new Lambertian(tex));
        static Sphere s2 = new Sphere(new vec3(0, -100.5f, -1), 100.0f, new Lambertian(new SolidTexture(new vec3(0.8f, 0.8f, 0))));
        static Sphere s3 = new Sphere(new vec3(1, 0, -1), 0.5f, new Metal(new vec3(0.8f, .6f, .2f)));
        static Sphere s4 = new Sphere(new vec3(-1, 0, -1), 0.5f, new Dieletric(2.4f));
        static Hitable scene = new HitList(new Hitable[] { s1, s2, s3, s4 });
        static PDF cosPDf = new CosinePDF();
        static PDF lightPDf;
        static PDF mixturePdf;
        static vec3 RayTracing(Ray r, int depth)
        {
            HitRecord record;
            if (scene.Hit(r, 0.001f, 100000.0f, out record))
            {
                Ray nextRay;
                vec3 color;
                var emmited = record.mat.Emitted(r,record, record.u, record.v, record.point);
                float pdf = 1.0f;
                if (depth < 50 && record.mat.Scatter(r, record, out color, out nextRay,out pdf))
                {
                    //if(Exten.rand01() < 0.5)
                    //{
                    //    //hard code light
                    //    //554, 213, 343, 227, 332
                    //    vec3 pointOnLight = new vec3(Exten.randRange(213, 343), 554, Exten.randRange(227, 332));
                    //    vec3 toLight = pointOnLight - record.point;
                    //    if(glm.dot(toLight,record.normal) <= 0)
                    //    {
                    //        return emmited;
                    //    }
                    //    nextRay = new Ray(record.point, toLight, nextRay.time);
                    //    if(nextRay.direction.y <0.0001f)
                    //    {
                    //        return emmited;
                    //    }
                    //    float area = (343 - 213) * (332 - 227);
                    //    float l = toLight.length();
                    //    pdf = l * l  /(nextRay.direction.y * area);
                    //}


                    var nextdir = mixturePdf.Generate(record.point, record.normal);
                    nextRay = new Ray(record.point, nextdir, r.time);
                    pdf = mixturePdf.Value(nextRay, record.normal); 
                    var nextColor = RayTracing(nextRay, depth + 1);
                    float scatterPdf = record.mat.Scatter_PDf(r, record, nextRay);
                    return emmited +   color.mul(nextColor) * scatterPdf / pdf;
                }
                else
                {
                    return emmited;
                }
            }
            return new vec3();
            vec3 d = r.direction;
            float f = 0.5f * (d.y + 1.0f);
            return 0.3f*((1 - f) * new vec3(1, 1, 1) + f * new vec3(0.5f, 0.7f, 1.0f));
        }

        static float drand48()
        {
            return Exten.rand01();
        }
        //class Scene : Hitable
        //{
        //    AABB _box;
        //    Hitable _inner;
        //    public Scene(HitList l,float t0,float t1)
        //    {
        //        _box = l.BoundVolume(t0,t1);
        //        _inner = l;
        //    }
        //    public AABB BoundVolume(float t0, float t1)
        //    {
        //        return _box;
        //    }

        //    public bool Hit(Ray ray, float min, float max, out HitRecord r)
        //    {
        //        r = null;
        //        if(!_box.Hit(ray,min,max))
        //        {
        //            return false;
        //        }
        //        return _inner.Hit(ray, min, max, out r);
        //    }
        //}

        static Hitable random_scene(bool bvh)
        {
            int n = 500;
            Hitable[] list = new Hitable[n + 1];
            vec3 black = new vec3(0.1f, 0.1f, 0.2f);
            vec3 white = new vec3(1f, 1f, 1f);
            var check = new CheckTexture(black, white); 
            var noise = new NoiseTexture(new Noise() );
            list[0] = new Sphere(new vec3(0, -1000, 0), 1000, new Lambertian(noise));
            int i = 1;
            //for (int a = -11; a < 11; a++)
            //{
            //    for (int b = -11; b < 11; b++)
            //    {
            //        float choose_mat = Exten.rand01();
            //        var center = new vec3(a + 0.9f * Exten.rand01(), 0.2f, b + 0.9f * Exten.rand01());
            //        if ((center - new vec3(4f, 0.2f, 0f)).length() > 0.9f)
            //        {
            //            if (choose_mat < 0.8)
            //            {  // diffuse
            //                var color = new vec3(Exten.rand01() * Exten.rand01(), Exten.rand01() * Exten.rand01(), Exten.rand01() * Exten.rand01());

            //                var mat = new Lambertian(new SolidTexture(color));
            //                list[i++] = new Sphere(center, 0.2f,mat);
            //            }
            //            else if (choose_mat < 0.95f)
            //            { // metal
            //                list[i++] = new Sphere(center, 0.2f,
            //                        new Metal(new vec3(0.5f * (1 + drand48()), 0.5f * (1 + drand48()), 0.5f * (1 + drand48())), 0.5f * drand48()));
            //            }
            //            else
            //            {  // glass
            //                list[i++] = new Sphere(center, 0.2f, new Dieletric(1.5f));
            //            }
            //        }
            //    }
            //}

            //list[i++] = new Sphere(new vec3(2, 1, 0), 1.0f, new Dieletric(1.3f));
            //list[i++] = new Sphere(new vec3(-3, 1, 2), 1.0f, new Metal(new vec3(0.4f, 0.2f, 0.1f),0.2f));
            list[i++] = new Sphere(new vec3(4, 1, 0), 1.0f, new Lambertian(noise));
            //list[i++] = new Sphere(new vec3(4, 1, -0.5f), 1.0f, new Metal(new vec3(0.7f, 0.6f, 0.5f), 0.0f));
            var finalList = list.Where(p => p != null).ToArray();
            if (!bvh)
            {
                return new HitList(finalList);
            }
            else
            {
                //Array.Sort(finalList, new BVHNode.BVHComparer(0));
                return new BVHNode(finalList, 0, finalList.Length, 0, 1);
            }
        }
        static Hitable SimpleLight()
        {
            Texture perlintext = new NoiseTexture(new Noise());
            Hitable[] hitables = new Hitable[4];
            int i = 0;
            var tex2 = new SolidTexture(new vec3(4, 4, 4));
            hitables[i++] = new Sphere(new vec3(0, -1000, 0), 1000, new Lambertian(perlintext));
            hitables[i++] = new Sphere(new vec3(0, 2, 0), 2, new Lambertian(perlintext));
            //hitables[i++] = new Sphere(new vec3(0, 7, 0), 2, new DiffuseLight(tex2));
            hitables[i++] = new XYRect(-2, 3, 5, 1, 3, new DiffuseLight(tex2));
            var finalList = hitables.Where(h => h != null).ToArray();
            return new BVHNode(finalList,0,finalList.Length,0,1);
        }
        static Hitable FinalTest()
        {
            int nb = 20;
            Hitable[] list = new Hitable[30];
            Hitable[] boxlist = new Hitable[10000];
            Material ground = new Lambertian(new SolidTexture(new vec3(0.48f, 0.83f, 0.53f)));
            int b = 0;
            for (int i = 0; i < nb; i++)
            {
                for (int j = 0; j < nb; j++)
                {
                    float w = 100;
                    float x0 = -1000 + i * w;
                    float z0 = -1000 + j * w;
                    float y0 = 0;
                    float x1 = x0 + w;
                    float y1 = 100 * (Exten.rand01() + 0.01f);
                    float z1 = z0 + w;
                    boxlist[b++] = new Box(new vec3(x0, y0, z0), new vec3(x1, y1, z1), ground);
                }
            }
            int l = 0;
            list[l++] = new BVHNode(boxlist,0, b, 0, 1);
            Material light = new DiffuseLight(new SolidTexture(new vec3(7, 7, 7)));
            list[l++] = new XZRect(554, 123, 423, 147, 412,   light);
            vec3 center = new vec3(400, 400, 200);
            //public Sphere(vec3 c, vec3 c1, float r, Material m, float time1, float time2)
            list[l++] = new Sphere(center, center + new vec3(30, 0, 0), 50, new Lambertian(new SolidTexture(new vec3(0.7f, 0.3f, 0.1f))), 0, 1);
            list[l++] = new Sphere(new vec3(260, 150, 45), 50, new Dieletric(1.5f));
            list[l++] = new Sphere(new vec3(0, 150, 145), 50, new Metal(new vec3(0.8f, 0.8f, 0.9f), 10.0f));
            Hitable boundary = new Sphere(new vec3(360, 150, 145), 70, new Dieletric(1.5f));
            list[l++] = boundary;
            boundary = new Sphere(new vec3(0, 0, 0), 5000, new Dieletric(1.5f));
            Texture pertext = new NoiseTexture(new Noise());
            list[l++] = new Sphere(new vec3(220, 280, 300), 80, new Lambertian(pertext));
            return new HitList(list.Where(arg => arg !=null).ToArray());
        }
        static Hitable CornellBox()
        {
            var red = new Lambertian(new vec3(0.65f, 0.05f, 0.05f));
            var white = new Lambertian(new vec3(0.73f, 0.73f, 0.73f));
            var green = new Lambertian(new vec3(0.12f, 0.45f, 0.15f));
            var light = new DiffuseLight(new SolidTexture(new vec3(15, 15, 15)));
            int i = 0;
            Hitable[] hitables = new Hitable[8];
            hitables[i++] = new YZRect(555, 0, 555, 0, 555,green,true);
            hitables[i++] = new YZRect(0, 0, 555, 0, 555, red);
            var lightRect = new XZRect(554, 213, 343, 227, 332, light, true);
            hitables[i++] = lightRect;
            hitables[i++] = new XZRect(0, 0, 555, 0, 555, white);
            hitables[i++] = new XZRect(555, 0, 555, 0, 555, white,true);
            hitables[i++] = new XYRect(555, 0, 555, 0, 555, white,true);

            Hitable box = new Box(new vec3(0, 0, 0), new vec3(165, 165, 165), white);
            box = new Tranlate(new RotateY(box, -18), new vec3(130, 0, 65));
            hitables[i++] = box;
            Hitable box2 = new Box(new vec3(0, 0, 0), new vec3(165, 330, 165), white);
            box2 = new Tranlate(new RotateY(box2, 15), new vec3(265, 0, 295));
            hitables[i++] = box2;
            lightPDf = new LightPDF(lightRect);
            mixturePdf = new MixPDF(cosPDf, lightPDf);
            return new HitList(hitables.Where(p=>p!=null).ToArray());
        }
#if UNITY_EDITOR
        static void ShowProgress(float p)
        {
            UnityEditor.EditorUtility.DisplayProgressBar("Hold on", "Compiling", p);
        }
        static void HideProgress()
        {
            UnityEditor.EditorUtility.ClearProgressBar();
        }
#else
        static void ShowProgress(float p)
        {
            Console.CursorLeft = 0;
            Console.Write("{0:p0}", p);
        }
        static void HideProgress()
        {
        }
#endif
        static int clamp(int v,int min,int max)
        {
            return v < min ? min : (v > max ? max : v); 
        }
        public static void Main(string[] args)
        {
            Exten.r = new Random(0);
            int width = 600;
            int height = 300;
            string outPath = "1.png";
            int sampleCount = 5;
            bool bvh = false;
            if (args.Length >= 1)
            {
                sampleCount = int.Parse(args[0]);
            }
            if( args.Length >=2)
            {
                width = int.Parse(args[1]);
            }
            if(args .Length >= 3)
            {
                height = int.Parse(args[2]);
            }
            if(args.Length >=4)
            {
                outPath = args[3];
            }
            if(args.Length >= 5)
            {
                bvh = bool.Parse(args[4]);
            }
            scene =  CornellBox();// SimpleLight(); //random_scene(bvh);
            vec3 lookfrom = new vec3(278, 278, -800);
            vec3 lookat = new vec3(278, 278, 0);
            float dist_to_focus = 10.0f;
            float aperture = 0.0f;


            Camera camera = new Camera(lookfrom,lookat,new vec3(0,1,0),40, (float)(width) / (float)(height), aperture, dist_to_focus,0,1);
            Bitmap bmp = new Bitmap(width, height);
            for (int i = height - 1; i >= 0; --i)
            {
                ShowProgress((height - i) / (float)height);
                for (int j = 0; j < width; ++j)
                {
                    var c = new vec3(0, 0, 0);
                    for (int k = 0; k < sampleCount; ++k)
                    {
                        float x = (j + Exten.rand01()) / (float)width;
                        float y = (i + Exten.rand01()) / (float)height;
                        var ray = camera.GenRay(x, y);
                        c += RayTracing(ray, 0);
                    }
                    c /= sampleCount;
                    //printColor(c.gamma());

                    c = c.gamma();
                    int r = (int)(c.x * 255.99f);
                    int g = (int)(c.y * 255.99f);
                    int b = (int)(c.z * 255.99f);
                    r = clamp(r,0, 255);
                    g = clamp(g, 0, 255);
                    b = clamp(b, 0, 255);
                    Color o = Color.FromArgb(r, g, b);
#if UNITY_EDITOR
                    bmp.SetPixel(j, i, o);
#else
                    bmp.SetPixel(j, height - i - 1, o);
#endif
                    
                }
            }
            bmp.Save(outPath, ImageFormat.Png);
            HideProgress();

        }
    }
}
