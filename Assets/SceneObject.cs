using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class SceneObject : MonoBehaviour
{
    private MeshRenderer render;
    private Collider collider;

    private void Start()
    {
        render = GetComponent<MeshRenderer>();
        collider = GetComponent<Collider>();
        _emittance = render.sharedMaterial.GetColor("_EmissionColor");
        _reflectance = render.sharedMaterial.color;
    }
    public bool RayCast(Ray ray, out RaycastHit hit)
    {
        if (collider == null)
        {
            collider = GetComponent<Collider>();
        }
        return collider.Raycast(ray, out hit, float.MaxValue);
    }

    private Color _emittance;
    public Color Emittance
    {
        get
        {
            return _emittance;
        }
    }
    private Color _reflectance;
    public Color reflectance
    {
        get
        {
            return _reflectance;
        }
    }

}

