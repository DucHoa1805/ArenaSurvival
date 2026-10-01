namespace ArenaSurvival.Meta
{
    /// <summary>Thứ người chơi THỰC SỰ nhận được sau 1 màn (đã áp hệ số lần đầu / chơi lại) — màn kết quả hiển thị cái này.</summary>
    public class StageGrant
    {
        public string stageId;
        public int gold;
        public int accountXp;
        public bool firstClear;
        public float multiplier;
        public int starsBefore;
        public int starsAfter;
    }
}
