using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ViewSwitchManager
{
    //按下F后依次切换视角，检查当前视角，并按顺序继续切换
    public void OnKeySwitch()
    {
        Debug.Log("F切换一次视角。");
    }

    //3个切换视角按钮
    public void OnView1ButtonClicked()
    {
        Debug.Log("切换到视角1。");
    }

    public void OnView2ButtonClicked()
    {
        Debug.Log("切换到视角2。");
    }

    public void OnView3ButtonClicked()
    {
        Debug.Log("切换到视角3。");
    }

}
