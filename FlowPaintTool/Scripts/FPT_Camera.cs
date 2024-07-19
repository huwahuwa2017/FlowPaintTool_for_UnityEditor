#if UNITY_EDITOR

using System;
using UnityEngine;

namespace FlowPaintTool
{
    public class FPT_Camera : MonoBehaviour
    {
        private Vector3 _eulerAngle = Vector3.zero;
        private Vector3Int _speedVector = Vector3Int.zero;
        private bool _key_w, _key_a, _key_s, _key_d, _key_e, _key_q, _key_leftShift;

        private bool _focus = false;

        private FPT_Core _fptCore = null;
        private FPT_EditorData _fptEditorData = null;

        public void ManualStart()
        {
            _eulerAngle = transform.rotation.eulerAngles;
            _fptCore = FPT_Core.GetSingleton();
            _fptEditorData = FPT_EditorData.GetSingleton();
        }

        private void Update()
        {
            if (!_focus)
                return;

            _key_w = Input.GetKey(KeyCode.W);
            _key_a = Input.GetKey(KeyCode.A);
            _key_s = Input.GetKey(KeyCode.S);
            _key_d = Input.GetKey(KeyCode.D);
            _key_e = Input.GetKey(KeyCode.E);
            _key_q = Input.GetKey(KeyCode.Q);
            _key_leftShift = Input.GetKey(KeyCode.LeftShift);

            if (Input.GetMouseButton(2))
            {
                float cameraRotateSpeed = _fptEditorData.GetCameraRotateSpeed();

                _eulerAngle.x -= Input.GetAxis("Mouse Y") * cameraRotateSpeed;
                _eulerAngle.y += Input.GetAxis("Mouse X") * cameraRotateSpeed;

                _eulerAngle.x = Mathf.DeltaAngle(0, _eulerAngle.x);
                _eulerAngle.y = Mathf.DeltaAngle(0, _eulerAngle.y);

                transform.rotation = Quaternion.Euler(_eulerAngle);
            }
        }

        private void FixedUpdate()
        {
            _focus = _fptCore.GetPrePreFocus();

            if (_key_d) _speedVector.x += 1;
            if (_key_a) _speedVector.x -= 1;
            if (_key_e) _speedVector.y += 1;
            if (_key_q) _speedVector.y -= 1;
            if (_key_w) _speedVector.z += 1;
            if (_key_s) _speedVector.z -= 1;

            if (!(_key_d || _key_a))
            {
                _speedVector.x -= Math.Sign(_speedVector.x);
            }

            if (!(_key_e || _key_q))
            {
                _speedVector.y -= Math.Sign(_speedVector.y);
            }

            if (!(_key_w || _key_s))
            {
                _speedVector.z -= Math.Sign(_speedVector.z);
            }

            int cameraInertia = _fptEditorData.GetCameraInertia();

            _speedVector.x = Mathf.Clamp(_speedVector.x, -cameraInertia, cameraInertia);
            _speedVector.y = Mathf.Clamp(_speedVector.y, -cameraInertia, cameraInertia);
            _speedVector.z = Mathf.Clamp(_speedVector.z, -cameraInertia, cameraInertia);

            float moveSpeed = _fptEditorData.GetCameraMoveSpeed();
            moveSpeed = (_key_leftShift) ? moveSpeed * 3f : moveSpeed;

            Vector3 speed = (Vector3)_speedVector / cameraInertia * moveSpeed;
            transform.position += transform.rotation * speed;
        }
    }
}

#endif
