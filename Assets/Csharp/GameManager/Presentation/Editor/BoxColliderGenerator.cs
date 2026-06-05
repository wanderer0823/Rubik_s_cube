using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class BoxColliderGenerator : EditorWindow
{
    private float meshSeparationThreshold = 5f;
    private bool includeChildren = true;
    private bool overwriteExisting = false;

    [MenuItem("Tools/碰撞体生成/为选中物体生成BoxCollider")]
    public static void ShowWindow()
    {
        GetWindow<BoxColliderGenerator>("BoxCollider 生成器");
    }

    private void OnGUI()
    {
        GUILayout.Label("碰撞体生成设置", EditorStyles.boldLabel);

        meshSeparationThreshold = EditorGUILayout.FloatField("合并网格阈值", meshSeparationThreshold);
        includeChildren = EditorGUILayout.Toggle("同时为子物体生成", includeChildren);
        overwriteExisting = EditorGUILayout.Toggle("为选中物体重新生成", overwriteExisting);

        if (GUILayout.Button("生成BoxCollider", GUILayout.Height(30)))
        {
            GenerateForSelected();
        }
    }

    private void GenerateForSelected()
    {
        GameObject[] selected = Selection.gameObjects;
        if (selected.Length == 0)
        {
            EditorUtility.DisplayDialog("提示", "请先选择一个或多个GameObject", "确定");
            return;
        }

        int totalColliders = 0;
        foreach (GameObject obj in selected)
        {
            totalColliders += GenerateBoxCollidersRecursive(obj);
        }

        EditorUtility.DisplayDialog("成功", $"已生成 {totalColliders} 个BoxCollider", "确定");
    }

    private int GenerateBoxCollidersRecursive(GameObject target)
    {
        int count = 0;
        count += GenerateBoxColliderForSingleObject(target);

        if (includeChildren)
        {
            foreach (Transform child in target.transform)
            {
                count += GenerateBoxCollidersRecursive(child.gameObject);
            }
        }

        return count;
    }

    private int GenerateBoxColliderForSingleObject(GameObject target)
    {
        MeshFilter meshFilter = target.GetComponent<MeshFilter>();
        SkinnedMeshRenderer skinnedMesh = target.GetComponent<SkinnedMeshRenderer>();

        Mesh mesh = null;
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            mesh = meshFilter.sharedMesh;
        }
        else if (skinnedMesh != null && skinnedMesh.sharedMesh != null)
        {
            mesh = skinnedMesh.sharedMesh;
        }

        if (mesh == null)
        {
            return 0;
        }

        if (overwriteExisting)
        {
            BoxCollider[] existingColliders = target.GetComponents<BoxCollider>();
            foreach (BoxCollider collider in existingColliders)
            {
                DestroyImmediate(collider);
            }
        }

        // 使用 KD-Tree 分析网格分离
        List<List<Vector3>> separatedMeshGroups = AnalyzeMeshSeparationWithKDTree(mesh);

        int colliderCount = 0;
        foreach (var meshGroup in separatedMeshGroups)
        {
            if (meshGroup.Count == 0) continue;

            BoxCollider collider = target.AddComponent<BoxCollider>();
            Bounds bounds = CalculateBoundsFromVertices(meshGroup);

            collider.center = bounds.center;
            collider.size = bounds.size;
            collider.isTrigger = false;

            colliderCount++;
        }

        return colliderCount;
    }

    private List<List<Vector3>> AnalyzeMeshSeparationWithKDTree(Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;
        if (vertices.Length == 0)
        {
            return new List<List<Vector3>>();
        }

        // 构建 KD-Tree 索引
        int[] indices = new int[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            indices[i] = i;
        }

        KDTree kdtree = new KDTree(vertices, indices);

        // 使用并查集优化连通性检测
        UnionFind uf = new UnionFind(vertices.Length);

        // 为每个顶点查找邻近顶点，建立连通关系
        float sqrThreshold = meshSeparationThreshold * meshSeparationThreshold;

        for (int i = 0; i < vertices.Length; i++)
        {
            // 使用 KD-Tree 快速查询邻近顶点
            List<int> neighbors = kdtree.FindNearby(vertices[i], meshSeparationThreshold);

            foreach (int neighbor in neighbors)
            {
                if (i != neighbor && Vector3.SqrMagnitude(vertices[i] - vertices[neighbor]) <= sqrThreshold)
                {
                    uf.Union(i, neighbor);
                }
            }
        }

        // 按连通分量分组
        Dictionary<int, List<Vector3>> groups = new Dictionary<int, List<Vector3>>();
        for (int i = 0; i < vertices.Length; i++)
        {
            int root = uf.Find(i);
            if (!groups.ContainsKey(root))
            {
                groups[root] = new List<Vector3>();
            }
            groups[root].Add(vertices[i]);
        }

        return groups.Values.ToList();
    }

    private Bounds CalculateBoundsFromVertices(List<Vector3> vertices)
    {
        if (vertices.Count == 0)
        {
            return new Bounds(Vector3.zero, Vector3.one);
        }

        Bounds bounds = new Bounds(vertices[0], Vector3.zero);
        for (int i = 1; i < vertices.Count; i++)
        {
            bounds.Encapsulate(vertices[i]);
        }

        return bounds;
    }

    // KD-Tree 实现
    private class KDTree
    {
        private KDNode root;
        private Vector3[] vertices;
        private int[] indices;

        public KDTree(Vector3[] verts, int[] inds)
        {
            this.vertices = verts;
            this.indices = inds;
            this.root = BuildTree(inds, 0, inds.Length - 1, 0);
        }

        private KDNode BuildTree(int[] indices, int start, int end, int depth)
        {
            if (start > end) return null;

            int axis = depth % 3;
            int mid = (start + end) / 2;

            // 快速选择算法找中位数
            QuickSelect(indices, start, end, mid, axis);

            KDNode node = new KDNode(indices[mid], vertices[indices[mid]]);
            node.left = BuildTree(indices, start, mid - 1, depth + 1);
            node.right = BuildTree(indices, mid + 1, end, depth + 1);

            return node;
        }

        private void QuickSelect(int[] indices, int start, int end, int k, int axis)
        {
            if (start >= end) return;

            int pi = Partition(indices, start, end, axis);

            if (pi == k)
                return;
            else if (pi < k)
                QuickSelect(indices, pi + 1, end, k, axis);
            else
                QuickSelect(indices, start, pi - 1, k, axis);
        }

        private int Partition(int[] indices, int start, int end, int axis)
        {
            int pivot = indices[end];
            int pi = start;

            for (int i = start; i < end; i++)
            {
                if (GetAxisValue(vertices[indices[i]], axis) < GetAxisValue(vertices[pivot], axis))
                {
                    int temp = indices[i];
                    indices[i] = indices[pi];
                    indices[pi] = temp;
                    pi++;
                }
            }

            int temp2 = indices[end];
            indices[end] = indices[pi];
            indices[pi] = temp2;
            return pi;
        }

        public List<int> FindNearby(Vector3 position, float range)
        {
            List<int> result = new List<int>();
            FindNearbyRecursive(root, position, range, 0, result);
            return result;
        }

        private void FindNearbyRecursive(KDNode node, Vector3 position, float range, int depth, List<int> result)
        {
            if (node == null) return;

            float dist = Vector3.Distance(node.position, position);
            if (dist <= range)
            {
                result.Add(node.index);
            }

            int axis = depth % 3;
            float diff = GetAxisValue(position, axis) - GetAxisValue(node.position, axis);

            KDNode first = diff < 0 ? node.left : node.right;
            KDNode second = diff < 0 ? node.right : node.left;

            FindNearbyRecursive(first, position, range, depth + 1, result);

            if (diff * diff <= range * range)
            {
                FindNearbyRecursive(second, position, range, depth + 1, result);
            }
        }

        private float GetAxisValue(Vector3 v, int axis)
        {
            return axis == 0 ? v.x : (axis == 1 ? v.y : v.z);
        }

        private class KDNode
        {
            public int index;
            public Vector3 position;
            public KDNode left, right;

            public KDNode(int idx, Vector3 pos)
            {
                this.index = idx;
                this.position = pos;
            }
        }
    }

    // 并查集优化连通性检测
    private class UnionFind
    {
        private int[] parent;
        private int[] rank;

        public UnionFind(int n)
        {
            parent = new int[n];
            rank = new int[n];
            for (int i = 0; i < n; i++)
            {
                parent[i] = i;
                rank[i] = 0;
            }
        }

        public int Find(int x)
        {
            if (parent[x] != x)
            {
                parent[x] = Find(parent[x]); // 路径压缩
            }
            return parent[x];
        }

        public void Union(int x, int y)
        {
            int px = Find(x);
            int py = Find(y);

            if (px == py) return;

            // 按秩合并
            if (rank[px] < rank[py])
            {
                parent[px] = py;
            }
            else if (rank[px] > rank[py])
            {
                parent[py] = px;
            }
            else
            {
                parent[py] = px;
                rank[px]++;
            }
        }
    }
}
