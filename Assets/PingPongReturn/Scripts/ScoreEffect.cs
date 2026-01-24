using UnityEngine;
using UnityEngine.Assertions;

public class ScoreEffect : MonoBehaviour
{
    [SerializeField] float lifeTime;

    private void Start()
    {
        Assert.IsTrue(lifeTime > 0);
        Destroy(gameObject, lifeTime);
    }
}
