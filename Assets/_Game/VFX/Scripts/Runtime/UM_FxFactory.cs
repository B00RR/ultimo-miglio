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
    }
}
