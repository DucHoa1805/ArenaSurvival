namespace ArenaSurvival.Stages
{
    /// <summary>
    /// Màn người chơi vừa chọn, giữ qua lúc chuyển scene (biến static sống suốt phiên chơi).
    /// Tạm thời cho tới khi có Hub (Phase 9) và hồ sơ người chơi (Phase 7). Trống thì StageSession dùng màn đầu tiên,
    /// nên bấm Play thẳng ở scene Game trong Editor vẫn chơi được.
    /// </summary>
    public static class StageSelection
    {
        public static StageData Current { get; private set; }

        // Editor có thể tắt domain reload: không để màn của lần Play trước rò sang lần sau.
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Current = null;

        public static void Select(StageData stage) => Current = stage;

        public static void Clear() => Current = null;
    }
}
