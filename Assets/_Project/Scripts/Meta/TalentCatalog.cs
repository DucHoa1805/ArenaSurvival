using UnityEngine;

namespace ArenaSurvival.Meta
{
    /// <summary>Danh sách Talent theo thứ tự hiển thị.</summary>
    [CreateAssetMenu(menuName = "ArenaSurvival/Talent Catalog", fileName = "TalentCatalog")]
    public class TalentCatalog : ScriptableObject
    {
        public TalentData[] talents = new TalentData[0];
    }
}
