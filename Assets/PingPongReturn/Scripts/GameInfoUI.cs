using TMPro;
using UnityEngine;
using UnityEngine.Assertions;

public class GameInfoUI : MonoBehaviour
{
    [SerializeField] TableTennisGameManager gameManager;

    [Space]
    [SerializeField] TextMeshProUGUI scoreText;
    [SerializeField] TextMeshProUGUI remainingServeCountText;

    // Start is called before the first frame update
    void Awake()
    {
        Assert.IsNotNull(gameManager);
        Assert.IsNotNull(scoreText);
        Assert.IsNotNull(remainingServeCountText);

        gameManager.onScoreChange += OnScoreChangeEvent;
        gameManager.onRemainingServeCountChange += OnRemainingServeCountChangeEvent;
    }

    void OnScoreChangeEvent(int currentScore, int addedScore)
    {
        scoreText.text = currentScore.ToString();
    }
    void OnRemainingServeCountChangeEvent(int count)
    {
        if (count == 0)
        {
            remainingServeCountText.text = "End";
        }
        else if (count == 1)
        {
            // ï™Ç©ÇËÇ‚Ç∑Ç¢ÇÊÇ§Ç…ç≈å„ÇÃàÍãÖÇÕï\é¶ÇïœÇ¶ÇÈ
            remainingServeCountText.text = "Last!";
        }
        else
        {
            remainingServeCountText.text = count.ToString();
        }
    }


}
