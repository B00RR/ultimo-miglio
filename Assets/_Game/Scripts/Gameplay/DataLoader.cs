using UnityEngine;
using UltimoMiglio.Core;
using UltimoMiglio.Core.Data;

namespace UltimoMiglio.Gameplay
{
    public static class DataLoader
    {
        /// <summary>Loads the imported ScriptableObject; no hardcoded fallback numbers (fail loudly).</summary>
        public static EconomyConfig LoadChapter(int chapter = 1)
        {
            var a = Resources.Load<ChapterDataAsset>($"Ch{chapter}Data");
            if (!a) throw new System.InvalidOperationException($"Ch{chapter}Data.asset missing: run 'Ultimo Miglio/Data/Import Ch1 Data'");
            return EconomyConfig.From(a.Data);
        }

        /// <summary>#101: passi dell'onboarding dall'asset importato (null se assente).</summary>
        public static OnboardingData LoadOnboarding(int chapter = 1) => Resources.Load<ChapterDataAsset>($"Ch{chapter}Data")?.Data?.onboarding;

        /// <summary>Parses the 7 JSON texts into ChapterData (used by the editor importer and tests).</summary>
        public static ChapterData Parse(int chapter, System.Func<string, string> readJson)
        {
            return new ChapterData
            {
                chapter = chapter,
                economy = JsonUtility.FromJson<EconomyData>(readJson("economy")),
                debt = JsonUtility.FromJson<DebtData>(readJson("debt")),
                upgrades = JsonUtility.FromJson<UpgradesData>(readJson("upgrades")),
                robots = JsonUtility.FromJson<RobotsData>(readJson("robots")),
                zones = JsonUtility.FromJson<ZonesData>(readJson("zones")),
                offline = JsonUtility.FromJson<OfflineData>(readJson("offline")),
                daycycle = JsonUtility.FromJson<DayCycleData>(readJson("daycycle")),
                onboarding = JsonUtility.FromJson<OnboardingData>(readJson("onboarding")),
            };
        }
    }
}
