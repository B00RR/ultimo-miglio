using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UltimoMiglio.Core;
using UltimoMiglio.Gameplay;

/// <summary>#171 (QA): il trigger di ogni UpgradePad coincide con il cerchio visibile. Kaia ferma in 8 punti all'80% del raggio
/// (N, NE, E, SE, S, SO, O, NO) fa caricare il pad; ferma al 120% del raggio no.</summary>
public class PadTriggerCerchioTest
{
    static readonly string[] Dir = { "N", "NE", "E", "SE", "S", "SO", "O", "NO" };

    [UnityTest, Timeout(240000)]
    public IEnumerator OgniPad_SiCaricaDentroIlCerchio_NonFuori()
    {
        AnimFacingTest.ResetGameKeys(); TutorialManager.TestSenzaComparse = true; TutorialManager.TestBloccaPassi = true; Time.timeScale = 1f;
        try
        {
            yield return SceneManager.LoadSceneAsync("Ch1_Deposito");
            for (int i = 0; i < 120 && (!GameRoot.Ready || !GameRoot.I.Nina); i++) yield return null;
            Assert.IsTrue(GameRoot.Ready, "GameRoot non pronto");
            if (TutorialManager.I) TutorialManager.I.Skip();
            var eco = GameRoot.I.Eco; var nina = GameRoot.I.Nina; var cc = nina.GetComponent<CharacterController>();
            var pads = Object.FindObjectsByType<UpgradePad>(FindObjectsSortMode.None);
            Assert.GreaterOrEqual(pads.Length, 1, "nessun UpgradePad in scena");
            Vector3 lontano = new Vector3(0f, nina.transform.position.y, -6f);
            int provati = 0;
            foreach (var pad in pads)
            {
                var ip = pad.GetComponent<InteractionPoint>(); Assert.IsNotNull(ip, $"{pad.name}: InteractionPoint mancante");
                var ring = ip.RingVisual ? ip.RingVisual : pad.transform;
                Vector3 c = ring.position; float r = ip.Radius * InteractionPoint.VisualScale; // bordo esterno dell'anello visibile
                var col = pad.GetComponent<Collider>();
                var cap = col as CapsuleCollider; Assert.IsNotNull(cap, $"{pad.name}: il trigger non è tondo ({col.GetType().Name})");
                Assert.AreEqual(r - cc.radius, cap.radius, 0.01f, $"{pad.name}: raggio trigger diverso da raggio cerchio - raggio di Kaia");
                Assert.Less(Vector2.Distance(new Vector2(col.bounds.center.x, col.bounds.center.z), new Vector2(c.x, c.z)), 0.01f, $"{pad.name}: trigger non centrato sul cerchio");
                Debug.Log($"[#171] {pad.name} cerchio=({c.x:0.00},{c.z:0.00}) r={r:0.00} trigger={col.bounds.center:F2} ext={col.bounds.extents:F2}");
                if (double.IsInfinity(eco.Cost(pad.Kind))) { Debug.Log($"[#171] {pad.name}: livello massimo, salto"); continue; }
                for (int k = 0; k < 8 * 2; k++)
                {
                    bool dentro = k < 8; float f = dentro ? 0.8f : 1.2f;
                    float a = (k % 8) * 45f * Mathf.Deg2Rad; var off = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * (r * f); // N = +z
                    yield return Metti(nina, cc, lontano); yield return Attendi(0.3f);
                    eco.S.Wallet = 1e6; eco.S.PadPaid[(int)pad.Kind] = 0;
                    double prima = eco.S.Wallet;
                    yield return Metti(nina, cc, new Vector3(c.x + off.x, nina.transform.position.y, c.z + off.z)); yield return Attendi(1.2f);
                    bool carica = eco.S.Wallet < prima;
                    string chi = $"{pad.name} {Dir[k % 8]} {(dentro ? "80%" : "120%")} (Kaia {nina.transform.position:F2})";
                    if (dentro) Assert.IsTrue(carica && pad.Occupied, $"{chi}: il pad non si carica dentro il cerchio");
                    else Assert.IsFalse(carica || pad.Occupied, $"{chi}: il pad si carica fuori dal cerchio");
                }
                provati++;
            }
            Assert.Greater(provati, 0, "nessun pad provato");
        }
        finally { TutorialManager.TestBloccaPassi = false; TutorialManager.TestSenzaComparse = false; Time.timeScale = 1f; }
    }

    static IEnumerator Metti(NinaController n, CharacterController cc, Vector3 p)
    {
        // CharacterController acceso: il collider si sposta con il transform e la fisica manda OnTriggerEnter/Exit come camminando
        n.ScriptedInput = Vector3.zero; n.transform.position = p; Physics.SyncTransforms();
        yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
    }
    static IEnumerator Attendi(float s) { float t = Time.time; while (Time.time - t < s) yield return null; }
}
