// Ultimo Miglio - procedural game-feel particles (mobile: low counts, unlit, no collisions/lights/trails).
// Each Build* returns a configured, non-playing ParticleSystem GameObject. Prefabs are baked by UM_FxPrefabBuilder.
using UnityEngine;

namespace UltimoMiglio.VFX
{
    public static class UM_FxFactory
    {
        public const string ParticleShader = "Universal Render Pipeline/Particles/Unlit";
        static Material _mat;
        /// Shared additive-free alpha-blended unlit material (1 material for all FX = batching). Uses default particle texture.
        public static Material SharedMaterial
        {
            get
            {
                if (_mat) return _mat;
                _mat = new Material(Shader.Find(ParticleShader)) { name = "M_FX_Particles" };
                _mat.SetFloat("_Surface", 1f); _mat.SetFloat("_Blend", 0f);
                _mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                _mat.renderQueue = 3000;
                return _mat;
            }
        }

        /// T_FX_Sheet 4x2 tiles: 0 soft dot, 1 puff, 2 sparkle, 3 soft ring (Kenney Particle Pack CC0); 4 spark lemon core + sun halo (pre-coloured, original)
        public const int TileDot = 0, TilePuff = 1, TileStar = 2, TileRing = 3, TileSpark = 4;
        static void Tile(ParticleSystem ps, int tile)
        {
            var t = ps.textureSheetAnimation; t.enabled = true; t.mode = ParticleSystemAnimationMode.Grid;
            t.numTilesX = 4; t.numTilesY = 2; t.animation = ParticleSystemAnimationType.WholeSheet;
            t.frameOverTime = new ParticleSystem.MinMaxCurve(0f); t.startFrame = new ParticleSystem.MinMaxCurve(tile / 8f); t.cycleCount = 1;
        }
        static int N(int n) => Mathf.Max(1, Mathf.RoundToInt(n * UM_QualityTier.ParticleScale));

        static ParticleSystem Make(string name, Material mat, int maxParticles, out ParticleSystemRenderer r)
        {
            var go = new GameObject(name);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var m = ps.main;
            m.playOnAwake = false; m.loop = false; m.maxParticles = maxParticles;
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            m.scalingMode = ParticleSystemScalingMode.Hierarchy;
            m.cullingMode = ParticleSystemCullingMode.Automatic;
            m.stopAction = ParticleSystemStopAction.Disable; // pool-friendly
            var e = ps.emission; e.rateOverTime = 0;
            r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat ? mat : SharedMaterial;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.sortingFudge = 0; r.maxParticleSize = 0.3f;
            return ps;
        }

        static ParticleSystem.MinMaxGradient Grad(Color a, Color b) => new ParticleSystem.MinMaxGradient(a, b);
        static void FadeOut(ParticleSystem ps, Color c, float hold = 0.6f)
        {
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(c, 0), new GradientColorKey(c, 1) },
                      new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, hold), new GradientAlphaKey(0, 1) });
            col.color = g;
        }
        /// Contact AO blob: flat quad under characters/packages (no SSAO). One shared material, ~2 tris each.
        public static GameObject BuildContactShadow(Material blobMat, float diameter = 0.8f)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Quad); g.name = "fx_contact_shadow";
            Object.DestroyImmediate(g.GetComponent<Collider>());
            g.transform.localRotation = Quaternion.Euler(90, 0, 0); g.transform.localPosition = new Vector3(0, 0.012f, 0);
            g.transform.localScale = Vector3.one * diameter;
            var r = g.GetComponent<MeshRenderer>(); r.sharedMaterial = blobMat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            return g;
        }

        static void ShrinkOut(ParticleSystem ps)
        {
            var s = ps.sizeOverLifetime; s.enabled = true;
            s.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, 0.6f), new Keyframe(0.15f, 1.1f), new Keyframe(1, 0)));
        }

        /// Package pop: when a box lands on the stack / shelf / counter. 10 cardboard+cream flakes, 0.45 s.
        public static GameObject BuildPackagePop(Material mat = null)
        {
            var ps = Make("fx_package_pop", mat, 16, out _); Tile(ps, TileDot);
            var m = ps.main;
            m.duration = 0.3f; m.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.45f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3f);
            m.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.12f);
            m.startColor = Grad(UM_Palette.Cartone, UM_Palette.Crema);
            m.gravityModifier = 0.8f;
            var e = ps.emission; e.SetBursts(new[] { new ParticleSystem.Burst(0, (short)N(10)) });
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Hemisphere; sh.radius = 0.2f;
            ShrinkOut(ps);
            return ps.gameObject;
        }

        /// Coins: arc burst toward HUD / from Cassa. Use with 24-48 tri coin mesh (05_3D_PIPELINE) if provided, else billboard.
        public static GameObject BuildCoins(Mesh coinMesh = null, Material mat = null, Material coinMat = null)
        {
            var ps = Make("fx_coins", mat, 12, out var r); if (!coinMesh) Tile(ps, TileDot);
            var m = ps.main;
            m.duration = 0.4f; m.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 0.8f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 4f);
            m.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.18f);
            m.startColor = Grad(UM_Palette.MonetaOro, Color.Lerp(UM_Palette.MonetaOro, UM_Palette.MonetaBordo, 0.2f)); // lira gold, brown rim on mesh/texture
            m.gravityModifier = 1.6f;
            m.startRotation3D = false;
            var e = ps.emission; e.SetBursts(new[] { new ParticleSystem.Burst(0, (short)N(6)), new ParticleSystem.Burst(0.12f, (short)N(3)) });
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 20f; sh.radius = 0.05f;
            sh.rotation = new Vector3(-90, 0, 0);
            var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(4f, 8f);
            ShrinkOut(ps);
            if (coinMesh)
            {   // mesh coins sample the palette atlas through their own UVs: white vertex colour so atlas hexes stay exact
                r.renderMode = ParticleSystemRenderMode.Mesh; r.mesh = coinMesh; r.alignment = ParticleSystemRenderSpace.World;
                if (coinMat) r.sharedMaterial = coinMat;
                m.startColor = Color.white; m.startSize = new ParticleSystem.MinMaxCurve(0.9f, 1.1f); // mesh authored at real size
                rot.enabled = false; m.startRotation3D = true;
                var r3 = ps.rotationOverLifetime; r3.enabled = true; r3.separateAxes = true;
                r3.y = new ParticleSystem.MinMaxCurve(6f, 10f); r3.x = 0; r3.z = 0;
            }
            // glint child: 1 cream sparkle per burst (vertex colors are LDR: it reads as a flash, not bloom)
            var glint = Make("glint", mat, 2, out _); Tile(glint, TileStar);
            glint.transform.SetParent(ps.transform, false);
            var gm = glint.main; gm.duration = 0.2f; gm.startLifetime = 0.2f; gm.startSpeed = 0;
            gm.startSize = 0.5f; gm.startColor = UM_Palette.MonetaLuce;
            var ge = glint.emission; ge.SetBursts(new[] { new ParticleSystem.Burst(0, 1) });
            ShrinkOut(glint);
            return ps.gameObject;
        }

        /// Upgrade: spiral of sun/lemon sparks rising from the upgrade pad + ground ring. ~22 particles total.
        public static GameObject BuildUpgrade(Material mat = null)
        {
            var ps = Make("fx_upgrade", mat, 24, out _); Tile(ps, TileSpark);
            var m = ps.main;
            m.duration = 0.8f; m.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.0f);
            m.startSpeed = 0.2f;
            m.startSize = new ParticleSystem.MinMaxCurve(0.11f, 0.21f); // v1.4: +30%
            m.startColor = Color.white; // colours baked in TileSpark: #F7E26B core, #F4C430 halo
            m.gravityModifier = -0.3f;
            var e = ps.emission; e.rateOverTime = N(20);
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 0.7f; sh.radiusThickness = 0f;
            sh.rotation = new Vector3(90, 0, 0);
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.Local;
            vel.orbitalZ = new ParticleSystem.MinMaxCurve(3f); vel.y = new ParticleSystem.MinMaxCurve(1.6f);
            vel.x = new ParticleSystem.MinMaxCurve(0f); vel.z = new ParticleSystem.MinMaxCurve(0f);
            FadeOut(ps, Color.white);
            ShrinkOut(ps);
            // ring: single expanding flat particle (horizontal billboard)
            var ring = Make("ring", mat, 1, out var rr); Tile(ring, TileRing);
            ring.transform.SetParent(ps.transform, false);
            rr.renderMode = ParticleSystemRenderMode.HorizontalBillboard; rr.maxParticleSize = 3f;
            // soft ring, slow pulse (2 breaths over 1.2 s), max alpha 0.45
            var rm = ring.main; rm.duration = 1.2f; rm.startLifetime = 1.2f; rm.startSpeed = 0; rm.startSize = 2.2f;
            var rc = UM_Palette.UpgradeSole; rc.a = 0.45f; rm.startColor = rc;
            var re = ring.emission; re.SetBursts(new[] { new ParticleSystem.Burst(0, 1) });
            var rs = ring.sizeOverLifetime; rs.enabled = true;
            rs.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0, 0.6f), new Keyframe(0.25f, 0.95f), new Keyframe(0.5f, 0.85f), new Keyframe(0.75f, 1f), new Keyframe(1f, 0.95f)));
            FadeOut(ring, rc);
            return ps.gameObject;
        }

        /// Dust: footstep/run puffs under Nina and NONNO wheels. Looping, distance-based (no emission when idle). Max 8 alive.
        public static GameObject BuildDust(Material mat = null)
        {
            var ps = Make("fx_dust", mat, 8, out _); Tile(ps, TilePuff);
            var m = ps.main;
            m.loop = true; m.playOnAwake = true; m.duration = 1f;
            m.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.55f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
            m.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.32f);
            m.startColor = Grad(UM_Palette.Polvere, Color.Lerp(UM_Palette.Polvere, UM_Palette.Legno, 0.3f));
            m.gravityModifier = -0.05f;
            m.stopAction = ParticleSystemStopAction.None;
            var e = ps.emission; e.rateOverTime = 0; e.rateOverDistance = N(3); // ~3 puffs per meter walked
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Hemisphere; sh.radius = 0.1f;
            FadeOut(ps, UM_Palette.Polvere, 0.8f); // v1.4: holds alpha longer
            var s = ps.sizeOverLifetime; s.enabled = true;
            s.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0, 0.5f, 1, 1.2f));
            return ps.gameObject;
        }

        // ===================== GAME FEEL (sprint My Little Universe) =====================

        /// Raccolta pacco: piccolo "pop" verso l'alto (cartone + crema) + 2 stelline. ~11 particelle, 0.4 s.
        public static GameObject BuildPackagePickup(Material mat = null)
        {
            var ps = Make("fx_package_pickup", mat, 14, out _); Tile(ps, TileDot);
            var m = ps.main;
            m.duration = 0.25f; m.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.4f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(1.8f, 3.2f);
            m.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.13f);
            m.startColor = Grad(UM_Palette.Cartone, UM_Palette.Crema);
            m.gravityModifier = 0.6f;
            var e = ps.emission; e.SetBursts(new[] { new ParticleSystem.Burst(0, (short)N(9)) });
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 25f; sh.radius = 0.15f; sh.rotation = new Vector3(-90, 0, 0);
            ShrinkOut(ps);
            var st = Make("stars", mat, 3, out _); st.transform.SetParent(ps.transform, false); Tile(st, TileStar);
            var sm = st.main; sm.duration = 0.2f; sm.startLifetime = 0.3f; sm.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1f);
            sm.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.35f); sm.startColor = UM_Palette.Crema;
            var se = st.emission; se.SetBursts(new[] { new ParticleSystem.Burst(0, 2) });
            var ss = st.shape; ss.shapeType = ParticleSystemShapeType.Sphere; ss.radius = 0.25f;
            ShrinkOut(st);
            return ps.gameObject;
        }

        /// Consegna pacco (bancone/cliente/furgone): anello a terra giallo sole + coriandoli palette che ricadono + stella centrale. ~17 particelle, 0.7 s.
        public static GameObject BuildPackageDeliver(Material mat = null)
        {
            var ps = Make("fx_package_deliver", mat, 20, out _); Tile(ps, TileDot);
            var m = ps.main;
            m.duration = 0.3f; m.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.7f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 4f);
            m.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.2f); // v2.0: +40%
            var g = new Gradient(); // coriandoli: terracotta, giallo sole, menta, limone
            g.SetKeys(new[] { new GradientColorKey(UM_Palette.Terracotta, 0), new GradientColorKey(UM_Palette.GialloSole, 0.33f), new GradientColorKey(UM_Palette.Menta, 0.66f), new GradientColorKey(UM_Palette.Limone, 1) },
                      new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) });
            m.startColor = new ParticleSystem.MinMaxGradient(g) { mode = ParticleSystemGradientMode.RandomColor };
            m.gravityModifier = 1.4f;
            var e = ps.emission; e.SetBursts(new[] { new ParticleSystem.Burst(0, (short)N(14)) });
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 35f; sh.radius = 0.2f; sh.rotation = new Vector3(-90, 0, 0);
            ShrinkOut(ps);
            var ring = Make("ring", mat, 1, out var rr); ring.transform.SetParent(ps.transform, false); Tile(ring, TileRing);
            rr.renderMode = ParticleSystemRenderMode.HorizontalBillboard; rr.maxParticleSize = 3f;
            var rm = ring.main; rm.duration = 0.45f; rm.startLifetime = 0.45f; rm.startSpeed = 0; rm.startSize = 2.3f; rm.startColor = UM_Palette.UpgradeSole;
            var re = ring.emission; re.SetBursts(new[] { new ParticleSystem.Burst(0, 1) });
            var rs = ring.sizeOverLifetime; rs.enabled = true; rs.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0, 0.25f, 1, 1f));
            FadeOut(ring, UM_Palette.UpgradeSole, 0.3f);
            var st = Make("star", mat, 1, out _); st.transform.SetParent(ps.transform, false); st.transform.localPosition = Vector3.up * 0.4f; Tile(st, TileStar);
            var sm = st.main; sm.duration = 0.2f; sm.startLifetime = 0.3f; sm.startSpeed = 0; sm.startSize = 1.0f; sm.startColor = UM_Palette.Crema;
            var se = st.emission; se.SetBursts(new[] { new ParticleSystem.Burst(0, 1) });
            ShrinkOut(st);
            return ps.gameObject;
        }

        /// Acquisto (pad pagato, sblocco, oggetto comprato): monete oro verso l'alto + anello menta + 2 stelle. ~15 particelle, 0.7 s.
        public static GameObject BuildPurchase(Material mat = null)
        {
            var ps = Make("fx_purchase", mat, 18, out _); Tile(ps, TileDot);
            var m = ps.main;
            m.duration = 0.3f; m.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.7f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(3f, 4.5f);
            m.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.2f);
            m.startColor = Grad(UM_Palette.MonetaOro, UM_Palette.UpgradeLimone);
            m.gravityModifier = 1.3f;
            var e = ps.emission; e.SetBursts(new[] { new ParticleSystem.Burst(0, (short)N(12)) });
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 22f; sh.radius = 0.15f; sh.rotation = new Vector3(-90, 0, 0);
            ShrinkOut(ps);
            var ring = Make("ring", mat, 1, out var rr); ring.transform.SetParent(ps.transform, false); Tile(ring, TileRing);
            rr.renderMode = ParticleSystemRenderMode.HorizontalBillboard; rr.maxParticleSize = 3f;
            var rm = ring.main; rm.duration = 0.45f; rm.startLifetime = 0.45f; rm.startSpeed = 0; rm.startSize = 2.0f; rm.startColor = UM_Palette.Menta;
            var re = ring.emission; re.SetBursts(new[] { new ParticleSystem.Burst(0, 1) });
            var rs = ring.sizeOverLifetime; rs.enabled = true; rs.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0, 0.2f, 1, 1f));
            FadeOut(ring, UM_Palette.Menta, 0.3f);
            var st = Make("stars", mat, 2, out _); st.transform.SetParent(ps.transform, false); st.transform.localPosition = Vector3.up * 0.6f; Tile(st, TileStar);
            var sm = st.main; sm.duration = 0.2f; sm.startLifetime = 0.35f; sm.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.8f); sm.startSize = new ParticleSystem.MinMaxCurve(0.6f, 0.9f); sm.startColor = UM_Palette.Crema;
            var se = st.emission; se.SetBursts(new[] { new ParticleSystem.Burst(0, 2) });
            var ss = st.shape; ss.shapeType = ParticleSystemShapeType.Sphere; ss.radius = 0.3f;
            ShrinkOut(st);
            return ps.gameObject;
        }

        /// Upgrade completato sul pad: flash a terra + scintille (TileSpark) a fontana. ~18 particelle, 0.8 s.
        public static GameObject BuildUpgradeComplete(Material mat = null)
        {
            var ps = Make("fx_upgrade_complete", mat, 22, out _); Tile(ps, TileSpark);
            var m = ps.main;
            m.duration = 0.3f; m.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 0.8f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(3f, 4.5f);
            m.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.24f);
            m.startColor = Color.white; m.gravityModifier = 1.2f;
            var e = ps.emission; e.SetBursts(new[] { new ParticleSystem.Burst(0, (short)N(16)) });
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 30f; sh.radius = 0.5f; sh.rotation = new Vector3(-90, 0, 0);
            ShrinkOut(ps);
            var fl = Make("flash", mat, 1, out var fr); fl.transform.SetParent(ps.transform, false); Tile(fl, TileDot);
            fr.renderMode = ParticleSystemRenderMode.HorizontalBillboard; fr.maxParticleSize = 3f;
            var fm = fl.main; fm.duration = 0.3f; fm.startLifetime = 0.3f; fm.startSpeed = 0; fm.startSize = 2.4f;
            var fc = UM_Palette.UpgradeLimone; fc.a = 0.8f; fm.startColor = fc;
            var fe = fl.emission; fe.SetBursts(new[] { new ParticleSystem.Burst(0, 1) });
            FadeOut(fl, fc, 0.1f);
            return ps.gameObject;
        }

        /// Moneta volante verso l'HUD: mesh lira (o billboard) + scia corta + scintille per distanza. Mossa da UM_CoinFlight.
        public static GameObject BuildCoinFlyer(Mesh coinMesh, Material coinMat, Material fxMat, Material trailMat)
        {
            var go = new GameObject("fx_coin_flyer");
            var body = new GameObject("coin"); body.transform.SetParent(go.transform, false);
            if (coinMesh) { body.AddComponent<MeshFilter>().sharedMesh = coinMesh; var mr = body.AddComponent<MeshRenderer>(); mr.sharedMaterial = coinMat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false; }
            var tr = go.AddComponent<TrailRenderer>();
            tr.sharedMaterial = trailMat; tr.time = 0.18f; tr.minVertexDistance = 0.08f; tr.numCapVertices = 0; tr.numCornerVertices = 0;
            tr.widthCurve = AnimationCurve.EaseInOut(0, 0.12f, 1, 0f);
            var tg = new Gradient(); tg.SetKeys(new[] { new GradientColorKey(UM_Palette.UpgradeLimone, 0), new GradientColorKey(UM_Palette.MonetaOro, 1) },
                                                new[] { new GradientAlphaKey(0.9f, 0), new GradientAlphaKey(0, 1) });
            tr.colorGradient = tg; tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; tr.receiveShadows = false; tr.emitting = false;
            var sp = Make("sparkle", fxMat, 6, out _); sp.transform.SetParent(go.transform, false); Tile(sp, TileStar);
            var sm = sp.main; sm.loop = true; sm.playOnAwake = false; sm.duration = 1f; sm.startLifetime = 0.25f; sm.startSpeed = 0.2f;
            sm.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.2f); sm.startColor = UM_Palette.MonetaLuce; sm.stopAction = ParticleSystemStopAction.None;
            var se = sp.emission; se.rateOverDistance = 2.5f;
            ShrinkOut(sp);
            go.AddComponent<UM_CoinFlight>();
            return go;
        }

        /// Anello di riempimento pad: quad a terra con UM_FxRingFill + UM_PadFillRing (SetProgress 0..1, flash a fine).
        public static GameObject BuildPadFillRing(Material ringMat, float diameter = 2.2f)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Quad); g.name = "fx_pad_fill_ring";
            Object.DestroyImmediate(g.GetComponent<Collider>());
            var q = new GameObject("fx_pad_fill_ring_root");
            g.transform.SetParent(q.transform, false);
            g.transform.localRotation = Quaternion.Euler(90, 0, 0); g.transform.localPosition = new Vector3(0, 0.015f, 0);
            g.transform.localScale = Vector3.one * diameter;
            var r = g.GetComponent<MeshRenderer>(); r.sharedMaterial = ringMat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            q.AddComponent<UM_PadFillRing>().ring = r;
            q.name = "fx_pad_fill_ring";
            g.name = "ring_quad";
            return q;
        }
    }
}
