using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraAction : MonoBehaviour
{
    public Transform Player;
    [SerializeField] private float minAngle = -80f;  // 向下看
    [SerializeField] private float maxAngle = 80f;   // 向上看

    private void OnEnable()
    {
        
    }


    private void OnDisable()
    {
        
    }

    void Execute()
    {

    }
}

