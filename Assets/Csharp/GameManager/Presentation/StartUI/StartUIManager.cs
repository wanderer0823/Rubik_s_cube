using System;
using UnityEngine;

namespace Csharp.GameManager.Presentation.StartUI
{
    public class StartUIManager : MonoBehaviour
    {
        public GameObject StartUI;
        public GameObject CreatorUI;

        private void Awake()
        {
            if (StartUI == null) return;
            StartUI.SetActive(true);
            CreatorUI.SetActive(false);
        }

        public void StartGame()
        {
            StartUI.SetActive(false);
            MusicAudioManager.Instance.PlaySfx("afterclass");
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