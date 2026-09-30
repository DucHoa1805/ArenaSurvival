using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArenaSurvival.Core
{
    /// <summary>
    /// Một chỗ duy nhất biết tên scene và cách chuyển scene, để UI không rải chuỗi tên scene khắp nơi.
    /// Scene phải có trong Build Settings thì mới nạp được bằng tên.
    /// </summary>
    public static class SceneLoader
    {
        public const string MainMenuScene = "MainMenu";
        public const string GameScene = "Game";

        public static AsyncOperation LoadMainMenu() => Load(MainMenuScene);

        /// <summary>Dùng cho cả Play lẫn Retry: nạp lại scene Game là cách reset ván chơi sạch nhất.</summary>
        public static AsyncOperation LoadGame() => Load(GameScene);

        private static AsyncOperation Load(string sceneName)
        {
            // Time.timeScale là giá trị toàn cục, không tự reset khi đổi scene. Rời scene lúc đang
            // Pause/Game Over (timeScale = 0) mà quên dòng này thì scene mới sẽ đứng hình.
            Time.timeScale = 1f;
            return SceneManager.LoadSceneAsync(sceneName);
        }
    }
}
