using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UltimoMiglio.Core;
using UltimoMiglio.Gameplay;
using UltimoMiglio.VFX;

/// <summary>#189 (criterio QA del Game Designer): ogni passo con obiettivo ha un bersaglio non nullo, l'anello compare entro 0,5 s
/// e la freccia a terra punta al bersaglio e si muove (chevron/bob). Vale anche per cassaPass.</summary>
public class TutorialGuidaTest
{
    [UnityTest, Timeout(180000)]
    public IEnumerator OgniPasso_HaBersaglio_AnelloEntroMezzoSecondo_FrecciaVersoObiettivo()
    {
        AnimFacingTest.ResetGameKeys(); TutorialManager.TestSenzaComparse = true; TutorialManager.TestAccelera = true; TutorialManager.TestBloccaPassi = true;
        try
        {
            yield return SceneManager.LoadSceneAsync("Ch1_Deposito");
            for (int i = 0; i < 120 && (!GameRoot.Ready || !GameRoot.I.Nina || TutorialManager.I == null); i++) yield return null;
            var tm = TutorialManager.I; Assert.IsNotNull(tm); var nina = GameRoot.I.Nina;
            int frecceViste = 0;
            for (var s = TutorialManager.Step.Move; s <= TutorialManager.Step.WallPad; s++)
            {
                if (!TutorialManager.HasWorldTarget(s)) continue; // move: joystick + mano fantasma (spec #189)
                tm.TestEntraNelPasso(s);
                float t0 = Time.unscaledTime; yield return null;
                Assert.AreEqual(s, tm.Current, $"passo {s} non attivo");
                var g = tm.GuidaDi(s);
                Assert.IsNotNull(g.Target, $"passo {s}: bersaglio della freccia nullo");
                Assert.AreSame(g.Target, tm.CurrentTarget, $"passo {s}: CurrentTarget diverso dalla guida");
                while (Time.unscaledTime - t0 < 0.5f) yield return null;
                Assert.AreEqual(s, tm.Current, $"passo {s} chiuso da solo durante il test");
                Assert.IsTrue(UM_TutorialMarker.Visible, $"passo {s}: anello non visibile entro 0,5 s");
                Assert.AreSame(tm.CurrentTarget, UM_TutorialMarker.Target, $"passo {s}: l'anello non è sul bersaglio del passo");
                if (s == TutorialManager.Step.CassaPass) Assert.IsNotNull(Object.FindAnyObjectByType<CassaZone>(), "Cassa mancante");

                var arrow = UM_TutorialMarker.ArrowTransform;
                var d = g.Target.position - nina.transform.position; d.y = 0;
                if (g.Freccia && d.magnitude > g.FrecciaOltre + 0.5f)
                {
                    Assert.IsNotNull(arrow); Assert.IsTrue(arrow.gameObject.activeInHierarchy, $"passo {s}: freccia a terra assente ({UM_TutorialMarker.Stato}, d={d.magnitude:0.0})");
                    Assert.Greater(Vector3.Dot(arrow.forward, d.normalized), 0.95f, $"passo {s}: la freccia non punta al bersaglio");
                    var p0 = arrow.position; float tm0 = Time.time; while (Time.time - tm0 < 0.2f) yield return null;
                    Assert.Greater((arrow.position - p0).sqrMagnitude, 1e-6f, $"passo {s}: freccia a terra statica");
                    frecceViste++;
                }
            }
            Assert.Greater(frecceViste, 3, "troppo poche frecce a terra verificate");
        }
        finally { TutorialManager.TestBloccaPassi = false; TutorialManager.TestSenzaComparse = false; TutorialManager.TestAccelera = false; Time.timeScale = 1f; }
    }
}
