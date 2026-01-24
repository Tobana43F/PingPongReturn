using UnityEngine;
using UnityEngine.Assertions;

public class ServeInfo : MonoBehaviour
{
    // HACK: AnimatorのParameterとの関連性が分かるようにできると良い
    public enum ServeTypeIndex
    {
        Normal,
        Under,
        Prendulum,
        Hook
    }

    [SerializeField] Transform standingPosition;
    [SerializeField] Transform servePosition;
    [SerializeField] Transform serveDirection;
    [SerializeField] Transform serveSpinAxis;
    [SerializeField] Transform receivePoint;

    [Space]
    [SerializeField] ServeTypeIndex serveType;
    [SerializeField] float serveVelocity;
    [SerializeField] float serveSpinCountPerSecond;


    public ServeTypeIndex ServeType { get { return serveType; } }
    public Vector3 StandingPosition
    {
        get
        {
            Assert.IsNotNull(standingPosition);
            // 地面の位置を指定するのでYは0固定
            return Vector3.Scale(standingPosition.position, new Vector3(1, 0, 1));
        }
    }
    public Vector3 ServePosition
    {
        get
        {
            Assert.IsNotNull(servePosition);
            return servePosition.position;
        }
    }
    public Vector3 ServeVector
    {
        get
        {
            Assert.IsNotNull(serveDirection);
            Assert.IsTrue(serveDirection.forward.sqrMagnitude > 0, "回転軸が不定です");
            Assert.IsTrue(serveVelocity > 0, "サーブの速度は0以上に設定してください。");
            return serveDirection.forward * serveVelocity;
        }
    }
    public Vector3 ServeSpinAxis
    {
        get
        {
            Assert.IsNotNull(serveSpinAxis);
            Assert.IsTrue(serveSpinAxis.up.sqrMagnitude > 0, "回転軸が不定です");
            return serveSpinAxis.up;
        }
    }
    public Vector3 ReceivePosition
    {
        get
        {
            Assert.IsNotNull(receivePoint);
            return receivePoint.position;
        }
    }

    /// <summary>
    /// サーブの回転量（度数法）
    /// </summary>
    public float ServeSpinSpeed { get { return serveSpinCountPerSecond * 360; } }


#if UNITY_EDITOR
    
    private void OnValidate()
    {
        // 不正な値の抑制
        serveVelocity = Mathf.Max(0, serveVelocity);
    }

    private void OnDrawGizmos()
    {
        var pos = ServePosition;
        // 方向の表示
        {
            var vec = ServeVector;
            var lengthScale = 0.25f;

            var tmp = Gizmos.color;
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(pos, pos + (vec * lengthScale));
            Gizmos.color = tmp;
        }

        // 回転軸の表示
        {
            var axis = ServeSpinAxis;

            float length = 0.5f;
            float hLength = length * 0.5f;
            var s = pos - (axis * hLength);
            var e = pos + (axis * hLength);

            var tmp = Gizmos.color;
            Gizmos.color = Color.green;
            Gizmos.DrawLine(s, e);
            Gizmos.DrawSphere(e, 0.01f);
            Gizmos.color = tmp;
        }

        // 球の表示
        {
            Gizmos.DrawWireSphere(pos, 0.05f);
        }
    }

#endif
}
