using UnityEngine;

public class TableTennisGameManager : MonoBehaviour
{
    public delegate void OnScoreChangeEvent(int currentScore, int addedScore);
    public event OnScoreChangeEvent onScoreChange;

    public delegate void OnRemainingServeCountChange(int count);
    public event OnRemainingServeCountChange onRemainingServeCountChange;

    [SerializeField] LevelInfo levelInfo;

    int score;
    int remainingServeCount;

    int prevScore;
    int prevRemainingServeCount;

    public int RemainingServeCount { get => remainingServeCount; }

    private void Start()
    {
        InitializeGame();
    }

    private void Update()
    {
        ObserveScoreChange();
        ObserveRemainingServeCountChange();
    }


    void InitializeGame()
    {
        ResetScore();
        ResetRemainingServeCount();
    }

    void ResetScore()
    {
        score = 0;
        prevScore = 0;
        onScoreChange?.Invoke(0, 0);
    }
    void ResetRemainingServeCount()
    {
        var count = levelInfo.BallCount;
        remainingServeCount = count;
        prevRemainingServeCount = count;
        onRemainingServeCountChange?.Invoke(remainingServeCount);
    }

    public void AddScore(int score)
    {
        this.score += score;
    }

    public void DecreaseServeCount()
    {
        remainingServeCount--;
    }

    void ObserveScoreChange()
    {
        if (score != prevScore)
        {
            var addedScore = score - prevScore;
            onScoreChange?.Invoke(score, addedScore);
        }
        // ëOèÓïÒÇçXêV
        prevScore = score;
    }

    void ObserveRemainingServeCountChange()
    {
        if (remainingServeCount != prevRemainingServeCount)
        {
            onRemainingServeCountChange?.Invoke(remainingServeCount);
        }
        prevRemainingServeCount = remainingServeCount;
    }
}
