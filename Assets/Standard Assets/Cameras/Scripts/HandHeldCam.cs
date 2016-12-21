using System;
using UnityEngine;

namespace UnityStandardAssets.Cameras
{
    public class HandHeldCam : LookatTarget
    {
        public float swaySpeed = .5f;
        public float baseSwayAmount = .5f;
        public float trackingSwayAmount = .5f;
        [Range(-1, 1)]
        public float trackingBias = 0;


        protected override void FollowTarget(float deltaTime)
        {
            base.FollowTarget(deltaTime);

            float bx = (Mathf.PerlinNoise(0, Time.time*swaySpeed) - 0.5f);
            float by = (Mathf.PerlinNoise(0, (Time.time*swaySpeed) + 100)) - 0.5f;

            bx *= baseSwayAmount;
            by *= baseSwayAmount;

            float tx = (Mathf.PerlinNoise(0, Time.time*swaySpeed) - 0.5f) + trackingBias;
            float ty = ((Mathf.PerlinNoise(0, (Time.time*swaySpeed) + 100)) - 0.5f) + trackingBias;

            tx *= -trackingSwayAmount*m_FollowVelocity.x;
            ty *= trackingSwayAmount*m_FollowVelocity.y;

            transform.Rotate(bx + tx, by + ty, 0);
        }
    }
}
