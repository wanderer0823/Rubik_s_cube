using UnityEngine;

[DisallowMultipleComponent]
public class RoomBatchingCache : MonoBehaviour
{
    [System.Serializable]
    public struct Entry
    {
        public MeshFilter meshFilter;
        public Renderer renderer;
        public Mesh originalMesh;
    }

    public Entry[] entries;

    /// <summary>
    /// 在克隆体上调用：恢复原始Mesh并清掉烘焙光照引用，让物体可以自由旋转
    /// </summary>
    public void RestoreForClone(bool clearLightmap = true)
    {
        if (entries == null) return;

        for (int i = 0; i < entries.Length; i++)
        {
            var e = entries[i];

            if (e.meshFilter != null && e.originalMesh != null)
                e.meshFilter.sharedMesh = e.originalMesh;

            if (e.renderer != null && clearLightmap)
            {
                e.renderer.lightmapIndex = -1;
                e.renderer.lightmapScaleOffset = new Vector4(1, 1, 0, 0);
                e.renderer.realtimeLightmapIndex = -1;
                e.renderer.realtimeLightmapScaleOffset = new Vector4(1, 1, 0, 0);
            }
        }
    }
}
