using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ScoreMultiplierManager : MonoBehaviour
{
    List<GameObject> scoreMultiplierGroups = new List<GameObject>();

    void Awake()
    {
        var count = transform.childCount;

        for (int i = 0; i < count; ++i)
        {
            var go = transform.GetChild(i).gameObject;
            scoreMultiplierGroups.Add(go);

            // ‚·‚×‚Ä–³Œø‰»‚·‚é
            go.SetActive(false);
        }
    }

    private void Start()
    {
        EnableAtRandom();
    }

    // ‘S‚Ä–³Œø‰»‚·‚é
    public void DisableAll()
    {
        scoreMultiplierGroups?.ForEach(go => go.SetActive(false));
    }

    // ƒ‰ƒ“ƒ_ƒ€‚Å1‚Â—LŒø‰»‚·‚é
    public void EnableAtRandom()
    {
        DisableAll();

        var index = UnityEngine.Random.Range(0, scoreMultiplierGroups.Count);
        scoreMultiplierGroups.ElementAt(index).SetActive(true);
    }
}
