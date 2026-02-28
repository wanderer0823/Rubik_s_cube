using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeTurnController : MonoBehaviour
{
    private void Awake()
    {

    }

    private void OnEnable()
    {
        GameEvents.OnArrowsExecute += ClickArrows;
    }
    private void OnDisable()
    {
        GameEvents.OnArrowsExecute -= ClickArrows;
    }

    void ClickArrows(int num)
    {
        Debug.Log("µã»÷¼ýÍ·°´Å¥£º" + num);
    }
}

