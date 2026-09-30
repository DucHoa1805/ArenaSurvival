namespace ArenaSurvival.Core
{
    public enum GameState
    {
        Menu,
        Playing,
        Paused,
        LevelUp, // game dừng để chọn nâng cấp — tách khỏi Paused để menu Pause không xen vào được
        GameOver
    }
}