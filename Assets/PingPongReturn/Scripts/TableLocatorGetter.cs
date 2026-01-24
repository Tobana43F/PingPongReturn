using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

public class TableLocatorGetter : MonoBehaviour
{
    /// <summary>
    /// レシーブの強さ
    /// </summary>
    // TMP: 定義する場所を変える
    public enum ReceiveImpulse
    {
        Low,
        Mid,
        High
    }

    [SerializeField] Transform tableLocator;
    [SerializeField] Transform netLocator;

    [SerializeField] Transform receiveLimitXLocator;

    // NOTE: 近い順にソート
    [SerializeField] List<Transform> receiveDepthLocators;

    [SerializeField] Transform receiveTiltDirectionLimitLocator;

    [SerializeField] Transform receiveFrontDirectionLocator;

    public Vector3 TableCenter
    {
        get
        {
            Assert.IsNotNull(tableLocator);
            return tableLocator.position;
        }
    }

    public Vector3 NetCenter
    {
        get
        {
            Assert.IsNotNull(netLocator);
            return netLocator.position;
        }
    }

    public float TableHeight
    {
        get
        {
            Assert.IsNotNull(tableLocator);
            return tableLocator.position.y;
        }
    }

    public float NetHeight
    {
        get
        {
            Assert.IsNotNull(netLocator);
            return netLocator.position.y;
        }
    }

    public Vector3 ReceiveTiltDirectionLimit
    {
        get
        {
            Assert.IsNotNull(receiveTiltDirectionLimitLocator);
            return receiveTiltDirectionLimitLocator.forward;
        }
    }

    public Vector3 ReceiveFrontDirection
    {
        get
        {
            Assert.IsNotNull(receiveFrontDirectionLocator);
            return receiveFrontDirectionLocator.forward;
        }
    }

    public float ReceiveLimitX
    {
        get
        {
            Assert.IsNotNull(receiveLimitXLocator);
            return receiveLimitXLocator.position.x;
        }
    }

    public float GetReceiveDepth(ReceiveImpulse impulse)
    {
        var index = Mathf.Clamp((int)impulse, 0, receiveDepthLocators.Count - 1);
        var transform = receiveDepthLocators.ElementAt(index);
        return transform.position.z;
    }
}
