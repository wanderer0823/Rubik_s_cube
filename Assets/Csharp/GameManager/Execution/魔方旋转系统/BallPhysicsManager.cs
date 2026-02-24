using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallPhysicsManager 
{
    [SerializeField] private GameObject View2Ball;
    private Rigidbody rb;

    public void UnlockBallPhysics()
    {
        Debug.Log("锁定小球物理。");
        GetBallRigidBody();
        rb.isKinematic = false;
    }

    public void LockBallPhysics()
    {
        Debug.Log("解锁小球物理。");
        GetBallRigidBody();
        rb.isKinematic = true;
    }

    //获取球所在正方形
    /*public 
    public Vector3 GetBallRelativePos()
    {

    }*/

    private void GetBallRigidBody()
    {
        rb=View2Ball.GetComponent<Rigidbody>();
    }
}
