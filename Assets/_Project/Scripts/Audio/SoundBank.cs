using UnityEngine;

namespace ArenaSurvival.Audio
{
    /// <summary>
    /// Toàn bộ âm thanh của game gom vào 1 asset. Muốn đổi tiếng hoặc chỉnh âm lượng: sửa asset này
    /// trong Inspector, không cần sửa code.
    /// </summary>
    [CreateAssetMenu(menuName = "ArenaSurvival/Sound Bank", fileName = "SoundBank")]
    public class SoundBank : ScriptableObject
    {
        [Header("Music")]
        public AudioClip music;
        [Range(0f, 1f)] public float musicVolume = 0.35f;

        [Header("Combat")]
        public SoundEffect shoot;
        public SoundEffect projectileHit;
        public SoundEffect enemyDeath;
        public SoundEffect playerHurt;

        [Header("Progression")]
        public SoundEffect xpPickup;
        public SoundEffect levelUp;
        [Tooltip("Lúc bấm chọn 1 nâng cấp trong bảng lên cấp.")]
        public SoundEffect upgradeSelect;

        [Header("Game Flow & UI")]
        public SoundEffect gameOver;
        [Tooltip("Thắng màn.")]
        public SoundEffect stageVictory;
        [Tooltip("Lúc chọn hồi sinh.")]
        public SoundEffect revive;
        public SoundEffect uiClick;
        [Tooltip("Lúc rê chuột vào nút hoặc thẻ nâng cấp.")]
        public SoundEffect uiHover;
    }
}
