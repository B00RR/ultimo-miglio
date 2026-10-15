using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UltimoMiglio.Core.Data;

/// <summary>Every JSON in data/ch1 must map 1:1 onto the DTOs: each leaf in the file round-trips through JsonUtility
/// with the same value (so nothing silently falls back to a default), and no DTO string/array/object is null or empty.</summary>
public class DataJsonTests
{
    static readonly Type[] Types = { typeof(EconomyData), typeof(DebtData), typeof(UpgradesData), typeof(RobotsData), typeof(ZonesData), typeof(OfflineData), typeof(DayCycleData) };

    [TestCaseSource(nameof(Indices))]
    public void JsonRoundTripsWithoutLoss(int i)
    {
        string raw = File.ReadAllText(Path.Combine(TestData.Dir, TestData.Files[i] + ".json"));
        object dto = JsonUtility.FromJson(raw, Types[i]);
        var back = JObject.Parse(JsonUtility.ToJson(dto));
        foreach (var leaf in JObject.Parse(raw).Descendants().OfType<JValue>())
        {
            string path = leaf.Path; if (path == "source" || path == "chapter") continue;
            var got = back.SelectToken(path);
            Assert.IsNotNull(got, $"{TestData.Files[i]}.json: key '{path}' not mapped in {Types[i].Name}");
            if (leaf.Type == JTokenType.Integer || leaf.Type == JTokenType.Float) Assert.AreEqual(leaf.Value<double>(), got.Value<double>(), 1e-6, path);
            else Assert.AreEqual(leaf.ToString(), got.ToString(), path);
        }
    }

    [TestCaseSource(nameof(Indices))]
    public void NoNullOrEmptyMembers(int i)
    {
        string raw = File.ReadAllText(Path.Combine(TestData.Dir, TestData.Files[i] + ".json"));
        Walk(JsonUtility.FromJson(raw, Types[i]), Types[i].Name, JObject.Parse(raw));
    }

    static void Walk(object o, string path, JToken json)
    {
        foreach (var f in o.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            var jv = json?[f.Name]; if (jv == null) continue; // field not in this JSON entry (e.g. costRef pads)
            var v = f.GetValue(o); string p = path + "." + f.Name;
            Assert.IsNotNull(v, p + " is null");
            if (v is string s) Assert.IsNotEmpty(s, p);
            else if (v is IList list) { Assert.Greater(list.Count, 0, p + " empty"); for (int k = 0; k < list.Count; k++) Walk(list[k], $"{p}[{k}]", jv[k]); }
            else if (!f.FieldType.IsPrimitive && f.FieldType != typeof(decimal)) Walk(v, p, jv);
            else if (jv.Type == JTokenType.Float || jv.Type == JTokenType.Integer)
                Assert.IsFalse(Convert.ToDouble(v) == 0 && jv.Value<double>() != 0, p + " defaulted to 0");
        }
    }

    static IEnumerable Indices() => Enumerable.Range(0, Types.Length);

    [Test] public void Ch1ValuesDriveConfig()
    {
        var c = UltimoMiglio.Core.EconomyConfig.From(TestData.Ch1());
        Assert.AreEqual(8400, c.TrancheLn); Assert.AreEqual(5, c.ZainoCapacity[0]); Assert.AreEqual(50, c.NonnoHireCost);
        Assert.AreEqual(336, c.CassaCapacity, 1e-6); Assert.AreEqual(10, c.ParcelValueLn, 1e-9);
    }
}
