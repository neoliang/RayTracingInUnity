

namespace GlmNet
{
    using vec3 = UnityEngine.Vector3;

    public static class glm
    {
        public static vec3 normalize(vec3 i)
        {
            return i.normalized;
        }
        public static float  dot(vec3 i,vec3 j)
        {
            return vec3.Dot(i, j);
        }
        public static vec3 cross(vec3 i,vec3 j)
        {
            return vec3.Cross(i, j);
        }
    }
    public static class MathF
    {
        public static float Sqrt(float f)
        {
            return UnityEngine.Mathf.Sqrt(f);
        }
        public static float PI = UnityEngine.Mathf.PI;
        public static float Tan(float f)
        {
            return UnityEngine.Mathf.Tan(f);
        }
        public static float Pow(float f,float p)
        {
            return UnityEngine.Mathf.Pow(f, p);
        }
        public static float Sin(float angle)
        {
            return UnityEngine.Mathf.Sin(angle);
        }
    }
}

namespace System.Drawing
{
    using ImageConversion = UnityEngine.ImageConversion;
    namespace Imaging
    {
        public class ImageFormat
        {
            public static int Png = 0;
        }
    }
    public class Color
    {
        public UnityEngine.Color innerColor;
        public static Color FromArgb(int r, int g, int b)
        {
            var c = new Color();
            c.innerColor = new UnityEngine.Color(r / 255.0f, g / 255.0f, b / 225.0f);
            return c;
        }
    }
    public class Bitmap
    {
        UnityEngine.Texture2D tex;
        public Bitmap(int width,int height)
        {
            tex = new UnityEngine.Texture2D(width, height, UnityEngine.TextureFormat.RGB24, false);
        }
        public void SetPixel(int x,int y,Color c)
        {
            tex.SetPixel(x, y, c.innerColor);
        }
        public void Save(string path,int f)
        {
            var bytes = ImageConversion.EncodeToPNG(tex);
            System.IO.File.WriteAllBytes(path, bytes);
        }
    }
}

public class TestTracing : UnityEngine.ScriptableObject
{
    public int Height = 300;
    public int Width = 600;
    public int SampleCount = 5;
    public string fileName = "1.ppm";
    public bool useBVH = true;

}