using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

    public class SceneRayHit
    {
        public Vector3 point;
        public Vector3 normal;
        public SceneObject HitObj;
        public float distance;
    }
public class Scene : MonoBehaviour
{
    private SceneObject[] SceneObjects;
    private void Start()
    {
        SceneObjects = GetComponentsInChildren<SceneObject>();
    }
    public SceneRayHit RayCast(Ray ray)
    {
        SceneRayHit hitObj = new SceneRayHit();
        for (int i = 0; i < SceneObjects.Length; ++i)
        {
            RaycastHit hit;
            if (SceneObjects[i].RayCast(ray, out hit))
            {
                if (hitObj.HitObj == null || hit.distance < hitObj.distance)
                {
                    hitObj.HitObj = SceneObjects[i];
                    hitObj.normal = hit.normal;
                    hitObj.point = hit.point;
                    hitObj.distance = hit.distance;
                }
            }
        }
        return hitObj.HitObj != null ? hitObj : null;
    }
}

