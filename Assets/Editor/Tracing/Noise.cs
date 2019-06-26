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
    class Noise
    {
        const int maxL = 256;
        float[] _randomValue = new float[maxL];
        int[] _xAxis = new int[maxL];
        int[] _yAxis = new int[maxL];
        int[] _zAxis = new int[maxL];
        float interpolte(float a,float b,float t)
        {
            return (1 - t) * a + t * b;
        }
        float bilinear_interplote(float[,]values,float v,float w)
        {
            float d0 = interpolte(values[0, 0], values[1, 0],v);
            float d1 = interpolte(values[0, 1], values[1, 1],v);
            return interpolte(d0, d1, w);                   
        }
        float trlinear_interpolte(float [,,]values,float u,float v,float w)
        {
            float[,] c = new float[2, 2];
            for(int i=0;i<2;++i)
            {
                for(int j = 0;j<2;++j)
                {
                    c[i, j] = interpolte(values[0, i, j], values[1, i, j], u);
                }
            }
            return bilinear_interplote(c, v, w);
        }
        public float GetValue(vec3 pos)
        {
            int x = (int)MathF.Abs (pos.x * 4)% 256;
            int y = (int)MathF.Abs(pos.y * 4)% 256;
            int z = (int)MathF.Abs(pos.z * 4)% 256;
            //return _randomValue[_xAxis[x] ^ _yAxis[y] ^ _zAxis[z]];
            float u = MathF.Abs(pos.x - (int)pos.x);
            float v = MathF.Abs(pos.y - (int)pos.y);
            float w = MathF.Abs(pos.z - (int)pos.z);
            float[,,] values = new float[2,2,2];
            for(int i=0;i<2;++i)
            {
                for(int j = 0;j<2;++j)
                {
                    for(int k=0;k<2;++k)
                    {
                        int idx = (_xAxis[x] + i) ^ (_yAxis[y] + j) ^ (_zAxis[z] + k);
                        values[i, j, k] = _randomValue[idx % 256];
                    }
                }
            }
            return trlinear_interpolte(values,u,v,w);
        }
        public Noise()
        {
            for(int i = 0;i<_randomValue.Length;++i)
            {
                _randomValue[i] = Exten.rand01();
            }
            for(int i =0;i< maxL;++i)
            {
                _xAxis[i] = (int)(maxL * Exten.rand01());
                _yAxis[i] = (int)(maxL * Exten.rand01()); 
                _zAxis[i] = (int)(maxL * Exten.rand01()); 
            }
        }
    }
}
