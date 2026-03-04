using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorVectorReturn : MonoBehaviour
{
    // 此脚本用于获取门对应的房间矢量（魔方坐标系下的）
    //  每个门都挂载此脚本

    //需要把获得门实例对应到魔方坐标系矢量，
    //已知旋转后的房间对应的是魔方中的面在世界坐标系下的旋转，
    //但本人认为因为重点是重力方向，所以可忽略面在世界坐标系下绕Y轴的旋转（因为不影响重力）
    //我在房间的局部坐标系下设置了6个代表矢量的空物体，来挂门，计算门在世界坐标系下对应房间的矢量方向
    //然后要把获得的门的世界矢量转换成魔方坐标系下的矢量，故依据重力在世界坐标系和魔方坐标系的不同数值来对应计算

    public Vector3 DoorinRoomVector;
    void Update()
    {
        ReturnDoorVector();
    }

    void ReturnDoorVector()
    {
        //这里我设置的上一级父物体的六个方向是按世界坐标系对应的矢量，此时门对应的矢量是房间旋转后
        Vector3 dir = transform.parent.localPosition;

        Vector3 parentPos = transform.root.rotation*dir;

        //把获得的门的世界矢量转换成魔方坐标系下的矢量，故依据重力在世界坐标系和魔方坐标系的不同数值来对应计算。
        //ps:如果用世界坐标系下的矢量来计算，直接注释下面两行就行。
        Quaternion rotation = Quaternion.FromToRotation(new Vector3(0,1,0), CubeRotateController.CurrentGDirinMF);
        parentPos=rotation*parentPos.normalized;

        //舍弃微小值
        float epsilon = 0.1f;

        if (Mathf.Abs(parentPos.x) < epsilon) parentPos.x = 0;
        if (Mathf.Abs(parentPos.y) < epsilon) parentPos.y = 0;
        if (Mathf.Abs(parentPos.z) < epsilon) parentPos.z = 0;

        

        DoorinRoomVector = parentPos;
    }
}
