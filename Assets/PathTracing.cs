using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Profiling;
public class PathTracing : MonoBehaviour
{
    readonly float M_PI = Mathf.PI;
    public Camera camera;
    public float PixelSize = 2;
    public int MaxDepth = 5;
    public int SampleCount = 2;
    float rand(float a,float b)
    {
        return UnityEngine.Random.Range(a, b);
    }
    Ray GenRay(float x,float y,Texture2D finalImage)
    {
        float pixelX = x + rand(0, PixelSize);
        float pixelY = y + rand(0, PixelSize);
        var pixel = new Vector2(pixelX / finalImage.width, pixelY / finalImage.height);
        Ray r = camera.ViewportPointToRay(pixel);
        return r;
    }
    Vector3 RandomUnitVectorInHemisphereOf(Vector3 normal)
    {
        Vector3 dir;
        do
        {
            dir = new Vector3(rand(-1,1), rand(-1,1), rand(-1,1));
        }
        while (dir.sqrMagnitude > 1.0f);
        return dir;
    }

    public UnityEngine.UI.RawImage traceOut;
    [ContextMenu("RunTrace")]
    void Run()
    {
        var tex = new Texture2D(512, 512, TextureFormat.RGB24, false);
        Render(tex, SampleCount);
        tex.Apply();
        traceOut.texture = tex;
    }
    void Render(Texture2D finalImage, int numSamples)
    {
        Profiler.BeginSample("Render");
        for (int y = 0; y < finalImage.height;++y)
        {
            //EditorUtility.DisplayCancelableProgressBar("tracing", "", y / (float)finalImage.height);
            for (int x = 0;x < finalImage.width;++x)
            {
                var color = Color.black;
                for (int i = 0; i < numSamples; ++i)
                {
                    Profiler.BeginSample("GenRay");
                    var r = GenRay(x, y, finalImage);
                    Profiler.EndSample();
                    Profiler.BeginSample("TracePath");
                    color += TracePath(r, 0);
                    Profiler.EndSample();
                }
                var finalColor = color / numSamples;
                finalColor.a = 1.0f;
                finalImage.SetPixel(x, y, finalColor);
            }
        }
        Profiler.EndSample();
        //EditorUtility.ClearProgressBar();
    }
  
    Color TracePath(Ray ray, int depth)
    {
        if (depth >= MaxDepth)
        {
            return Color.black;  // Bounced enough times.
        }
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit) == false)
        {
            return Color.black;  // Nothing was hit.
        }
        Profiler.BeginSample("GetComponent<MeshRenderer");
        Material material = hit.transform.GetComponent<MeshRenderer>().sharedMaterial;
        Profiler.EndSample();
        Profiler.BeginSample("GetEmissionColor");
        var emittance = material.GetColor("_EmissionColor");
        Profiler.EndSample();
        // Pick a random direction from here and keep going.
        // This is NOT a cosine-weighted distribution!
        Profiler.BeginSample("RandomUnitVectorInHemisphere");
        var direction = RandomUnitVectorInHemisphereOf(hit.normal);
        Profiler.EndSample();
        Ray newRay = new Ray(hit.point, direction);
        // Recursively trace reflected light sources.
        Color incoming = TracePath(newRay, depth + 1);
        Profiler.BeginSample("LightCal");
        // Probability of the newRay
        float p = 1 / (2 * M_PI);
        // Compute the BRDF for this ray (assuming Lambertian reflection)
        Profiler.BeginSample("Dot");
        float cos_theta = Mathf.Max(0, Vector3.Dot(newRay.direction.normalized, hit.normal.normalized));
        Profiler.EndSample();
        Profiler.BeginSample("GetColor");
        Color BRDF = material.color / M_PI;
        Profiler.EndSample();
        // Apply the Rendering Equation here.
        var finalColor = emittance + (BRDF * incoming * cos_theta / p);
        Profiler.EndSample();
        return finalColor;
    }
}
