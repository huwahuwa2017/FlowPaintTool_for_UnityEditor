using System;
using UnityEngine;

namespace FlowPaintTool
{
    public class CameraControl2 : MonoBehaviour
    {
        [SerializeField]
        private float _verticalSpeed = 2f;

        [SerializeField]
        private float _horizontalSpeed = 2f;

        [SerializeField]
        private float _moveSpeed = 0.05f;

        [SerializeField]
        private int _inertia = 8;

        private Vector3 _eulerAngle = Vector3.zero;

        private Vector3Int _speedVector = Vector3Int.zero;

        private bool _key_w, _key_a, _key_s, _key_d, _key_space, _key_leftControl, _key_leftShift;

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
            _key_space = Input.GetKey(KeyCode.Space);
            _key_leftControl = Input.GetKey(KeyCode.LeftControl);
            _key_leftShift = Input.GetKey(KeyCode.LeftShift);

            if (Input.GetMouseButton(2))
            {
                _eulerAngle.x -= Input.GetAxis("Mouse Y") * _verticalSpeed;
                _eulerAngle.y += Input.GetAxis("Mouse X") * _horizontalSpeed;

                _eulerAngle.x = Mathf.DeltaAngle(0, _eulerAngle.x);
                _eulerAngle.y = Mathf.DeltaAngle(0, _eulerAngle.y);

                transform.rotation = Quaternion.Euler(_eulerAngle);
            }
        }

        private void FixedUpdate()
        {
            _inertia = Math.Max(_inertia, 1);

            if (_key_d) _speedVector.x += 1;
            if (_key_a) _speedVector.x -= 1;
            if (_key_space) _speedVector.y += 1;
            if (_key_leftControl) _speedVector.y -= 1;
            if (_key_w) _speedVector.z += 1;
            if (_key_s) _speedVector.z -= 1;

            if (!(_key_d || _key_a))
            {
                _speedVector.x -= Math.Sign(_speedVector.x);
            }

            if (!(_key_space || _key_leftControl))
            {
                _speedVector.y -= Math.Sign(_speedVector.y);
            }

            if (!(_key_w || _key_s))
            {
                _speedVector.z -= Math.Sign(_speedVector.z);
            }

            _speedVector.x = Mathf.Clamp(_speedVector.x, -_inertia, _inertia);
            _speedVector.y = Mathf.Clamp(_speedVector.y, -_inertia, _inertia);
            _speedVector.z = Mathf.Clamp(_speedVector.z, -_inertia, _inertia);

            float moveSpeed = (_key_leftShift) ? _moveSpeed * 3f : _moveSpeed;
            Vector3 speed = (Vector3)_speedVector / _inertia * moveSpeed;
            transform.position += transform.rotation * speed;
        }
    }
}