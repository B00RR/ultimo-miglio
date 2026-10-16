using System.Linq;
using NUnit.Framework;
using UltimoMiglio.Core;
using UltimoMiglio.Gameplay;

/// <summary>#91/#101: formato delle righe [ONB] (docs/design/13_CAP1_EVENTI_ONBOARDING.md §1) e 16 passi di onboarding.json.</summary>
public class OnboardingLogTests
{
    [Test] public void SpecExamples_MatchRegex()
    {
        Assert.AreEqual("[ONB] start id=hireNonno n=9 t=130.0 reveals=1", OnbLog.Line("start", "hireNonno", 9, 130, "reveals", 1));
        Assert.AreEqual("[ONB] gate id=hireNonno n=9 t=131.2 event=nonnoHired dur=1.2", OnbLog.Line("gate", "hireNonno", 9, 131.2, "event", "nonnoHired", "dur", 1.2));
        Assert.AreEqual("[ONB] hint id=move n=1 t=36.0 lvl=2 idle=20.0 kind=manoFantasma", OnbLog.Line("hint", "move", 1, 36, "lvl", 2, "idle", 20.0, "kind", "manoFantasma"));
        foreach (var l in new[] {
            OnbLog.Line("softgate", "combo", 7, 140, "idle", 40.0),
            OnbLog.Line("nolnp", "hireNonno", 9, 129.5, "pad", "assumiNonno", "wallet", 32, "cost", 50),
            OnbLog.Line("skip", "nonnoIntro", 8, 120.4, "from", "nonnoIntro"),
            OnbLog.Line("end", "freePlay", 15, 301, "total", 301.0, "hints", 2, "lvl3", 0) })
            Assert.IsTrue(OnbLog.IsValid(l), l);
    }
    [Test] public void DecimalPointAndSpaces_AreSafe()
    {
        var prev = System.Threading.Thread.CurrentThread.CurrentCulture;
        try
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("it-IT");
            var l = OnbLog.Line("hint", "pickUp", 2, 1234.56, "lvl", 1, "idle", 10.04, "kind", "camera breve");
            Assert.AreEqual("[ONB] hint id=pickUp n=2 t=1234.6 lvl=1 idle=10.0 kind=camera_breve", l);
            Assert.IsTrue(OnbLog.IsValid(l));
        }
        finally { System.Threading.Thread.CurrentThread.CurrentCulture = prev; }
        Assert.IsFalse(OnbLog.IsValid("[ONB] unlock id=9b t=131.2"));
        Assert.IsFalse(OnbLog.IsValid("[ONB] gate id=move n=1 t=3,5"));
    }
    [Test] public void OnboardingJson_Has16Steps_InTutorialOrder()
    {
        var d = TestData.Ch1().onboarding;
        Assert.IsNotNull(d); Assert.AreEqual(16, d.steps.Length);
        CollectionAssert.AreEqual(TutorialManager.StepIds, d.steps.Select(s => s.id).ToArray());
        Assert.Less(System.Array.IndexOf(TutorialManager.StepIds, "zainoPad"), System.Array.IndexOf(TutorialManager.StepIds, "hireNonno"), "Zaino prima di NONNO");
        Assert.AreEqual("comboReached", d.steps[(int)TutorialManager.Step.Combo].gate.@event);
        Assert.AreEqual(TutorialManager.StepCount, (int)TutorialManager.Step.Chiuso);
    }
    [Test] public void EveryStep_StartAndGateLines_AreValid()
    {
        for (int i = 0; i < TutorialManager.StepCount; i++)
        {
            Assert.IsTrue(OnbLog.IsValid(OnbLog.Line("start", TutorialManager.StepIds[i], i, i * 10.0, "reveals", 1)));
            Assert.IsTrue(OnbLog.IsValid(OnbLog.Line("gate", TutorialManager.StepIds[i], i, i * 10.0 + 1, "event", "x", "dur", 1.0)));
        }
    }
}
