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
    class Camera
    {
        vec3 origin;
        vec3 lower_left_corner;
        vec3 horizontal;
        vec3 vertical;
        float openTime;
        float closeTime;
        float lenRadius;
        public Camera(vec3 lookfrom, vec3 lookat, vec3 head, float vfov, float aspect, float aperture, float focus_dist, float o, float c)
        {
            openTime = o;
            closeTime = c;
            float theta = vfov * MathF.PI / 180;
            float half_height = focus_dist * MathF.Tan(theta / 2);
            float half_width = aspect * half_height;

            lenRadius = aperture / 2.0f;
            origin = lookfrom;

            vec3 view = lookat - lookfrom;
            view = glm.normalize(view);
            vec3 right = glm.cross(view, head);
            right = glm.normalize(right);
            vec3 up = glm.cross(right, view);
            up = glm.normalize(up);

            lower_left_corner = origin - half_width * right - half_height * up + focus_dist * view;

            horizontal = 2.0f * half_width * right;
            vertical = 2.0f * half_height * up;
        }
        vec3 random_in_unit_disk()
        {
            vec3 p;
            do
            {
                p = 2.0f * new vec3(Exten.rand01(), Exten.rand01(), 0) - new vec3(1, 1, 0);
            } while (glm.dot(p, p) >= 1.0f);
            return p;
        }
        public Ray GenRay(float s, float t)
        {
            vec3 offset = lenRadius * random_in_unit_disk();
            var point = origin + offset;
            var time = openTime + (closeTime - openTime) * Exten.rand01();
            return new Ray(point, lower_left_corner + s * horizontal + t * vertical - point, time);
        }

    }
}