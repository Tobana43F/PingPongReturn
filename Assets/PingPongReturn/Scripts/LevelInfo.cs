using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

public class LevelInfo : MonoBehaviour
{
    [System.Serializable]
    class LevelServeInfo
    {
        [SerializeField] public ServeInfo serveInfo;
        [SerializeField] public int frequency;
    }

    [SerializeField] int ballCount;
    [SerializeField] List<LevelServeInfo> levelServeInfos;

    public int BallCount { get => ballCount; }

    int FreqDenominator
    {
        get
        {
            return levelServeInfos.Select(e => e.frequency).Sum();
        }
    }

    private void Awake()
    {
        Assert.IsTrue(ballCount > 0);
    }

    public ServeInfo GetServeInfoByFrequency()
    {
        Assert.IsTrue(levelServeInfos.Count > 0);

        var denominator = FreqDenominator;
        var numerator = UnityEngine.Random.Range(0, denominator);

        foreach (var info in levelServeInfos)
        {
            if (numerator <= info.frequency)
            {
                return info.serveInfo;
            }

            numerator -= info.frequency;
        }

        Assert.IsTrue(false, $"{nameof(LevelServeInfo)}の抽選に失敗しました。（到達しないコード）");
        return levelServeInfos.Last().serveInfo;
    }
}
