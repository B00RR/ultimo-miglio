using UnityEngine;
using UltimoMiglio.Core.Data;

namespace UltimoMiglio.Gameplay
{
    /// <summary>ScriptableObject wrapper of data/ch1 JSON. Created/refreshed by the editor importer
    /// (menu Ultimo Miglio/Data/Import Ch1 Data, also automatic on JSON reimport) at Assets/_Game/Data/Resources/Ch1Data.asset.</summary>
    [CreateAssetMenu(menuName = "UltimoMiglio/Chapter Data")]
    public sealed class ChapterDataAsset : ScriptableObject
    {
        public ChapterData Data = new ChapterData();
        public EconomyData economy => Data.economy;
    }
}
