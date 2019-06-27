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
        int floor(float f)
        {
            return (int)Math.Floor(f);
        }
        public float GetValue(vec3 pos)
        {
            int x = (int)Math.Floor (pos.x);
            int y = (int)Math.Floor(pos.y);
            int z = (int)Math.Floor(pos.z);
            //return _randomValue[_xAxis[x] ^ _yAxis[y] ^ _zAxis[z]];
            float u = pos.x - floor(pos.x);
            float v = pos.y - floor(pos.y);
            float w = pos.z - floor(pos.z);
            u = u * u* (3 - 2 * u);
            v = v * v * (3 - 2 * v);
            w = w * w * (3 - 2 * w);
            float[,,] values = new float[2,2,2];
            for(int i=0;i<2;++i)
            {
                for(int j = 0;j<2;++j)
                {
                    for(int k=0;k<2;++k)
                    {
                        int idx = (_xAxis[(x+i)&255]) ^ (_yAxis[(y+j) & 255]) ^ (_zAxis[(z+k) & 255]);
                        values[i, j, k] = _randomValue[idx];
                    }
                }
            }
            return trlinear_interpolte(values,u,v,w);
        }
        public float turb(vec3 p, int depth = 7)
        {
            float accum = 0;
            vec3 temp_p = p;
            float weight = 1.0f;
            for (int i = 0; i < depth; i++)
            {
                accum += weight * GetValue(temp_p);
                weight *= 0.5f;
                temp_p *= 2;
            }
            return Math.Abs(accum);
        }
        public Noise()
        {
            for(int i = 0;i<_randomValue.Length;++i)
            {
                _randomValue[i] = Exten.rand01();
            }
            for(int i =0;i< maxL;++i)
            {
                _xAxis[i] = i; //(int)(maxL * Exten.rand01());
                _yAxis[i] = i; //(int)(maxL * Exten.rand01());
                _zAxis[i] = i; //(int)(maxL * Exten.rand01()); 
            }
            shuff(_xAxis);
            shuff(_yAxis);
            shuff(_zAxis);
        }
        void shuff (int [] vs )
        {
            for(int i = 0;i<vs.Length-1;++i)
            {
                int nextIdx = i + 1 + (int)((vs.Length - i-1) * Exten.rand01());
                var temp = vs[i];
                vs[i] = vs[nextIdx];
                vs[nextIdx] = temp;
            }
        }
    }

}

