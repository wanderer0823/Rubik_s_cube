using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static InitCubeSlot;

public class ViewModeManager : MonoBehaviour
{
    public static ViewModeManager Instance;
    private GameState gs;
    public GameObject player;
    [Header("灏忕悆")]
    public GameObject ball_p;
    public Transform ball;
    public float minBallSpeed = 0.02f;
    private Rigidbody rb;
    [Header("绌洪棿绯荤粺寮曠敤")]
    public Transform cubeRoot;
    public InitCubeSlot cubeData;
    [Header("鎴块棿鏃嬭浆鐢ㄧ殑鏃堕棿")]
    [SerializeField] private float RotationTime=5f;


    //娆ф坊鍔狅細鍦ㄦ洿鏂版棆杞悗閲嶇疆灏忕悆浣嶇疆

    private Quaternion newRotation = Quaternion.Euler(360, 360, 360);
    private Quaternion lastRoomRotation = Quaternion.Euler(360, 360, 360);

    private void ResetPlayerToStartPosition()
    {
        if (player == null)
            return;

        PlayerAction playerAction = player.GetComponent<PlayerAction>();
        if (playerAction == null)
            return;

        playerAction.ResetToStartPosition();
    }

    void Awake()
    {
        Instance = this;
        if (GameState.Instance == null)
            new GameState();

        gs = GameState.Instance;
        //rb = ball_p.GetComponent<Rigidbody>();
    }
    public void OnEnable()
    {
        //璁㈤槄GM璇锋眰浜嬩欢
        GameEvents.OnTabRequest += CheckTab;
        GameEvents.OnMoveRequest += CheckMove;
        GameEvents.OnViewSwitchRequest += CheckViewSwitch;
        //GameEvents.OnOpenDoorRequest += CheckOpenDoor;
        GameEvents.OnRotateRequest += CheckRotate;
        GameEvents.OnRotateFinishRequest += CheckRotateFinish;
        GameEvents.OnMouseLookRequest += CheckMouseMove; //娆?
        //璁㈤槄UIM璇锋眰浜嬩欢
        GameEvents.OnDirectViewSwitchRequest += CheckDirectViewSwitch;
        GameEvents.OnArrowsClickRequest += CheckArrowsClick;  //寮犲ぉ濮?
        //璁㈤槄CRC璇锋眰浜嬩欢
        //GameEvents.OnBallSpaceUpdateRequest += CheckBallSpaceUpdate;
        //鏂板
        GameEvents.OnInteractRequest += CheckInteract;
        GameEvents.OnScrollRequest += CheckScroll;
        GameEvents.OnMatChangeRequest += CheckMatChange;
        GameEvents.OnViewSwitchExecute += OnViewSwitch;
    }

    public void OnDisable()
    {
        //鍙栨秷璁㈤槄
        GameEvents.OnTabRequest -= CheckTab;
        GameEvents.OnMoveRequest -= CheckMove;
        GameEvents.OnViewSwitchRequest -= CheckViewSwitch;
        //GameEvents.OnOpenDoorRequest -= CheckOpenDoor;
        GameEvents.OnRotateRequest -= CheckRotate;
        GameEvents.OnRotateFinishRequest -= CheckRotateFinish;
        GameEvents.OnMouseLookRequest -= CheckMouseMove; //娆?
        //UIM璇锋眰浜嬩欢
        GameEvents.OnDirectViewSwitchRequest -= CheckDirectViewSwitch;
        GameEvents.OnArrowsClickRequest -= CheckArrowsClick;  //寮犲ぉ濮?
        //璁㈤槄CRC璇锋眰浜嬩欢
        //GameEvents.OnBallSpaceUpdateRequest -= CheckBallSpaceUpdate;
        //鏂板
        GameEvents.OnInteractRequest -= CheckInteract;
        GameEvents.OnScrollRequest -= CheckScroll;
        GameEvents.OnMatChangeRequest -= CheckMatChange;
        GameEvents.OnViewSwitchExecute -= OnViewSwitch;
    }

    /// <summary> 閭诲眳棰勫姞杞芥帴鍙ｏ細鍦?View3 鍒囨崲鎴栧紑闂ㄨ浆鍦烘椂璋冪敤 RoomPreloadController.ExecutePreload() </summary>
    public void RequestNeighborPreload()
    {
        var rpc = GameManager.Instance?.roomPreloadSystem;
        if (rpc != null) rpc.ExecutePreload();
    }

    #region 鐢℅S妫€鏌ュ綋鍓嶅叏灞€鐘舵€?
    bool CheckViewMode(ViewMode mode)
    {
        if (gs.CurrentView == mode)
            return true;
        else return false;
    }

    bool CheckPlayerState(PlayerState playerState)
    {
        if (gs.CurrentPlayerState ==playerState)
            return true;
        else return false;
    }
    #endregion

    #region ============================================
    #region 鐩戝惉璁㈤槄鍑芥暟
    void CheckTab()
    {
        if (gs.IsBagOpen)
        {
            // 鑳屽寘宸插紑 鈫?鍏抽棴
            gs.CloseBag();
            GameEvents.onBagCloseExecute();
        }
        else
        {
            // 鑳屽寘鏈紑 鈫?妫€鏌ユ槸鍚﹀厑璁告墦寮€
            if (!CheckPlayerState(PlayerState.isMoving)
                && !CheckPlayerState(PlayerState.turningFinished)
                && !CheckPlayerState(PlayerState.rotatingFinished))
                return;

            gs.OpenBag();
            GameEvents.onBagOpenExecute();
        }
    }

    void CheckMove(Vector3 moveDir)
    {
        if (!CheckViewMode(ViewMode.View3))
            return;
        // 绉诲姩鏃惰嚜鍔ㄥ叧鑳屽寘
        if (CheckPlayerState(PlayerState.isOpeningBag) && moveDir != Vector3.zero)
        {
            gs.CloseBag();
            GameEvents.onBagCloseExecute();
        }

        if (!CheckPlayerState(PlayerState.isMoving)&&!CheckPlayerState(PlayerState.isGrabbing))
            return;

        GameEvents.onMoveExecute(moveDir);
    }

    void CheckViewSwitch()//F
    {
        // 鑳屽寘鎵撳紑鏃跺厛鍏宠儗鍖呭啀鍒囪瑙?
        if (CheckPlayerState(PlayerState.isOpeningBag))
        {
            gs.CloseBag();
            GameEvents.onBagCloseExecute();
        }

        if (!CheckPlayerState(PlayerState.rotatingFinished)
            && !CheckPlayerState(PlayerState.turningFinished)
            && !CheckPlayerState(PlayerState.isMoving))
            return;

        if (isRotating)//鍦ㄦ棆杞殑鏃跺€欎笉鑳藉垏瑙嗚
            return;

        gs.FSetView();
    }

    void CheckDirectViewSwitch(ViewMode targetMode)
    {
        // 鑳屽寘鎵撳紑鏃跺厛鍏宠儗鍖呭啀鍒囪瑙?
        if (CheckPlayerState(PlayerState.isOpeningBag))
        {
            gs.CloseBag();
            GameEvents.onBagCloseExecute();
        }
        if (!CheckPlayerState(PlayerState.rotatingFinished)
            && !CheckPlayerState(PlayerState.turningFinished)
            && !CheckPlayerState(PlayerState.isMoving))
            return;
        //鏇存柊view mode
        gs.SetView(targetMode);
    }
    #region 灏佽鏃嬭浆CurrentRoom鏂规硶
    private void OnViewSwitch(ViewMode mode)
    {
        if (mode != ViewMode.View3)
            return;
        RotateCurrentRoom();
    }
    private bool isRotating = false;  // 闃叉鏃嬭浆杩囩▼涓啀娆¤Е鍙?

private void RotateCurrentRoom()
{
    if (isRotating) return;  // 姝ｅ湪鏃嬭浆锛屽拷鐣ユ湰娆¤皟鐢?

    GameObject currentRoom = cubeData.CurrentRoom;

        // 1. 璁＄畻鐩爣鏃嬭浆锛堜笌鍘熸潵鐩稿悓锛?
    Quaternion rotation_R = Quaternion.FromToRotation(
    CubeRotateController.CurrentGDirinMF,
    new Vector3(0, -1, 0));
    Quaternion qStart = Quaternion.Euler(270, 0, 0);
    GameObject pieceObj = cubeData.GetPieceGameObjectByRoomID(gs.CurrentRoomID);
    Quaternion qEnd = pieceObj.transform.localRotation;
    if (pieceObj.transform.parent == cubeRoot)
    {
        qEnd = pieceObj.transform.localRotation;
    }
    Quaternion rotation_T = qEnd * Quaternion.Inverse(qStart);
    Quaternion targetRotation = rotation_R * rotation_T;

    // 2. 鍚姩骞虫粦鏃嬭浆鍗忕▼
    StartCoroutine(RotateOverTime(currentRoom.transform, targetRotation, RotationTime)); // 0.5绉掑畬鎴愭棆杞?
}

private IEnumerator RotateOverTime(Transform targetTransform, Quaternion targetRotation, float duration)
{
    isRotating = true;
    Quaternion startRotation = targetTransform.rotation;
    float elapsed = 0f;

    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        float t = elapsed / duration;  // 0鈫?
        // 浣跨敤Slerp淇濊瘉鏃嬭浆璺緞鏈€鐭?
        targetTransform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
        yield return null;
    }

    // 纭繚鏈€缁堢簿纭埌杈剧洰鏍囨棆杞?
    targetTransform.rotation = targetRotation;
    isRotating = false;

    // 鏃嬭浆瀹屾垚鍚庣殑澶勭悊锛堝師鏉ュ啓鍦≧otateCurrentRoom鏈熬鐨勯€昏緫锛?
    newRotation = targetTransform.rotation;
    if (Quaternion.Angle(lastRoomRotation, newRotation) > 0f)
    {
        //ResetPlayerToStartPosition();
        lastRoomRotation = newRotation;
    }
}
    #endregion

    void CheckRotate(RotateType type)//left right
    {
        if (!CheckViewMode(ViewMode.View2)
            || !CheckPlayerState(PlayerState.rotatingFinished))
            return;
        gs.SetPlayerState(PlayerState.isRotating);
        if(type==RotateType.Left)
        {
            GameEvents.onCubeRotateStart();
        }
        if (type == RotateType.Right)
        {
            GameEvents.onCameraRotateStart();
        }
    }

    void CheckRotateFinish(RotateType type)
    {
        if (!CheckViewMode(ViewMode.View2)
            || !CheckPlayerState(PlayerState.isRotating))
            return;
        gs.SetPlayerState(PlayerState.rotatingFinished);
        if (type == RotateType.Left)
        {
            GameEvents.onCubeRotateEnd();
        }
        if (type == RotateType.Right)
        {
            GameEvents.onCameraRotateEnd();
        }
    }

    //娆э細璁㈤槄GM鐨勯紶鏍囩Щ鍔ㄨ姹備簨浠?
    void CheckMouseMove(Vector2 mouseMove)
    {
        if (!CheckViewMode(ViewMode.View3))
            return;
        if(!CheckPlayerState(PlayerState.isMoving)
            &&!CheckPlayerState(PlayerState.isGrabbing))
            return;
        GameEvents.onMouseLookExecute(mouseMove);
    }

    //寮犲ぉ濮匡細璁㈤槄UIM鐨勭澶磋姹備簨浠?
    void CheckArrowsClick(int number)
    {
        if (!CheckPlayerState(PlayerState.turningFinished)
            || !CheckViewMode(ViewMode.View2))
            return;
        //鏇存柊view mode
        gs.SetPlayerState(PlayerState.isTurning);
        GameEvents.onArrowsExecute(number);
    }
    // ===== yiu鏂板锛欵閿氦浜?=====
    void CheckInteract()
    {
        // 浠?View3 + isMoving 鏃跺厑璁窫浜や簰
        if (!CheckViewMode(ViewMode.View3)
            || !CheckPlayerState(PlayerState.isMoving))
            return;
        GameEvents.onInteractExecute();
    }

    // ===== 鏂板锛氭粴杞垎娴?=====
    void CheckScroll(float delta)
    {
        if (CheckPlayerState(PlayerState.isOpeningBag))
        {
            // 鑳屽寘鎵撳紑鏃?鈫?鑳屽寘婊氬姩
            GameEvents.onBagScrollExecute(delta);
        }
        else if (CheckPlayerState(PlayerState.isGrabbing)
                 && CheckViewMode(ViewMode.View3))
        {
            // 涓捐捣鐗╀綋鏃?鈫?鏃嬭浆鐗╀綋
            GameEvents.onGrabRotateExecute(delta);
        }
        // 鍏朵粬鐘舵€佷笅婊氳疆鏃犳晥
    }

    // ===== 鏂板锛氳儗鍖呭唴鏉愯川鍒囨崲 =====
    void CheckMatChange(PlayerMatState targetMat)
    {
        if (!CheckPlayerState(PlayerState.isOpeningBag))
            return;
        gs.SetMatState(targetMat);
        GameEvents.onMatChangeExecute(targetMat);
    }

    #endregion
    #endregion

    #region ============================================
    #region 鎺у埗灏忕悆鐗╃悊鐘舵€侊紙鍒犻櫎锛?
    //璁㈤槄CRC璇锋眰浜嬩欢
    /*void CheckBallSpaceUpdate(Vector3 ballPos)
    {
            //璁＄畻骞舵洿鏂板皬鐞冪┖闂翠綅缃殑鍏ㄥ眬鐘舵€?
            var surface =
                BallLocationService.CalculateSurface(
                    cubeRoot,
                    cubeData,
                    ballPos
                );
            if (surface == null)
                return;
            gs.SetCurrentSurface(surface);
            Vector3 localDown =
                cubeRoot.InverseTransformDirection(Vector3.down);

            //鏀瑰彉鏂伴噸鍔涙柟鍚戝湪鐩稿鍧愭爣绯讳腑鐨勭煝閲?
            FaceDir gravityFace =
                BallLocationService.CalculateGravityFace(localDown);
            gs.SetGravityFace(gravityFace);

    }*/
    #endregion
    #endregion
}
