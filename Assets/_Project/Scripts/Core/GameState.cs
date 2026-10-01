namespace ArenaSurvival.Core
{
    public enum GameState
    {
        Menu,
        Playing,
        Paused,
        LevelUp, // game dừng để chọn nâng cấp — tách khỏi Paused để menu Pause không xen vào được
        RevivePrompt, // Player vừa chết, game dừng chờ chọn hồi sinh hay bỏ cuộc
        Victory,      // thắng màn
        GameOver      // thua màn
    }
}