using GraphicAssets;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

public partial class BallServer : MonoBehaviour
{
    // TODO: 共通で使う場合は別の場所に定義する
    public enum ServeSeq
    {
        Ready,
        Toss,
        Hit,
        Served
    }

    [SerializeField] TableTennisGameManager gameManager;

    [SerializeField] BallController ballPrefab;
    [SerializeField] LevelInfo levelInfo;

    [SerializeField] Racket racket;

    [Space]
    [SerializeField] Animator humanAnimator;
    [SerializeField] HumanAnimationEventHandler humanAnimationHandler;

    [Space]
    [SerializeField] Transform ballHoldingLocator;
    [SerializeField] Transform impactPointLocator;

    [SerializeField] ScoreMultiplierManager scoreMultiplierManager;

    [Space]
    [SerializeField] AudioSource tossAudioSource;

    ServeInfo curServeInfo;
    BallController curBallController;

    // TODO: ServeInfoの情報から取得する
    [SerializeField] AudioSource serveHitAudioSource;
    [SerializeField] AudioClip serveSE;

    // TODO: 削除
    [SerializeField] PlayerController playerController;

    private void Awake()
    {
        humanAnimationHandler.onTossed += OnTossed;
        humanAnimationHandler.onHit += OnHit;
    }

    void Start()
    {
        InitializeState();
    }

    void Update()
    {
        UpdateState();
    }

    //----------------------------------------------------------
    // Event
    //----------------------------------------------------------

    void OnTossed()
    {
        // ボールが上がり始める位置と打球位置を取得する
        var tossPosition = Vector3.zero;
        var hitPosition = Vector3.zero;
        {
            tossPosition = ballHoldingLocator.position;
            hitPosition = impactPointLocator.position;
        }

        // 打球するまでの時間を取得する
        float timeToServe = 0;
        {
            var clipInfos = humanAnimator.GetCurrentAnimatorClipInfo(0);
            var clipInfo = clipInfos.ElementAt(0);
            var events = clipInfo.clip.events;

            var tossedEvent = events.First(e => e.functionName == HumanAnimationEventHandler.OnTossedFunctionName);
            var hitEvent = events.First(e => e.functionName == HumanAnimationEventHandler.OnHitFunctionName);
            Assert.IsNotNull(tossedEvent);
            Assert.IsNotNull(hitEvent);

            timeToServe = hitEvent.time - tossedEvent.time;
            Assert.IsTrue(timeToServe > 0);
        }

        // SE再生
        tossAudioSource.Play();

        this.curBallController.Toss(tossPosition, hitPosition, timeToServe);

    }
    void OnHit()
    {
        Serve();
    }

    //----------------------------------------------------------
    // Func
    //----------------------------------------------------------

    void Serve()
    {
        Assert.IsNotNull(curBallController);
        curBallController.Serve(curServeInfo);

        // プレイヤーに情報を渡す
        playerController.RegisterBall(curBallController);
        playerController.RegisterReceivePosition(curServeInfo.ReceivePosition);

        playerController.OnServe();

        serveHitAudioSource.PlayOneShot(serveSE);
    }
}

// TODO: 整理　わかりやすいようにpartial で作ってる
public partial class BallServer
{
    const string AnmParamServeType = "ServeType";
    const string AnmParamServeBegin = "ServeBegin";
    const string AnmParamMoveBegin = "MoveBegin";

    enum State
    {
        Idle,
        Move,
        Serving,
        Served,
    }

    State prevState = State.Idle;
    State curState = State.Idle;
    bool isEnter = true;

    // TMP: 削除
    float t = 30.0f;

    void UpdateState()
    {
        // ステートの更新
        var stateUpdater = new Dictionary<State, Action>()
        {
            { State.Idle, UpdateIdle },
            { State.Move, UpdateMove },
            { State.Serving, UpdateServing },
            { State.Served, UpdateServed },
        };
        stateUpdater[curState]();

        // Enter状態を初期化
        isEnter = false;

        // ステートが変更されたかどうかを判断する
        if (curState != prevState)
        {
            isEnter = true;
        }
        // 前情報を更新
        prevState = curState;

        t -= Time.deltaTime;
    }


    void InitializeState()
    {
        curState = State.Idle;
    }

    void UpdateIdle()
    {
        if (isEnter)
        {
            t = 1.0f;

            // サーブ情報を取得する
            curServeInfo = levelInfo.GetServeInfoByFrequency();
        }

        // TMP: しばらく待機
        // サーブがすべて終わったらこのステートで待機させる
        if (gameManager.RemainingServeCount > 0 &&
            t <= 0)
        {
            curState = State.Move;
        }
    }
    void UpdateMove()
    {
        const float MoveTime = 2.0f;
        if (isEnter)
        {
            t = MoveTime;

            // 移動開始
            humanAnimator.SetTrigger(AnmParamMoveBegin);

            // ボール生成
            this.curBallController = Instantiate(ballPrefab);

            this.curBallController.onScore += (score) =>
            {
                gameManager.AddScore(score);
                scoreMultiplierManager.EnableAtRandom();
            };

            // ボールを手に乗せる
            this.curBallController.Hold(this.ballHoldingLocator);
        }

        // TODO: 所定の位置まで移動(ServeInfoの位置まで移動する)
        {
            var pos = transform.position;
            var target = this.curServeInfo.StandingPosition;

            var result = Vector3.Slerp(pos, target, 1.0f - (t / MoveTime));
            transform.position = result;
        }

        // TMP: 一定時間待つだけ
        if (t <= 0) 
        {
            // 念のため座標を設定する
            transform.position = this.curServeInfo.StandingPosition;

            // サーブを始める
            curState = State.Serving;

        }
    }
    void UpdateServing()
    {
        if (isEnter) 
        {
            var serveType = curServeInfo.ServeType;
            humanAnimator.SetInteger(AnmParamServeType, (int)serveType);
            humanAnimator.SetTrigger(AnmParamServeBegin);
        }

        // TODO: サーブアニメが終了したら次へ遷移する
        // TMP: Idleアニメに戻ったら終了
        var anmStateInfo = humanAnimator.GetCurrentAnimatorStateInfo(0);
        if(anmStateInfo.IsName("Idle"))
        {
            curState = State.Served;
        }
    }
    void UpdateServed()
    {
        if (isEnter) 
        {
            t = 0.5f;
            gameManager.DecreaseServeCount();
        }

        // TMP: 一定時間でIdleに戻す
        if (t <= 0) 
        {
            curState = State.Idle;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Handles.Label(transform.position, $"state:{curState}, t:{t}");
    }
#endif
}