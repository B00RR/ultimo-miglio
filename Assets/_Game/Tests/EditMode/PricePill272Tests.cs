using NUnit.Framework;
using UnityEngine;
using UltimoMiglio.Gameplay;

/// <summary>#272: con Kaia al centro del pad e sul bordo, il rettangolo a schermo della pillola prezzo
/// non interseca i bounds a schermo della capsula di Kaia. La pillola resta opaca (contrasto ≥ 4,5:1).</summary>
public class PricePill272Tests
{
    const float Radius = 0.7f;

    [Test]
    public void Raggio_CentroEBordoSonoVicini_FuoriNo()
    {
        var pad = Vector3.zero;
        Assert.IsTrue(PricePillLayout.WithinRadius(pad, Vector3.zero, Radius), "centro");
        Assert.IsTrue(PricePillLayout.WithinRadius(pad, new Vector3(0f, 0f, Radius), Radius), "bordo nord");
        Assert.IsFalse(PricePillLayout.WithinRadius(pad, new Vector3(0f, 0f, Radius + 0.05f), Radius), "oltre il bordo");
    }

    [Test]
    public void Transizione_DuraQuindiciCentesimi_EArriva()
    {
        Assert.AreEqual(0.15f, PricePillLayout.DodgeSeconds, 1e-6f);
        Assert.AreEqual(0f, PricePillLayout.Smooth(0f), 1e-5f);
        Assert.AreEqual(1f, PricePillLayout.Smooth(1f), 1e-5f);
        Assert.AreEqual(0.5f, PricePillLayout.Smooth(0.5f), 1e-5f);
    }

    [Test]
    public void Prezzo_InchiostroSuGiallo_ContrastoAlmenoQuattroEMezzo()
    {
        Assert.AreEqual(1f, InteractionPoint.Giallo.a, 1e-5f, "la pillola resta opaca");
        Assert.AreEqual(1f, InteractionPoint.Ink.a, 1e-5f);
        float c = PricePillLayout.Contrast(InteractionPoint.Ink, InteractionPoint.Giallo);
        Assert.GreaterOrEqual(c, 4.5f, "contrasto inchiostro su giallo sole");
    }

    [TestCase(390, 844, 4.5f)]
    [TestCase(412, 915, 4.5f)]
    [TestCase(390, 844, 3.825f)]
    public void CentroEBordo_LaPillolaNonCopreKaia(int w, int h, float ortho)
    {
        var cam = CameraIso(w, h, ortho, 50f, 45f);
        var rt = cam.targetTexture;
        try
        {
            Assert.AreEqual(w, cam.pixelWidth, "larghezza viewport");
            Assert.AreEqual(h, cam.pixelHeight, "altezza viewport");
            var pad = Vector3.zero;
            var others = new Vector2[2];
            others[0] = Ancora(cam, new Vector3(0f, 0f, 1.9f), h);
            others[1] = Ancora(cam, new Vector3(0f, 0f, -1.9f), h);
            float scale = PricePillLayout.ScaleOf(w, h);
            Verifica(cam, pad, Vector3.zero, others, scale, w, h, "centro");
            for (int deg = 0; deg < 360; deg += 45)
            {
                float a = deg * Mathf.Deg2Rad;
                var feet = new Vector3(Mathf.Cos(a) * Radius, 0f, Mathf.Sin(a) * Radius);
                Verifica(cam, pad, feet, others, scale, w, h, "bordo " + deg);
            }
        }
        finally
        {
            cam.targetTexture = null;
            Object.DestroyImmediate(cam.gameObject);
            if (rt) Object.DestroyImmediate(rt);
        }
    }

    static void Verifica(Camera cam, Vector3 pad, Vector3 feet, Vector2[] others, float scale, int w, int h, string nome)
    {
        Assert.IsTrue(PricePillLayout.WithinRadius(pad, feet, Radius), nome + " entro il raggio");
        Vector3 sp = PricePillLayout.AnchorScreen(cam, pad, Radius, feet, 1f, 0f, w, h, others, others.Length);
        var pill = PricePillLayout.ColumnRect(new Vector2(sp.x, sp.y), scale);
        var body = PricePillLayout.KaiaScreenBounds(cam, feet);
        Assert.IsFalse(pill.Overlaps(body), nome + ": pillola " + pill + " copre Kaia " + body);
        float hud = PricePillLayout.HudTopDp * scale;
        Assert.LessOrEqual(pill.yMax, h - hud + 0.6f, nome + ": la colonna entra nella fascia HUD");
        for (int i = 0; i < others.Length; i++)
        {
            var altra = PricePillLayout.ColumnRect(others[i], scale);
            Assert.IsFalse(pill.Overlaps(altra), nome + ": pillola sopra l'altra a " + others[i]);
        }
    }

    static Vector2 Ancora(Camera cam, Vector3 pad, int h)
    {
        Vector3 s = PricePillLayout.ProjectRest(cam, pad, 0f, h);
        Assert.Greater(s.z, 0f, "pad davanti alla camera");
        return new Vector2(s.x, s.y);
    }

    static Camera CameraIso(int w, int h, float ortho, float pitch, float yaw)
    {
        var go = new GameObject("pill-cam-272");
        var cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = ortho;
        cam.aspect = (float)w / h;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 120f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        cam.transform.position = -cam.transform.forward * 20f;
        var rt = new RenderTexture(w, h, 16);
        cam.targetTexture = rt;
        cam.rect = new Rect(0f, 0f, 1f, 1f);
        return cam;
    }
}
