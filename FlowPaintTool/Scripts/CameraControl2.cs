using System;
using UnityEngine;

namespace FlowPaintTool
{
    public class CameraControl2 : MonoBehaviour
    {
        private Vector3 _eulerAngle = Vector3.zero;

        private Vector3Int _speedVector = Vector3Int.zero;

        private bool _key_w, _key_a, _key_s, _key_d, _key_e, _key_q, _key_leftShift;

        private void Start()
        {
            _eulerAngle = transform.rotation.eulerAngles;
        }

        private void Update()
        {
            _key_w = Input.GetKey(KeyCode.W);
            _key_a = Input.GetKey(KeyCode.A);
            _key_s = Input.GetKey(KeyCode.S);
            _key_d = Input.GetKey(KeyCode.D);
            _key_e = Input.GetKey(KeyCode.E);
            _key_q = Input.GetKey(KeyCode.Q);
            _key_leftShift = Input.GetKey(KeyCode.LeftShift);

            if (Input.GetMouseButton(2))
            {
                FlowPaintToolEditorData fpted = FlowPaintToolControl.FPT_EditorData;

                _eulerAngle.x -= Input.GetAxis("Mouse Y") * fpted.CameraVerticalRotateSpeed;
                _eulerAngle.y += Input.GetAxis("Mouse X") * fpted.CameraVerticalRotateSpeed;

                _eulerAngle.x = Mathf.DeltaAngle(0, _eulerAngle.x);
                _eulerAngle.y = Mathf.DeltaAngle(0, _eulerAngle.y);

                transform.rotation = Quaternion.Euler(_eulerAngle);
            }
        }

        private void FixedUpdate()
        {
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

            FlowPaintToolEditorData fpted = FlowPaintToolControl.FPT_EditorData;

            _speedVector.x = Mathf.Clamp(_speedVector.x, -fpted.CameraInertia, fpted.CameraInertia);
            _speedVector.y = Mathf.Clamp(_speedVector.y, -fpted.CameraInertia, fpted.CameraInertia);
            _speedVector.z = Mathf.Clamp(_speedVector.z, -fpted.CameraInertia, fpted.CameraInertia);

            float moveSpeed = (_key_leftShift) ? fpted.CameraMoveSpeed * 3f : fpted.CameraMoveSpeed;
            Vector3 speed = (Vector3)_speedVector / fpted.CameraInertia * moveSpeed;
            transform.position += transform.rotation * speed;
        }
    }
}