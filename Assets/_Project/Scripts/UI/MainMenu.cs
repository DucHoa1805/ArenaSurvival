using ArenaSurvival.Core;
using ArenaSurvival.Save;
using TMPro;
using UnityEngine;

namespace ArenaSurvival.UI
{
    public class MainMenu : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Button _playButton;
        [SerializeField] private UnityEngine.UI.Button _quitButton;
        [SerializeField] private TMP_Text _bestText;

        private void Awake()
        {
            _playButton.onClick.AddListener(HandlePlayClicked);
            _quitButton.onClick.AddListener(HandleQuitClicked);
        }

        private void Start()
        {
            SaveData best = SaveSystem.Load();
            _bestText.text = best.bestSurvivalTime > 0f
                ? $"Kỷ lục: {TimeFormatter.ToClock(best.bestSurvivalTime)}  ·  {best.bestKillCount} quái"
                : "Chưa có kỷ lục";
        }

        private void OnDestroy()
        {
            _playButton.onClick.RemoveListener(HandlePlayClicked);
            _quitButton.onClick.RemoveListener(HandleQuitClicked);
        }

        private void HandlePlayClicked()
        {
            // Nạp scene là bất đồng bộ: khóa nút để bấm 2 lần không nạp scene Game 2 lần.
            _playButton.interactable = false;
            SceneLoader.LoadGame();
        }

        private void HandleQuitClicked()
        {
            // Application.Quit() không có tác dụng trong Editor (và trên WebGL), nên trong Editor
            // thoát Play mode để nút vẫn thử được.
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
