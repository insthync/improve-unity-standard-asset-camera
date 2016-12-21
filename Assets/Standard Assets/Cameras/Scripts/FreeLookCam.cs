using System;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

namespace UnityStandardAssets.Cameras
{
    public class FreeLookCam : PivotBasedCameraRig
    {
        // This script is designed to be placed on the root object of a camera rig,
        // comprising 3 gameobjects, each parented to the next:

        // 	Camera Rig
        // 		Pivot
        // 			Camera

        public float moveSpeed = 1f;                      // How fast the rig will move to keep up with the target's position.
        [Range(0f, 10f)]
        public float turnSpeed = 1.5f;   // How fast the rig will rotate from user input.
        public float turnSmoothing = 0.0f;                // How much smoothing to apply to the turn input, to reduce mouse-turn jerkiness
        public float tiltMax = 75f;                       // The maximum value of the x axis rotation of the pivot.
        public float tiltMin = 45f;                       // The minimum value of the x axis rotation of the pivot.
        public float minZoomDistance = 2f;                // How far in front of the pivot the character's look target is.
        public float maxZoomDistance = 10f;
        public float zoomSpeed = 2f;
        public float zoomMoveTime = 0.4f;
        public bool enabledRotation = true;
        public bool enabledZoom = true;
        public bool lockCursor = false;                   // Whether the cursor should be hidden and locked.
        public bool verticalAutoReturn = false;           // set wether or not the vertical axis should auto return
        public ProtectCameraFromWallClip wallClipProtector;

        protected float m_LookAngle;                    // The rig's y axis rotation.
        protected float m_TiltAngle;                    // The pivot's x axis rotation.
        protected float m_OriginalDist;
        protected float m_CurrentDist;
        protected float m_CurrentZoomDist;
        protected float m_MoveVelocity;             // the velocity at which the camera moved
        protected Vector3 m_PivotEulers;
        protected Quaternion m_PivotTargetRot;
        protected Quaternion m_TransformTargetRot;
        protected bool m_LockCursorDirty;

        public static FreeLookCam Singleton { get; protected set; }

        protected static FreeLookCam CreateCamera()
        {
            FreeLookCam newCamera = new GameObject("_FreeLookCam").AddComponent<FreeLookCam>();
            GameObject pivot = new GameObject("_FreeLookCam_pivot");
            pivot.transform.SetParent(newCamera.transform);
            pivot.transform.localPosition = Vector3.zero;
            pivot.transform.localScale = Vector3.one;
            pivot.transform.localEulerAngles = Vector3.zero;
            GameObject camera = new GameObject("_FreeLookCam_camera");
            camera.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.SetParent(pivot.transform);
            camera.transform.localPosition = Vector3.zero;
            camera.transform.localScale = Vector3.one;
            camera.transform.localEulerAngles = Vector3.zero;

            return newCamera;
        }

        protected override void Awake()
        {
            base.Awake();
            // Lock or unlock the cursor.
            Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !lockCursor;
            m_PivotEulers = m_Pivot.rotation.eulerAngles;

            m_PivotTargetRot = m_Pivot.transform.localRotation;
            m_TransformTargetRot = transform.localRotation;
            
            m_OriginalDist = m_Cam.localPosition.magnitude;
            m_CurrentDist = m_OriginalDist;
            m_CurrentZoomDist = m_CurrentDist;

            if (wallClipProtector == null)
                wallClipProtector = GetComponent<ProtectCameraFromWallClip>();

            Singleton = this;
        }
        
        protected void Update()
        {
            if (enabledRotation)
                HandleRotationMovement();

            if (enabledZoom)
                HandleZoom();

            if (lockCursor != m_LockCursorDirty)
            {
                Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
                Cursor.visible = !lockCursor;
                m_LockCursorDirty = lockCursor;
            }
        }

        protected void LateUpdate()
        {
            if (enabledZoom && wallClipProtector == null)
            {
                m_CurrentZoomDist = Mathf.SmoothDamp(m_CurrentZoomDist, m_CurrentDist, ref m_MoveVelocity, zoomMoveTime);
                m_Cam.localPosition = -Vector3.forward * m_CurrentZoomDist;
            }
        }
        
        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        
        protected override void FollowTarget(float deltaTime)
        {
            if (target == null) return;
            // Move the rig towards target position.
            transform.position = Vector3.Lerp(transform.position, target.position, deltaTime * moveSpeed);
        }
        
        private void HandleRotationMovement()
        {
            if (Time.timeScale < float.Epsilon)
                return;

            // Read the user input
            var x = CrossPlatformInputManager.GetAxis("Mouse X");
            var y = CrossPlatformInputManager.GetAxis("Mouse Y");

            // Adjust the look angle by an amount proportional to the turn speed and horizontal input.
            m_LookAngle += x * turnSpeed;

            // Rotate the rig (the root object) around Y axis only:
            m_TransformTargetRot = Quaternion.Euler(0f, m_LookAngle, 0f);

            if (verticalAutoReturn)
            {
                // For tilt input, we need to behave differently depending on whether we're using mouse or touch input:
                // on mobile, vertical input is directly mapped to tilt value, so it springs back automatically when the look input is released
                // we have to test whether above or below zero because we want to auto-return to zero even if min and max are not symmetrical.
                m_TiltAngle = y > 0 ? Mathf.Lerp(0, -tiltMin, y) : Mathf.Lerp(0, tiltMax, -y);
            }
            else
            {
                // on platforms with a mouse, we adjust the current angle based on Y mouse input and turn speed
                m_TiltAngle -= y * turnSpeed;
                // and make sure the new value is within the tilt range
                m_TiltAngle = Mathf.Clamp(m_TiltAngle, -tiltMin, tiltMax);
            }

            // Tilt input around X is applied to the pivot (the child of this object)
            m_PivotTargetRot = Quaternion.Euler(m_TiltAngle, m_PivotEulers.y, m_PivotEulers.z);

            if (turnSmoothing > 0)
            {
                m_Pivot.localRotation = Quaternion.Slerp(m_Pivot.localRotation, m_PivotTargetRot, turnSmoothing * Time.deltaTime);
                transform.localRotation = Quaternion.Slerp(transform.localRotation, m_TransformTargetRot, turnSmoothing * Time.deltaTime);
            }
            else
            {
                m_Pivot.localRotation = m_PivotTargetRot;
                transform.localRotation = m_TransformTargetRot;
            }
        }

        private void HandleZoom()
        {
            // zoom (speed scales with distance)
            var scroll = CrossPlatformInputManager.GetAxis("Mouse ScrollWheel");
            var step = scroll * zoomSpeed;
            m_CurrentDist = Mathf.Clamp(m_CurrentDist - step, minZoomDistance, maxZoomDistance);

            if (wallClipProtector != null)
                wallClipProtector.lookDistance = m_CurrentDist;
        }
    }
}
