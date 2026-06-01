using System;
using UnityEngine;

namespace Csharp.GameManager.Presentation.StartUI
{
    public class StartUIManager : MonoBehaviour
    {
        public GameObject StartUI;
        public GameObject CreatorUI;
        //Yiu
        private GameState gs;

        private void Awake()
        {
            if (StartUI != null)
                StartUI.SetActive(true);

            if (CreatorUI != null)
                CreatorUI.SetActive(false);
            //Yiu
            gs = GameState.Instance;
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
    }
}