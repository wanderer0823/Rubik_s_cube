using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static InitCubeSlot;

public class CameraManager
{
    public static float cameraDist;

    private Camera View1Camera;
    private Camera View2Camera;
    private Camera View3Camera;

    private ViewMode currentMode;
    private int currentView1Index = 0;
    private FaceDir playerFace;

    public void SwitchCamera(ViewMode mode)
    {
        DisableAllCameras();
        currentMode = mode;

        switch (currentMode)
        {
            case ViewMode.View1:
                TransView1Camera();
                View1Camera.gameObject.SetActive(true);
                break;

            case ViewMode.View2:
                View2Camera.gameObject.SetActive(true);
                break;

            case ViewMode.View3:
                View3Camera.gameObject.SetActive(true);
                break;
        }

    }

    private void DisableAllCameras()
    {
        View1Camera.gameObject.SetActive(false);
        View2Camera.gameObject.SetActive(false);
        View3Camera.gameObject.SetActive(false);
    }

    private void TransView1Camera()
    {/*
        //更改朝向
        return SearchPlayerFace() switch
        {
            FaceDir.Up => 0,
            FaceDir.Down => 1,
            FaceDir.Left => 2,
            FaceDir.Right => 3,
            FaceDir.Front => 4,
            FaceDir.Back => 5,
            _ => 0
        };
        //更改位置
        View1Camera.transform.position = cubeCenter.position + playerFaceDir*cameraDist;
    */
    }

    private FaceDir SearchPlayerFace()
    {
        return playerFace switch
        {

            _=>0
        };
    }

}
