using System;
using UnityEngine;
using JoyconLib;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Sandbox
{
    public class JoyconPoseTest : MonoBehaviour
    {

        [SerializeField] private JoyconSide side;
        [SerializeField] private RotationMode rotationMode;

        private Joycon joycon = null;
        private Vector3 gyro;
        private Vector3 accel;
        private Quaternion orientation;

        private void Start()
        {
            foreach(var j in JoyconManager.Instance.j)
            {
                if (j.isLeft == IsLeft()) 
                {
                    joycon = j;
                    break;
                }
            }

            // Joy-Conが存在しない場合は警告。
            if (joycon is null) 
            {
                Debug.LogWarning("Joycon doesn't exist.", gameObject);
            }
        }

        private void Update()
        {
            // Joy-Conがなければ更新処理を行わない。
            if (joycon is null) { return; }

            // 補正
            if (joycon.GetButtonDown(Joycon.Button.DPAD_RIGHT))
            {
                joycon.Recenter();

                // Gyro用に回転を初期化する
                transform.rotation = Quaternion.identity;
                Debug.Log("Recentered.", gameObject);
            }
            // 回転モード変更
            if (joycon.GetButtonDown(Joycon.Button.DPAD_LEFT))
            {
                rotationMode = (RotationMode)(((int)rotationMode + 1) % (Enum.GetNames(typeof(RotationMode)).Length));
                Debug.Log($"Rotation Mode changed. (cur:{rotationMode})", gameObject);
            }

            // 値を更新
            gyro = this.joycon.GetGyro();
            accel = this.joycon.GetAccelWithoutGravity();
            orientation = this.joycon.GetVector();

            // 姿勢を合わせる
            switch (rotationMode)
            {
                case RotationMode.Orientation:
                    {
                        transform.rotation = orientation;
                    }
                    break;
                case RotationMode.Gyro:
                    {
                        transform.Rotate(gyro);
                    }
                    break;
                default:
                    break;
            }
        }

        private bool IsLeft()
        {
            return side == JoyconSide.Left;
        }

        enum JoyconSide
        {
            Left,
            Right
        }

        enum RotationMode
        {
            Orientation,
            Gyro
        }

#if UNITY_EDITOR

        private void OnDrawGizmosSelected()
        {
            var euler = orientation.eulerAngles;

            var str = 
                $"rot:{euler}\n" +
                $"gyro:{this.gyro}\n" +
                $"accel:{this.accel}";
            Handles.Label(transform.position, str);

            void DrawVector(in Vector3 vec, in Color col)
            {
                var orig = transform.position;
                var target = orig + vec;

                var tmp = Handles.color;
                Handles.color = col;
                Handles.DrawLine(orig, target);
                Handles.color = tmp;
            }
            DrawVector(accel, Color.red);
        }

#endif
    }
}
