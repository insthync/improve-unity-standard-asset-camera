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

        public float m_MoveSpeed = 1f;                      // How fast the rig will move to keep up with the target's position.
        [Range(0f, 10f)]
        public float m_TurnSpeed = 1.5f;   // How fast the rig will rotate from user input.
        public float m_TurnSmoothing = 0.0f;                // How much smoothing to apply to the turn input, to reduce mouse-turn jerkiness
        public float m_TiltMax = 75f;                       // The maximum value of the x axis rotation of the pivot.
        public float m_TiltMin = 45f;                       // The minimum value of the x axis rotation of the pivot.
        public float m_MinZoomDistance = 2f;                // How far in front of the pivot the character's look target is.
        public float m_MaxZoomDistance = 10f;
        public float m_ZoomSpeed = 2f;
        public float m_ZoomMoveTime = 0.4f;
        public bool m_EnabledRotation = true;
        public bool m_EnabledZoom = true;
        public bool m_LockCursor = false;                   // Whether the cursor should be hidden and locked.
        public bool m_VerticalAutoReturn = false;           // set wether or not the vertical axis should auto return
        public ProtectCameraFromWallClip m_WallClipProtector;

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
            Cursor.lockState = m_LockCursor ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !m_LockCursor;
            m_PivotEulers = m_Pivot.rotation.eulerAngles;

            m_PivotTargetRot = m_Pivot.transform.localRotation;
            m_TransformTargetRot = transform.localRotation;
            
            m_OriginalDist = m_Cam.localPosition.magnitude;
            m_CurrentDist = m_OriginalDist;
            m_CurrentZoomDist = m_CurrentDist;

            if (m_WallClipProtector == null)
                m_WallClipProtector = GetComponent<ProtectCameraFromWallClip>();

            Singleton = this;
        }
        
        protected void Update()
        {
            if (m_EnabledRotation)
                HandleRotationMovement();

            if (m_EnabledZoom)
                HandleZoom();

            if (m_LockCursor != m_LockCursorDirty)
            {
                Cursor.lockState = m_LockCursor ? CursorLockMode.Locked : CursorLockMode.None;
                Cursor.visible = !m_LockCursor;
                m_LockCursorDirty = m_LockCursor;
            }
        }

        protected void LateUpdate()
        {
            if (m_EnabledZoom && m_WallClipProtector == null)
            {
                m_CurrentZoomDist = Mathf.SmoothDamp(m_CurrentZoomDist, m_CurrentDist, ref m_MoveVelocity, m_ZoomMoveTime);
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
            if (m_Target == null) return;
            // Move the rig towards target position.
            transform.position = Vector3.Lerp(transform.position, m_Target.position, deltaTime * m_MoveSpeed);
        }
        
        private void HandleRotationMovement()
        {
            if (Time.timeScale < float.Epsilon)
                return;

            // Read the user input
            var x = CrossPlatformInputManager.GetAxis("Mouse X");
            var y = CrossPlatformInputManager.GetAxis("Mouse Y");

            // Adjust the look angle by an amount proportional to the turn speed and horizontal input.
            m_LookAngle += x * m_TurnSpeed;

            // Rotate the rig (the root object) around Y axis only:
            m_TransformTargetRot = Quaternion.Euler(0f, m_LookAngle, 0f);

            if (m_VerticalAutoReturn)
            {
                // For tilt input, we need to behave differently depending on whether we're using mouse or touch input:
                // on mobile, vertical input is directly mapped to tilt value, so it springs back automatically when the look input is released
                // we have to test whether above or below zero because we want to auto-return to zero even if min and max are not symmetrical.
                m_TiltAngle = y > 0 ? Mathf.Lerp(0, -m_TiltMin, y) : Mathf.Lerp(0, m_TiltMax, -y);
            }
            else
            {
                // on platforms with a mouse, we adjust the current angle based on Y mouse input and turn speed
                m_TiltAngle -= y * m_TurnSpeed;
                // and make sure the new value is within the tilt range
                m_TiltAngle = Mathf.Clamp(m_TiltAngle, -m_TiltMin, m_TiltMax);
            }

            // Tilt input around X is applied to the pivot (the child of this object)
            m_PivotTargetRot = Quaternion.Euler(m_TiltAngle, m_PivotEulers.y, m_PivotEulers.z);

            if (m_TurnSmoothing > 0)
            {
                m_Pivot.localRotation = Quaternion.Slerp(m_Pivot.localRotation, m_PivotTargetRot, m_TurnSmoothing * Time.deltaTime);
                transform.localRotation = Quaternion.Slerp(transform.localRotation, m_TransformTargetRot, m_TurnSmoothing * Time.deltaTime);
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
            var step = scroll * m_ZoomSpeed;
            m_CurrentDist = Mathf.Clamp(m_CurrentDist - step, m_MinZoomDistance, m_MaxZoomDistance);

            if (m_WallClipProtector != null)
                m_WallClipProtector.lookDistance = m_CurrentDist;
        }
    }
}
