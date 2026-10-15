using System;
using System.IO;
using UnityEngine;

namespace UltimoMiglio.UI.Data
{
    /// <summary>
    /// Read-only view of the few numbers the HUD needs to *display* (never to simulate).
    /// The Tech Lead's data layer should implement this; until it exists, <see cref="JsonHudConfig"/>
    /// reads data/ch1/*.json, and the fallback is "unknown" (the HUD hides the number instead of inventing one).
    /// </summary>
    public interface IHudConfig
    {
        /// <summary>Pedaggio multiplier on LN credits while in mora (e.g. 0.85). NaN if unknown.</summary>
        float TollMultiplier { get; }
        int DaysPerYear { get; }
        /// <summary>Installment amount in LN for installment k (1-based). NaN if unknown.</summary>
        double InstallmentAmount(int k);
        /// <summary>Due day of installment k (1-based). -1 if unknown.</summary>
        int InstallmentDueDay(int k);
        int StackBaseCapacity { get; }
    }

    [Serializable]
    public class Ch1HudJson
    {
        // Field names are a proposal for the Game Designer; JsonUtility ignores missing ones.
        public float tollMultiplier = float.NaN;
        public int daysPerYear = 365;
        public int installmentsPerYear = 0;      // rata k due at round(daysPerYear*k/installmentsPerYear)
        public double[] installmentAmounts = new double[0];
        public int[] installmentDueDays = new int[0];
        public int stackBaseCapacity = 0;
    }

    public class JsonHudConfig : IHudConfig
    {
        readonly Ch1HudJson d;
        public JsonHudConfig(Ch1HudJson data) { d = data ?? new Ch1HudJson(); }

        /// <summary>Loads from a TextAsset (Resources/Addressables) or a file path (StreamingAssets / dev box).</summary>
        public static JsonHudConfig FromText(string json)
        {
            var d = new Ch1HudJson();
            if (!string.IsNullOrEmpty(json)) JsonUtility.FromJsonOverwrite(json, d);
            return new JsonHudConfig(d);
        }
        public static JsonHudConfig FromFile(string path) =>
            FromText(File.Exists(path) ? File.ReadAllText(path) : null);

        public float TollMultiplier => d.tollMultiplier;
        public int DaysPerYear => d.daysPerYear > 0 ? d.daysPerYear : 365;
        public int StackBaseCapacity => d.stackBaseCapacity;

        public double InstallmentAmount(int k) =>
            k >= 1 && k <= d.installmentAmounts.Length ? d.installmentAmounts[k - 1] : double.NaN;

        public int InstallmentDueDay(int k)
        {
            if (k >= 1 && k <= d.installmentDueDays.Length) return d.installmentDueDays[k - 1];
            if (d.installmentsPerYear > 0) return Mathf.RoundToInt((float)DaysPerYear * k / d.installmentsPerYear);
            return -1;
        }
    }

    /// <summary>Inspector-friendly holder so scenes can point at a JSON TextAsset.</summary>
    [CreateAssetMenu(menuName = "Ultimo Miglio/UI/Hud Config Source", fileName = "HudConfigSource")]
    public class HudConfigSource : ScriptableObject
    {
        public TextAsset ch1Json;
        IHudConfig cached;
        public IHudConfig Config => cached ??= JsonHudConfig.FromText(ch1Json ? ch1Json.text : null);
        public void Override(IHudConfig cfg) => cached = cfg;
    }
}
