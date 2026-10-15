using System.IO;
using UltimoMiglio.Core;
using UltimoMiglio.Core.Data;
using UltimoMiglio.Gameplay;

static class TestData
{
    public const string Dir = "Assets/_Game/Data/ch1";
    public static readonly string[] Files = { "economy", "debt", "upgrades", "robots", "zones", "offline", "daycycle" };
    public static ChapterData Ch1() => DataLoader.Parse(1, n => File.ReadAllText(Path.Combine(Dir, n + ".json")));
    public static EconomyCore NewCore() => new EconomyCore(EconomyConfig.From(Ch1()));
}
