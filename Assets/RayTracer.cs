using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        }
        target.Apply();
        TraceOut.texture = target;
    }
    Color CalLight(Color lightColor,Vector3 lightDir,Color hitColor,Vector3 hitNormal,Vector3 viewDir)
    {
        var diffuse = Vector3.Dot(hitNormal, -lightDir) * lightColor * hitColor;
        var V = -viewDir;
        var R = Vector3.Reflect(hitNormal, lightDir);
        var spec = Mathf.Pow(Vector3.Dot(V, R), 32) * lightColor * hitColor;
        return diffuse + spec;
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
    Color TracingRay(Ray ray,int currentStep=0)
    {
        if(currentStep >= MaxTraceStep)
        {
            return Color.gray;
        }
        RaycastHit hitInfo;
        if(Physics.Raycast(ray, out hitInfo))
        {

            var mat = hitInfo.transform.GetComponent<MeshRenderer>().sharedMaterial;
            var myColor = mat.color.linear;
            var texture = mat.mainTexture as Texture2D;
            if ( texture !=  null)
            {
                float x = hitInfo.textureCoord.x * texture.width;
                float y = hitInfo.textureCoord.y * texture.height;
                myColor = texture.GetPixel((int)x, (int)y);
            }

            //shadow
            var lightDir = GetLightDir(lights[0]);
            if(!Physics.Raycast(hitInfo.point,-lightDir))
            {
                myColor = CalLight(lights[0].color,lightDir , myColor, hitInfo.normal, ray.direction);
            }
            else
            {
                myColor = Color.black;
            }

            //mirror
            var nextDir = Vector3.Reflect(ray.direction, hitInfo.normal);
            var mirrorColor = TracingRay(new Ray(hitInfo.point, nextDir), currentStep + 1);
            mirrorColor = CalLight(mirrorColor, -nextDir, myColor, hitInfo.normal, ray.direction);
            //diff
            nextDir = hitInfo.normal + GenSemSphrereDir();
            nextDir.Normalize();
            var inDirectColor = TracingRay(new Ray(hitInfo.point, nextDir), currentStep + 1);
            inDirectColor = CalLight(inDirectColor, -nextDir, myColor, hitInfo.normal, ray.direction);
            float reflect = mat.GetFloat("_Glossiness");
            inDirectColor = inDirectColor * (1 - reflect) + mirrorColor * reflect;
            return 0.2f * myColor  + 0.8f* inDirectColor;
        }
        else
        {
            return renderCamera.backgroundColor.linear;
        }

    }
}
