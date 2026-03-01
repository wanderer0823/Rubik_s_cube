using System;
using System.Collections.Generic;  
using UnityEngine;  
using InitCubeSlotAxis = InitCubeSlot.Axis;  
using InitCubeSlotFaceDir = InitCubeSlot.FaceDir;  
using ArrowSide = ArrowsButton.ArrowSide;  
using UnityEngine.UI;

public class CubeTurnController : MonoBehaviour
{
    //箭头按键参数  
    [Tooltip("箭头预制体")]  
    [SerializeField] private GameObject arrowButtonPrefab;  
    [SerializeField] private List<RectTransform> ButtonsRectTranform = new List<RectTransform>();  
    //旋转参数  
    [SerializeField] private InitCubeSlot initCubeSlot;  
    [SerializeField] private InitCubeSlotFaceDir currentFaceDir = InitCubeSlotFaceDir.Up;  
    public List<InitCubeSlot.CubePiece> currentCubePiece = new List<InitCubeSlot.CubePiece>();  
    public InitCubeSlotAxis currentAxis;  
    // 逻辑位置要乘2  
    private int coordScale = 2;  
    // index 0/1/2 映射到 coord 值 -scale, 0, scaleprivate
    int IndexToCoordValue(int index)
    {
        if (index < 3) return (index - 1) * coordScale;
        else return (index - 4) * coordScale;
    }
    
    private void Awake()
    {
        if (initCubeSlot == null)  
            initCubeSlot = FindObjectOfType<InitCubeSlot>();  
        // currentFaceDir = BallLocationService.GetBallFaceDirByPos()
    }

    void Start()
    {
        InitArrowsButton();
    }
    
    private void OnEnable()
    {
        GameEvents.OnArrowsExecute += RotateByCurrentArrow;
    }
    private void OnDisable()
    {
        GameEvents.OnArrowsExecute -= RotateByCurrentArrow;
    }

    public void SetCurrentFace(InitCubeSlotFaceDir face) => currentFaceDir = face;  
  
    public void InitArrowsButton()  
        {            
            if (!arrowButtonPrefab.TryGetComponent<ArrowsButton>(out var button))  
            {                
                Debug.LogError("预制体缺少ArrowsButton脚本");  
                return;  
            }            
            int index = 0;  
            foreach (RectTransform rect in ButtonsRectTranform)  
            {                
                GameObject go = Instantiate(arrowButtonPrefab, rect);  
                ArrowsButton  arrowButton = go.GetComponent<ArrowsButton>();
                Button b = go.GetComponent<Button>();
                UIManager.Instance.AddArrowButton(b);
                if (index < 3) arrowButton.SetArrowSide(ArrowSide.Up);
                else  arrowButton.SetArrowSide(ArrowSide.Left);
                arrowButton.SetArrowIndex(index++);  
            }  
            UIManager.Instance.BindArrowsButtons();
        }        
    #region 获取层级立方体并旋转  
        // 根据当前朝向和箭头配置，筛选出对应一层的立方体。  
        public void GetPiecesForArrow(  
            int index )  
        {            
            var face = currentFaceDir;  
            var i = index;
            ArrowSide s;
            if (index < 3)
            {
                s = ArrowSide.Up;
            }
            else
            {
                index -= 3;
                s =  ArrowSide.Left;
            }
            
            GetPiecesForArrowInternal(face, s, i);  
        }  
        private void GetPiecesForArrowInternal(  
            InitCubeSlotFaceDir faceDir,  
            ArrowSide side,  
            int index)  
        {            
            (InitCubeSlotAxis axis, int coordValue) = GetLayerFilter(faceDir, side, index);  
            currentCubePiece.Clear();  
            currentAxis = axis;  
            currentCubePiece = initCubeSlot.GetPiecesInLayer(axis, coordValue);  
        }        
        // 根据朝向面和箭头位置，得到筛选条件：(轴, 坐标值)。  
        // 例如 face=Up, side=Up, index=0 → (X, -2) 表示左边竖列。  
        public (InitCubeSlotAxis axis, int coordValue) GetLayerFilter(  
            InitCubeSlotFaceDir faceDir,  
            ArrowSide side,  
            int index)  
        {            
            int val = IndexToCoordValue(Mathf.Clamp(index, 0, 2));  
  
            switch (faceDir)  
            {                
                case InitCubeSlotFaceDir.Up:   // 看 Y+ 面，平面为 XZ
                    return side is ArrowSide.Up or ArrowSide.Left  
                        ? (InitCubeSlotAxis.X, val)   // 上下边箭头 → 选列（X）  
                        : (InitCubeSlotAxis.Z, val);  // 左右边箭头 → 选行（Z）  
  
                case InitCubeSlotFaceDir.Down: // 看 Y- 面，平面为 XZ
                    return side is ArrowSide.Up or ArrowSide.Left  
                        ? (InitCubeSlotAxis.X, val)  
                        : (InitCubeSlotAxis.Z, -val);  
  
                case InitCubeSlotFaceDir.Left:  // 看 X- 面，平面为 YZ
                    return side is ArrowSide.Up or ArrowSide.Left  
                        ? (InitCubeSlotAxis.Z, val)  
                        : (InitCubeSlotAxis.Y, val);  
  
                case InitCubeSlotFaceDir.Right: // 看 X+ 面，平面为 YZ
                    return side is ArrowSide.Up or ArrowSide.Left  
                        ? (InitCubeSlotAxis.Z, -val)  
                        : (InitCubeSlotAxis.Y, val);  
  
                case InitCubeSlotFaceDir.Front: // 看 Z+ 面，平面为 XY
                    return side is ArrowSide.Up or ArrowSide.Left  
                        ? (InitCubeSlotAxis.X, val)  
                        : (InitCubeSlotAxis.Y, val);  
  
                case InitCubeSlotFaceDir.Back:  // 看 Z- 面，平面为 XY
                    return side is ArrowSide.Up or ArrowSide.Left  
                        ? (InitCubeSlotAxis.X, -val)  
                        : (InitCubeSlotAxis.Y, val);  
            }  
            return (InitCubeSlotAxis.X, val);  
        }        
        public void RotateByCurrentArrow(int arrowIndex)  
        {            
            GetPiecesForArrow(arrowIndex);
            
            // 将 Axis 映射为世界坐标中的单位方向（等价于按轴取 FaceOffset）  
            Vector3Int axisOffset = AxisToOffset(currentAxis);  
  
            foreach (InitCubeSlot.CubePiece piece in currentCubePiece)  
            {                // TOOD : 添加dotween动画  
                Quaternion rotation = Quaternion.AngleAxis(90, axisOffset);  
                piece.indexCube.rotation *= rotation;  
  
                Vector3Int coord = piece.coord;  
                switch (currentAxis)  
                {                    case InitCubeSlot.Axis.X:  
                        piece.coord = new Vector3Int(coord.x, -coord.z, coord.y);  
                        break;  
                    case InitCubeSlot.Axis.Y:  
                        piece.coord = new Vector3Int(coord.z, coord.y, -coord.x);  
                        break;  
                    case InitCubeSlot.Axis.Z:  
                        piece.coord = new Vector3Int(-coord.y, coord.x, coord.z);  
                        break;  
                }            }                    }  
  
        // Axis -> 对应的单位方向向量（类似 FaceOffset.Up/Down/Left... 的轴向量）  
        private static Vector3Int AxisToOffset(InitCubeSlotAxis axis)  
        {            return axis switch  
            {  
                InitCubeSlotAxis.X => new Vector3Int(1, 0, 0),  
                InitCubeSlotAxis.Y => new Vector3Int(0, 1, 0),  
                InitCubeSlotAxis.Z => new Vector3Int(0, 0, 1),  
                _ => Vector3Int.zero  
            };  
        }        
    #endregion  
    
}

