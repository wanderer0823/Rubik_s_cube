using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static InitCubeSlot;

public class View1CameraManager : MonoBehaviour
{
    public Transform cubeCenter;
    public float View1CameraDist=10.0f;

    private void OnEnable()
    {
        //¶©ÔÄÇÐ»»ÊÓ½Ç1
        GameEvents.IsView1Now += TransCamera;
    }

    private void OnDisable()
    {
        //È¡Ïû¶©ÔÄÇÐ»»ÊÓ½Ç1
        GameEvents.IsView1Now -= TransCamera;
    }

    private void TransCamera()
    {
        Vector3 playerDir = FaceOffset[GameState.Instance.CurrentPlayerFace];
        Vector3 cameraDir = (-1)*playerDir;

        transform.position = cubeCenter.position + View1CameraDist*playerDir;
        transform.LookAt(cameraDir);
    }
}
