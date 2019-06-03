using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
public class RayTracerNew : MonoBehaviour
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
        if (Physics.Raycast(ray, out hitInfo,100.0f))
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
        float specFactor = Mathf.Pow(Mathf.Max(Vector3.Dot(V, R), 0), 32);
        var spec = specFactor * lightColor * hitColor;
        return diffuse + spec * reflect;
    }
    static Vector3 GetLightDir(Light l)
    {
        return (l.transform.rotation * new Vector3(0, 0, 1)).normalized;
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

    static bool Refract(Vector3 v,Vector3 normal,float indices,out Vector3 r)
    {
        v.Normalize();
        float dt = Vector3.Dot(v, normal);
        float discr = 1.0f - indices * indices * (1 - dt * dt);
        if(discr > 0)
        {
            r = indices * (v - normal * dt) - normal * Mathf.Sqrt(discr);
            return true;
        }
        r = Vector3.zero;
        return false;
    }
    static Vector3 Refract(Vector3 v,Vector3 normal, float indices)
    {
        var rotDir = Vector3.Cross(normal, v);
        var theta1 = Mathf.Acos(Vector3.Dot(normal, -v));
        var theta2 = Mathf.Asin(Mathf.Sin(theta1) / indices);
        var delta = theta1 - theta2;
        var r =  Quaternion.AngleAxis(delta * Mathf.Rad2Deg, rotDir) * v;
        r.Normalize();
        return r;
    }
    public float refractor = 1.34f;
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

        float refract = mat.GetFloat("_Metallic");
        float reflect = mat.GetFloat("_Glossiness");

        var inDirectColor = Color.black;
        //refract
        if(refract > 0)
        {
            var refractIn = Vector3.Dot(ray.direction, hitInfo.normal) < 0;

            Vector3 refractDir;
            if(Refract(ray.direction, refractIn ? hitInfo.normal : -hitInfo.normal, refractIn ?  1.0f /refractor : refractor,out refractDir))
            {
                inDirectColor = TracingRay(new Ray(hitInfo.point, refractDir), currentStep + 1);
                inDirectColor = CalLight(inDirectColor, -refractDir, myColor, hitInfo.normal, ray.direction, 1);
            }

        }
        else
        {
            //mirror
            var nextDir = Vector3.Reflect(ray.direction, hitInfo.normal);
            nextDir.Normalize();
            var mirrorColor = TracingRay(new Ray(hitInfo.point, nextDir), currentStep + 1);
            mirrorColor = CalLight(mirrorColor, -nextDir, myColor, hitInfo.normal, ray.direction, reflect);

            //diff
            nextDir = hitInfo.normal + GenSemSphrereDir();
            nextDir.Normalize();
            inDirectColor = TracingRay(new Ray(hitInfo.point, nextDir), currentStep + 1);
            inDirectColor = CalLight(inDirectColor, -nextDir, myColor, hitInfo.normal, ray.direction, reflect);
            inDirectColor = inDirectColor * (1 - reflect) + mirrorColor * reflect;
        }


        var dirColor = Color.black;
        //shadow
        for (int i =0;i<lights.Length;++i)
        {
            if(lights[i].isActiveAndEnabled)
            {
                var lightDir = GetLightDir(lights[i]);
                if (!Physics.Raycast(hitInfo.point, -lightDir,100.0f))
                {
                    dirColor += CalLight(lights[i].color.linear, lightDir, myColor, hitInfo.normal, ray.direction, reflect);
                }
            }

        }        
        return  inDirectColor;
    }
}
