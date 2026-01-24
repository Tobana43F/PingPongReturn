using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

public class BallController : MonoBehaviour
{
    private enum ControlMode
    {
        Physics,
        Receive,
        Constraint,
        Toss,
    }

    private enum ReceiveState
    {
        None,
        SuccessIn,
        MissOut,
        MissNet,
        MissUnreached,
    }

    /// <summary>
    /// 見た目の制御で使用するオブジェクト
    /// </summary>
    [SerializeField] GameObject ballModelObject;
    [SerializeField] SphereCollider mainCollider;

    [SerializeField] LayerMask dropRaycastTargetLayerMask;

    [Space]
    [SerializeField] float SpinAttenuationRate = 0.2f;
    [SerializeField] float MagnusForceEffectRate = 0.002f;

    [Space]
    [SerializeField] AudioSource boundAudioSource;
    [SerializeField] List<PhysicMaterial> boundSoundPhysicsMaterial;


    [SerializeField] ScoreEffect score0Effet;
    [SerializeField] ScoreEffect score1Effet;
    [SerializeField] ScoreEffect score2Effet;
    [SerializeField] ScoreEffect score3Effet;

    // スコア確定時のイベント
    public event Action<int /*score*/> onScore;

    // 物理制御情報
    Rigidbody ballRigidbody;
    bool isInAir = true;
    bool isMagnusForceEnabled = true;

    // 打球時の情報
    private Vector3 hitVector;
    private Vector3 hitSpinAxis;
    private float hitSpinSpeed;
    private float curSpinSpeed;

    // レシーブ情報
    bool isReceived = false;
    Vector3 receivePos;
    Vector3 receiveDirection;
    Vector3 dropPos;
    Vector3 receiveControlPoint;
    float receiveWholeTime;
    float receiveElapsedTime;
    ReceiveState receiveState = ReceiveState.None;

    bool isLifeTimeCountEnabled = false;
    float ellapsedTimeFromReceive = 0;
    bool isScored = false;

    // トス情報
    Vector3 tossStartPos;
    Vector3 tossEndPos;
    Vector3 tossControlPoint;
    float tossWholeTime;
    float tossElapsedTime;

    List<ScoreMultiplier> touchingScoreMultipliers = new List<ScoreMultiplier>();

    bool IsReceiveComplete
    {
        get
        {
            return isReceived && (receiveElapsedTime >= receiveWholeTime);
        }
    }
    bool IsTossComplete
    {
        get
        {
            return tossElapsedTime >= tossWholeTime;
        }
    }

    ControlMode __controlMode = ControlMode.Physics;
    ControlMode CurControlMode
    {
        get => __controlMode;
        set
        {
            __controlMode = value;

            // リジッドボディの状態も併せて変更する
            // TODO: 物理更新の最後で行うように変更する（WaitForFixedUpdate）
            if (ballRigidbody)
            {
                switch (__controlMode)
                {
                    case ControlMode.Physics:
                        ballRigidbody.isKinematic = false;
                        break;

                    case ControlMode.Receive:
                    case ControlMode.Constraint:
                    case ControlMode.Toss:
                        ballRigidbody.isKinematic = true;
                        break;

                    default:
                        break;
                }
            }
        }
    }

    private float BallRadius
    {
        get
        {
            Assert.IsNotNull(mainCollider);
            return mainCollider.radius;
        }
    }

    private void Awake()
    {
        Debug.Assert(ballModelObject is not null);

        // 保持しておく
        ballRigidbody = GetComponent<Rigidbody>();
        Debug.Assert(ballRigidbody is not null);
    }

    private void Update()
    {
        switch (CurControlMode)
        {
            case ControlMode.Physics:
            case ControlMode.Receive:
            {
                // 見た目の回転処理を行う
                ballModelObject.transform.Rotate(hitSpinAxis, curSpinSpeed * Time.deltaTime);
            }
            break;

            case ControlMode.Constraint:
            {
                // HACK: 調査）一度設定したはずが、位置がずれるので毎フレーム位置を合わせている
                transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
            break;

            default:
                break;
        }

        // レシーブ後の挙動
        if (receiveState != ReceiveState.None)
        {
            void DestorySelf()
            {
                Debug.Log($"Destory ball.");
                Destroy(this.gameObject);
            }
            void OnScore(int score)
            {
                // 一度のみ処理する
                if (isScored) { return; }
                Debug.Log($"Score: {score}, state: {receiveState})");

                boundAudioSource.pitch = 1.0f;
                if (score == 0)
                {
                    Instantiate(score0Effet, transform.position, Quaternion.identity);
                }
                else
                {
                    if (touchingScoreMultipliers.Count() > 0)
                    {
                        var maxMultiplier = touchingScoreMultipliers.Select(e => e.Multiplier).Max();
                        score *= maxMultiplier;

                        if (maxMultiplier == 2)
                        {
                            Instantiate(score2Effet, transform.position, Quaternion.identity);
                        }
                        else
                        {
                            Instantiate(score3Effet, transform.position, Quaternion.identity);
                        }
                    }
                    else
                    {
                        Instantiate(score1Effet, transform.position, Quaternion.identity);
                    }
                }

                onScore?.Invoke(score);
                isScored = true;
            }

            if (receiveState == ReceiveState.SuccessIn)
            {
                if (IsReceiveComplete)
                {
                    // TODO: スコア計算（倍率ゾーン対応）
                    OnScore(1);
                    DestorySelf();
                }
            }
            else if (receiveState == ReceiveState.MissOut)
            {
                if (IsReceiveComplete)
                {
                    // レシーブ処理完了時にスコア確定
                    OnScore(0);

                    // 寿命を設定
                    isLifeTimeCountEnabled = true;
                }

                // 寿命が来たら削除
                if (isLifeTimeCountEnabled && ellapsedTimeFromReceive >= 2.0f)
                {
                    DestorySelf();
                }
            }
            else if (receiveState == ReceiveState.MissUnreached ||
                receiveState == ReceiveState.MissNet)
            {
                // TODO: 調整項目にする
                // 一定時間経過後にスコア、削除
                if (ellapsedTimeFromReceive >= 1.0f)
                {
                    OnScore(0);
                    DestorySelf();
                }
            }

            // レシーブ後の経過時間を更新する
            ellapsedTimeFromReceive += Time.deltaTime;
        }
    }

    private void FixedUpdate()
    {
        if (CurControlMode == ControlMode.Physics)
        {
            // 回転量を減衰させる
            curSpinSpeed *= 1.0f - (SpinAttenuationRate * Time.deltaTime);

            // 空中ではマグヌス効果による力を加える
            if (isInAir && isMagnusForceEnabled)
            {
                var moveVec = ballRigidbody.velocity;
                var spinAxis = hitSpinAxis;

                // 進行方向と回転軸に対して垂直方向に力を加える
                var forceVec = Vector3.Cross(spinAxis, moveVec) * curSpinSpeed;

                // 回転の進行方向に対する影響力
                var axisEffectRate = 1.0f - Mathf.Abs(Vector3.Dot(spinAxis, moveVec.normalized));

                float multiplier = axisEffectRate * MagnusForceEffectRate;

                // NOTE: 制御しやすいように質量を無視して力を加える
                ballRigidbody.AddForce(forceVec * multiplier, ForceMode.Acceleration);
            }
        }
        else if (CurControlMode == ControlMode.Receive)
        {
            // レシーブの時間を経過させる
            this.receiveElapsedTime += Time.deltaTime;

            float t = Mathf.Clamp01(this.receiveElapsedTime / this.receiveWholeTime);

            var curPos = Vector3.zero;
            // ベジェ曲線
            {
                var p1 = receivePos;
                var p2 = receiveControlPoint;
                var p3 = dropPos;
                curPos = Utility.Get2DBezierCurve(p1, p2, p3, t);
            }
            ballRigidbody.MovePosition(curPos);

            // レシーブ完了時
            if (t >= 1.0f)
            {
                // TMP: 仮実装
                CurControlMode = ControlMode.Physics;
            }
        }
        else if (CurControlMode == ControlMode.Toss)
        {
            // レシーブの時間を経過させる
            this.tossElapsedTime += Time.deltaTime;

            float t = Mathf.Clamp01(this.tossElapsedTime / this.tossWholeTime);

            var curPos = Vector3.zero;
            // ベジェ曲線
            {
                var p1 = tossStartPos;
                var p2 = tossControlPoint;
                var p3 = tossEndPos;
                curPos = Utility.Get2DBezierCurve(p1, p2, p3, t);
            }
            transform.position = curPos;
        }

        touchingScoreMultipliers?.Clear();
    }

    private void OnCollisionEnter(Collision collision)
    {
        isInAir = false;

        if (boundSoundPhysicsMaterial.Contains(collision.collider.sharedMaterial))
        {
            // 音のバリエーションとしてピッチを変える
            boundAudioSource.pitch = UnityEngine.Random.Range(0.95f, 1.05f);
            boundAudioSource.Play();
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        // 何にも触れていない場合は空中として判定する
        isInAir = true;
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.TryGetComponent<ScoreMultiplier>(out var scoreMultiplier))
        {
            touchingScoreMultipliers.Add(scoreMultiplier);
        }
    }

    public void Hold(Transform parentTransform)
    {
        CurControlMode = ControlMode.Constraint;

        transform.SetParent(parentTransform);
        transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
    }

    public void Toss(in Vector3 tossPosition, in Vector3 hitPosition, float timeToServe)
    {
        // TODO: の制御を実装する
        CurControlMode = ControlMode.Toss;

        // ワールド座標での制御にする
        transform.SetParent(null);

        // 情報を保持する
        tossStartPos = tossPosition;
        tossEndPos = hitPosition;
        tossWholeTime = timeToServe;
        tossElapsedTime = 0;

        // 制御点を求める
        {
            var midPos = (tossStartPos + tossEndPos) * 0.5f;

            // トスの時間で高さを決める
            float tossHeight = 3.0f * timeToServe;
            midPos.y += tossHeight;

            this.tossControlPoint = midPos;
        }
    }

    // TODO: 力を加えるなどの汎用的な名前と処理に変更する
    public void Serve(in ServeInfo serveInfo)
    {
        // 物理での制御にする
        CurControlMode = ControlMode.Physics;

        // TODO: ボールを管理するオブジェクトの配下にする
        transform.SetParent(null);

        // サーブ座標に移動
        ballRigidbody.position = serveInfo.ServePosition;
        //transform.position = serveInfo.ServePosition;

        // 打球時の情報を保持する
        hitVector = serveInfo.ServeVector;
        hitSpinAxis = serveInfo.ServeSpinAxis;
        hitSpinSpeed = serveInfo.ServeSpinSpeed;

        // 現在の情報として設定する
        curSpinSpeed = hitSpinSpeed;

        // 物理的に力を加える
        // NOTE: 制御しやすいように質量を無視して力を加える
        ballRigidbody.AddForce(hitVector, ForceMode.VelocityChange);
    }

    // TODO: ラケットがボールの情報を受け取う形にする
    // サーブ、レシーブの処理をラケットに持たせる
    // HACK: 仮実装やTODOが多いので対応する
    public void Receive(
        in Vector3 direction,
        in Racket racket,
        in TableLocatorGetter.ReceiveImpulse receiveImpulse,
        in TableLocatorGetter tableLocatorGetter)
    {
        Assert.AreApproximatelyEqual(direction.sqrMagnitude, 1, $"{nameof(direction)} が正規化されていません。");

        // 一度のみレシーブ処理を行う
        if (isReceived) { return; }
        isReceived = true;

        ellapsedTimeFromReceive = 0;

        // レシーブ後は物理挙動をシンプルにするため、マグヌス力を発生させないようにする
        isMagnusForceEnabled = false;

        // 剛体の速度を保持しておく
        var rigidbodyVelocity = ballRigidbody.velocity;

        CurControlMode = ControlMode.Receive;

        // 打球のベクトルを求める
        var resultVec = Vector3.zero;
        {
            // 回転の影響を計算する
            var axis = hitSpinAxis;
            var spinEffectVec = Vector3.Cross(axis, direction) * racket.SpinEffectRate;

            // 打球の力が加わるベクトル
            var hitVec = direction;

            // 最終的なベクトル
            resultVec = hitVec + spinEffectVec;
        }
        this.receivePos = ballRigidbody.position;
        this.receiveDirection = resultVec.normalized;

        // 極端に上下にずれている場合は入らないようにする
        {
            // ほぼ真上
            {
                var dir = this.receiveDirection;
                var dirLim = tableLocatorGetter.ReceiveTiltDirectionLimit;

                var dirZY = new Vector2(dir.z, dir.y).normalized;
                var dirLimZY = new Vector2(dirLim.z, dirLim.y).normalized;

                var isReachable = Vector3.Cross(dirLimZY, dirZY).z < 0.0f;

                // 相手コートに届くかどうか
                if (isReachable)
                {
                    Debug.Log("Reachable.");
                }
                else
                {
                    Debug.Log("Miss!(Unreached)");

                    receiveState = ReceiveState.MissUnreached;

                    // ラケットで跳ねさせてそのまま後ろに抜けさせる
                    CurControlMode = ControlMode.Physics;

                    // TODO: ボールの進行方向の影響も考慮するようにした方が自然に見えそう
                    var multiplier = 2.0f;
                    var dirXY = Vector3.Scale(this.receiveDirection, new Vector3(1, 1, 0)).normalized;
                    var velocity = dirXY * multiplier;
                    velocity.z = rigidbodyVelocity.z;
                    ballRigidbody.velocity = velocity;

                    return;
                }
            }

            // ネットを超えない球
            {
                var orig = this.receivePos;
                var dir = this.receiveDirection;

                var netPos = tableLocatorGetter.NetCenter;

                // ネットを超えられる高さ
                var netPassablePos = netPos + new Vector3(0, BallRadius, 0);

                // ZY平面で判定する
                var origZY = new Vector2(orig.z, orig.y);
                var targetZY = new Vector2(netPassablePos.z, netPassablePos.y);
                var dirToTargetZY = (targetZY - origZY).normalized;

                var dirZY = new Vector2(dir.z, dir.y).normalized;

                var isPassable = Vector3.Cross(dirToTargetZY, dirZY).z >= 0.0f;

                // 超えられるかどうか
                if (isPassable)
                {
                    Debug.Log("Passable.");
                }
                else
                {
                    Debug.Log("Miss!(Net)");

                    receiveState = ReceiveState.MissNet;

                    // TMP: 仮実装（ネットにあたるように物理的に力を加える）
                    var vecToNet = netPos - orig;
                    var force = vecToNet.magnitude;
                    var multiplier = 2.0f;

                    CurControlMode = ControlMode.Physics;
                    ballRigidbody.velocity = this.receiveDirection * force * multiplier;
                    return;
                }
            }
        }

        // 着弾点を求める
        {
            // 奥行方向の座標を取得する
            // TODO: 演出として少しずらす（JoyConの加速度のあたいをもとにする）
            var depth = tableLocatorGetter.GetReceiveDepth(receiveImpulse);

            // xの値を求める
            {
                // 交点を求める
                var pos = new Vector2(ballRigidbody.position.x, ballRigidbody.position.z);
                var vec = new Vector2(resultVec.x, resultVec.z);

                // 交差判定時のベクトルの長さ
                const float VectorLength = 10000;

                // 交差判定
                if (Utility.IsCrossing(
                    new Vector2(-VectorLength, depth),
                    new Vector2(VectorLength, depth),
                    pos,
                    pos + (vec * VectorLength),
                    out var intersection))
                {
                    // 落下地点を取得
                    var targetPos = new Vector3(intersection.x, tableLocatorGetter.TableHeight, intersection.y);

                    // 少し上に上げて、下方向にレイを飛ばし、卓球台があるかを確認する
                    var rayDistance = 10000.0f;
                    var ray = new Ray(targetPos + new Vector3(0, 1, 0), Vector3.down);

                    // 返球にかかる時間を設定
                    this.receiveWholeTime = 1.0f;
                    this.receiveElapsedTime = 0.0f;

                    // テーブル内に入っているかを確認する
                    if (Physics.SphereCast(ray, BallRadius, rayDistance, dropRaycastTargetLayerMask.value))
                    {
                        receiveState = ReceiveState.SuccessIn;

                        this.dropPos = targetPos;
                        Debug.Log($"In! DropPos{this.dropPos}");

                        // NOTE: ここでエッジの判定が可能
                    }
                    // 左右にオーバー
                    else
                    {
                        receiveState = ReceiveState.MissOut;

                        // 演出としての落下地点を設定する
                        this.dropPos = targetPos;
                        float limitX = tableLocatorGetter.ReceiveLimitX;

                        // 左右の上限値にクランプ
                        this.dropPos.x = Mathf.Clamp(this.dropPos.x, -limitX, limitX);

                        Debug.Log($"Miss!(Out) DropPos{this.dropPos}");
                    }

                    CurControlMode = ControlMode.Receive;

                    // 演出として回転を変える
                    // 回転を反転して遅くする
                    curSpinSpeed *= -1f * 0.5f;

                    // 制御点を求める
                    {
                        var p1 = this.receivePos;
                        var p3 = this.dropPos;
                        var midPoint = Vector3.Lerp(p1, p3, 0.5f);
                        var dir = receiveDirection;

                        var origZY = new Vector2(p1.z, p1.y);
                        var dirZY = new Vector2(dir.z, dir.y).normalized;
                        var origMidPointZY = new Vector2(midPoint.z, midPoint.y);

                        if (Utility.IsCrossing(
                            origZY,
                            origZY + dirZY * VectorLength,
                            origMidPointZY,
                            origMidPointZY + Vector2.up * VectorLength,
                            out var heightIntersection))
                        {
                            Debug.Log($"OK MidPoint:{heightIntersection}");
                            float heightOffset = 0.3f;
                            midPoint.y = heightIntersection.y + heightOffset;
                        }
                        else
                        {
                            Debug.LogError("制御点が求められませんでした。");
                        }

                        receiveControlPoint = midPoint;
                    }
                }
                // 前方に飛ばなかった場合
                else
                {
                    receiveState = ReceiveState.MissUnreached;

                    Debug.Log($"Miss!(Back)");

                    // TMP: ラケットで跳ねさせてそのまま後ろに抜けさせる
                    // HACK: 上限を超えたときの処理を同じなので、共通処理にできるか検討する（そもそも上下限の判定をしているので不要かもしれない）
                    CurControlMode = ControlMode.Physics;

                    var multiplier = 2.0f;
                    var dirXY = Vector3.Scale(this.receiveDirection, new Vector3(1, 1, 0)).normalized;
                    var velocity = dirXY * multiplier;
                    velocity.z = rigidbodyVelocity.z;
                    ballRigidbody.velocity = velocity;
                    return;
                }
            }
        }
    }

#if UNITY_EDITOR

    float s = 3.0f;

    private void OnDrawGizmos()
    {
        var style = new GUIStyle();
        style.fontSize = 20;
        style.normal.textColor = isInAir ? Color.white : Color.red;
        Handles.Label(transform.position, $"{isInAir}", style);

        if (isReceived && s > 0)
        {
            Handles.DrawLine(this.receivePos, this.receivePos + this.receiveDirection);
            s -= Time.deltaTime;

            Gizmos.DrawSphere(dropPos, 0.1f);
        }

    }

#endif
}
