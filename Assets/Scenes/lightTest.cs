using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public class lightTest : MonoBehaviour
{
    private void Start()
    {
        LightmapData[] data = new LightmapData[1];
        data[0] = new LightmapData();
        LightmapSettings.lightmaps = data;
    }
}
