using System;
using UnityEngine;


namespace UnityStandardAssets.Cameras
{
    public class TargetFieldOfView : AbstractTargetFollower
    {
        // This script is primarily designed to be used with the "LookAtTarget" script to enable a
        // CCTV style camera looking at a target to also adjust its field of view (zoom) to fit the
        // target (so that it zooms in as the target becomes further away).
        // When used with a follow cam, it will automatically use the same target.

        public float fovAdjustTime = 1;             // the time taken to adjust the current FOV to the desired target FOV amount.
        public float zoomAmountMultiplier = 2;      // a multiplier for the FOV amount. The default of 2 makes the field of view twice as wide as required to fit the target.
        public bool includeEffectsInSize = false;   // changing this only takes effect on startup, or when new target is assigned.

        private float m_BoundSize;
        private float m_FovAdjustVelocity;
        private Camera m_Cam;
        private Transform m_LastTarget;

        // Use this for initialization
        protected override void Start()
        {
            base.Start();
            m_BoundSize = MaxBoundsExtent(target, includeEffectsInSize);

            // get a reference to the actual camera component:
            m_Cam = GetComponentInChildren<Camera>();
        }


        protected override void FollowTarget(float deltaTime)
        {
            // calculate the correct field of view to fit the bounds size at the current distance
            float dist = (target.position - transform.position).magnitude;
            float requiredFOV = Mathf.Atan2(m_BoundSize, dist)*Mathf.Rad2Deg*zoomAmountMultiplier;

            m_Cam.fieldOfView = Mathf.SmoothDamp(m_Cam.fieldOfView, requiredFOV, ref m_FovAdjustVelocity, fovAdjustTime);
        }


        public override void SetTarget(Transform newTransform)
        {
            base.SetTarget(newTransform);
            m_BoundSize = MaxBoundsExtent(newTransform, includeEffectsInSize);
        }


        public static float MaxBoundsExtent(Transform obj, bool includeEffects)
        {
            // get the maximum bounds extent of object, including all child renderers,
            // but excluding particles and trails, for FOV zooming effect.

            var renderers = obj.GetComponentsInChildren<Renderer>();

            Bounds bounds = new Bounds();
            bool initBounds = false;
            foreach (Renderer r in renderers)
            {
                if (!((r is TrailRenderer) || (r is ParticleSystemRenderer)))
                {
                    if (!initBounds)
                    {
                        initBounds = true;
                        bounds = r.bounds;
                    }
                    else
                    {
                        bounds.Encapsulate(r.bounds);
                    }
                }
            }
            float max = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
            return max;
        }
    }
}
