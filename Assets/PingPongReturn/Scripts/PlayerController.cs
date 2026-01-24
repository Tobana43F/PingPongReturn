using JoyconLib;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using static TableLocatorGetter;

public class PlayerController : MonoBehaviour
{
    enum HandState
    {
        BackToDefault,
        Focus,
        Fixed,
        Wait,
    }

    private Joycon joycon = null;
    private Vector3 gyro = Vector3.zero;
    private Vector3 accel = Vector3.zero;
    private Quaternion orientation = Quaternion.identity;

    [SerializeField] private GameObject handObject = null;
    [SerializeField] private Racket racket = null;
    [SerializeField] private TableLocatorGetter receiveDepthGetter = null;

    [Space]
    [SerializeField] AudioSource receiveAudioSource;

    List<Vector3> axisAccels = new List<Vector3>(new Vector3[10]);

    HandState handState = HandState.BackToDefault;
    Vector3 defaultPosition;
    float stateTime = 0;

    // TMP: 仮実装
    BallController ball;
    Vector3 receivePosition;

    private void Awake()
    {
        Assert.IsNotNull(handObject);
        Assert.IsNotNull(racket);
        Assert.IsNotNull(receiveDepthGetter);
    }

    void Start()
    {
        InitializeJoycon();

        // ボール衝突時の処理を登録する
        racket.onBallEnter += OnBallEnter;

        defaultPosition = handObject.transform.position;
    }

    void Update()
    {
        UpdateJoycon();
        UpdateAxisAccels();

        UpdateHand();
    }

    private void OnBallEnter(BallController ballController)
    {
        if (handState != HandState.Focus) { return; }

        var dir = racket.transform.forward.normalized;

        // バック面の時は向きを反転させる
        var frontDirection = receiveDepthGetter.ReceiveFrontDirection;
        if (Vector3.Dot(dir, frontDirection) < 0)
        {
            dir *= -1;
        }

        // 球を返す力を決定する
        // TMP: 加速度線さの変化が敏感なので手触りの調整が必要ありそう
        //      Ex)過去数フレームの最大値とか
        var impulse = TableLocatorGetter.ReceiveImpulse.Low;
        {
            var axisAccel = GetAxisAccelMax();

            //float hitImpulse = Mathf.Max(Vector3.Dot(dir, axisAccel), 0);
            float hitImpulse = Mathf.Abs(Vector3.Dot(dir, axisAccel));

            Debug.Log($"hitImpulse{hitImpulse}");

            // TMP: 仮実装
            if (hitImpulse < 0.1)
            {
                impulse = ReceiveImpulse.Low;
            }
            else if (hitImpulse < 0.7)
            {
                impulse = ReceiveImpulse.Mid;
            }
            else
            {
                impulse = ReceiveImpulse.High;
            }
        }

        ballController.Receive(dir, racket, impulse, receiveDepthGetter);
        ball = null;

        receiveAudioSource.Play();

        // 状態遷移
        handState = HandState.Wait;
        stateTime = 0.3f;
    }

    // TODO: 公開せずイベントに登録するように実装を変更する
    public void OnServe()
    {
        handState = HandState.Focus;
    }

    // TODO: 実装を変更する（テーブルから情報を取得するように変更する）
    public void RegisterBall(BallController ball)
    {
        this.ball = ball;
    }
    public void RegisterReceivePosition(Vector3 receivePosition)
    {
        this.receivePosition = receivePosition;
    }

    void InitializeJoycon()
    {
        // 右手のジョイ婚を探す
        foreach (var j in JoyconManager.Instance.j)
        {
            //if (j.isLeft == false)
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

    void UpdateJoycon()
    {
        if (joycon is null)
        {
            // Joyconの情報が取れていない場合は再度取得する
            InitializeJoycon();
            return;
        }

        // 補正
        if (joycon.GetButtonDown(Joycon.Button.DPAD_RIGHT))
        {
            joycon.Recenter();
        }

        // リセット処理
        if (joycon.GetButtonDown(Joycon.Button.DPAD_UP))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        // 値を更新
        this.gyro = this.joycon.GetGyro();
        this.accel = this.joycon.GetAccelWithoutGravity();
        this.orientation = this.joycon.GetVector();
    }
    void UpdateAxisAccels()
    {
        // 配列をずらす
        for (int i = axisAccels.Count - 1; i > 0; --i)
        {
            axisAccels[i] = axisAccels[i - 1];
        }

        // 最新情報で更新
        axisAccels[0] = orientation * accel;
    }

    void UpdateHand()
    {
        handObject.transform.rotation = orientation;

        switch (handState)
        {
            case HandState.BackToDefault:
            {
                // TODO: デフォルト位置に向かわせる
                var pos = handObject.transform.position;
                var vec = defaultPosition - pos;

                var approachingRate = 5f;
                pos += (vec * approachingRate) * Time.deltaTime;

                handObject.transform.position = pos;
            }
            break;
            case HandState.Focus:
            {
                // レシーブポイントに向かわせる
                {
                    var pos = handObject.transform.position;
                    var vec = receivePosition - pos;

                    var approachingRate = 10f;
                    pos += (vec * approachingRate) * Time.deltaTime;

                    handObject.transform.position = pos;
                }

                // TODO: 目標位置についたら固定する
            }
            break;
            case HandState.Fixed:
            {

            }
            break;
            case HandState.Wait:
            {
                if (stateTime <= 0)
                {
                    handState = HandState.BackToDefault;
                }
                stateTime -= Time.deltaTime;
            }
            break;
            default:
                break;
        }
    }


    Vector3 GetAxisAccelAvg()
    {
        Debug.Log("----");
        var avg = Vector3.zero;
        foreach (var elem in axisAccels)
        {
            Debug.Log($"Acesl {elem}");
            avg += elem;
        }
        Debug.Log("----");

        avg /= axisAccels.Count;
        return avg;
    }
    Vector3 GetAxisAccelMax()
    {
        Debug.Log("----");

        var max = Vector3.zero;
        foreach (var elem in axisAccels)
        {
            Debug.Log($"Acesl {elem}");

            if (elem.sqrMagnitude >= max.sqrMagnitude)
            {
                max = elem;
            }
        }
        Debug.Log("----");
        return max;
    }

#if UNITY_EDITOR

    private void OnDrawGizmos()
    {
        var pos = racket.transform.position;
        var axisAccel = orientation * accel;
        Handles.DrawLine(pos, pos + axisAccel);

        Handles.Label(defaultPosition, $"State:{nameof(handState)}");

    }

#endif
}
