using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Csharp.GameManager.Presentation.StartUI
{
    public class StartUIManager : MonoBehaviour
    {
        public GameObject StartUI;
        public GameObject CreatorUI;
        //Yiu
        public Button StartButton;
        private GameState gs;
        private bool haveBackToHere=false;
        private TextMeshProUGUI buttonText;

        private void Awake()
        {
            if (StartUI != null)
                StartUI.SetActive(true);

            if (CreatorUI != null)
                CreatorUI.SetActive(false);
            //Yiu
            gs = GameState.Instance;
        }

        private void Start()
        {
            buttonText = StartButton.GetComponent<TextMeshProUGUI>();
        }

        public void OnEnable()
        {
            GameEvents.OnBackStartUI += BackToStartUI;
        }

        public void OnDisable()
        {
            GameEvents.OnBackStartUI -= BackToStartUI;
        }

        public void StartGame()
        {
            StartUI.SetActive(false);
            MusicAudioManager.Instance.PlaySfx("afterclass");
            //Yiu
            if (GameState.Instance == null)
                new GameState();
            GameState.Instance.SetPlayerState(PlayerState.isMoving);
        }

        public void ExitGame()
        {
            Application.Quit();
        }

        public void OpenCreator()
        {
            CreatorUI.SetActive(true);
        }
        
        public void CloseCreator()
        {
            CreatorUI.SetActive(false);
        }

        //Yiu
        private void BackToStartUI()
        {
            haveBackToHere = true;
            if(StartButton!=null) StartUI.SetActive(true);
            RenameStartButton();
        }
        private void RenameStartButton()
        {
            if(buttonText == null&&StartButton!=null)
            {
                buttonText = StartButton.GetComponentInChildren<TextMeshProUGUI>();
            }
            if (buttonText != null)
                buttonText.text = "继续游戏";
            else /*__DEBUGTOOL_START__*/Debug.Log("SUM：找不到开始游戏按钮的文字组件");/*__DEBUGTOOL_END__*/
        }
    }
}