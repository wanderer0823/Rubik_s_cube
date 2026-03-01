using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

//一个Forward可视化工具
public class DrawForward : MonoBehaviour
{
    // 可调整的参数
    [SerializeField] private float lineLength = 2f;  // 线的长度
    [SerializeField] private Color lineColor = Color.blue;  // 线的颜色

    void OnDrawGizmos()
    {
        // 设置颜色
        Gizmos.color = lineColor;

        // 从物体位置向前方画一条线
        Gizmos.DrawRay(transform.position, transform.forward * lineLength);

        // 在线的末端画一个小球，更容易看到
        Gizmos.DrawSphere(transform.position + transform.forward * lineLength, 0.1f);

    }
}
