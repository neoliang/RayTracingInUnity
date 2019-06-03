using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
public class RayTracer : MonoBehaviour
{
    public Camera renderCamera;
    public Light[] lights;
    public int MaxTraceStep = 2;
    public int RenderTargetWidth = 512;
    public int RenderTargetHeight = 512;
    public UnityEngine.UI.RawImage TraceOut;
    [ContextMenu("Tracing")]
    public void Tracing()
    {
        Texture2D target = new Texture2D(RenderTargetWidth,RenderTargetHeight,TextureFormat.RGB24,false);
        for (int y = 0; y < target.height;++y)
        {
            for (int x = 0; x < target.width;++x)
            {
                Vector2 screenPos = new Vector2(x / (float)target.width, y / (float)target.height);
                var ray = renderCamera.ViewportPointToRay(screenPos);
                var color = TracingRay(ray);
                target.SetPixel(x, y, color.gamma);
            }
#if UNITY_EDITOR
            if(EditorUtility.DisplayCancelableProgressBar("Tracing", "", y / (float)target.height))
            {
                break;
            }
#endif
        }
        target.Apply();
        TraceOut.texture = target;
#if UNITY_EDITOR
        EditorUtility.ClearProgressBar();
#endif
    }
    Color TracingRay(Ray ray, int currentStep = 0)
    {
        if (currentStep >= MaxTraceStep)
        {
            return Color.black;
        }
        RaycastHit hitInfo;
        if (Physics.Raycast(ray, out hitInfo))
        {
            return RenderHit(ray, hitInfo, currentStep);
        }
        else
        {
            return renderCamera.backgroundColor.linear;
        }

    }

    Color CalLight(Color lightColor,Vector3 lightDir,Color hitColor,Vector3 hitNormal,Vector3 viewDir,float reflect)
    {
        var diffuse = Mathf.Max( Vector3.Dot(hitNormal, -lightDir),0 )* lightColor * hitColor;
        var V = -viewDir;
        var R = Vector3.Reflect(lightDir, hitNormal);
        var spec = Mathf.Pow(Mathf.Max(Vector3.Dot(V, R),0), 32) * lightColor * hitColor;
        return diffuse + spec * reflect;
    }
    static Vector3 GetLightDir(Light l)
    {
        return l.transform.rotation * new Vector3(0, 0, 1);
    }
    float rand()
    {
        return UnityEngine.Random.Range(-1.0f, 1.0f);
    }
    Vector3 GenSemSphrereDir()
    {
        Vector3 dir;
        do
        {
            dir = new Vector3(rand(), rand(), rand());
        }
        while (dir.sqrMagnitude > 1.0f);
        return dir;
    }

    //RenderEquation
    Color RenderHit(Ray ray,RaycastHit hitInfo, int currentStep)
    {
        var mat = hitInfo.transform.GetComponent<MeshRenderer>().sharedMaterial;
        var myColor = mat.color.linear;
        var texture = mat.mainTexture as Texture2D;
        if (texture != null)
        {
            float x = hitInfo.textureCoord.x * texture.width;
            float y = hitInfo.textureCoord.y * texture.height;
            myColor = texture.GetPixel((int)x, (int)y).linear;
        }
        float reflect = mat.GetFloat("_Glossiness");
        var dirColor = Color.black;
        //shadow
        for (int i =0;i<lights.Length;++i)
        {
            if(lights[i].isActiveAndEnabled)
            {
                var lightDir = GetLightDir(lights[i]);
                if (!Physics.Raycast(hitInfo.point, -lightDir))
                {
                    dirColor += CalLight(lights[i].color.linear, lightDir, myColor, hitInfo.normal, ray.direction, reflect);
                }
            }

        }


        //mirror
        var nextDir = Vector3.Reflect(ray.direction, hitInfo.normal);
        var mirrorColor = TracingRay(new Ray(hitInfo.point, nextDir), currentStep + 1);
        mirrorColor = CalLight(mirrorColor, -nextDir, myColor, hitInfo.normal, ray.direction, reflect);
        //diff
        nextDir = hitInfo.normal + GenSemSphrereDir();
        nextDir.Normalize();
        var inDirectColor = TracingRay(new Ray(hitInfo.point, nextDir), currentStep + 1);
        inDirectColor = CalLight(inDirectColor, -nextDir, myColor, hitInfo.normal, ray.direction, reflect);

        inDirectColor = inDirectColor * (1 - reflect) + mirrorColor * reflect;
        return 0.1f * dirColor + 0.9f * inDirectColor;
    }
}
