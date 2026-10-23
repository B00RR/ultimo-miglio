using UnityEngine;
using UltimoMiglio.UI;

namespace UltimoMiglio.Gameplay
{
    /// <summary>
    /// #272: con Kaia entro il raggio del pad la colonna del prezzo (freccia, barra LN, pillola)
    /// non resta a 1,55 m sul centro — lì copre testa e busto — ma si sposta oltre il bordo,
    /// sul lato opposto alla camera (in alto sullo schermo, ancorata al bordo del pad).
    /// Se la fascia HUD o un'altra pillola occupano quel punto, scorre lungo il bordo.
    /// La pillola resta opaca: inchiostro su giallo sole, contrasto ≥ 4,5:1.
    /// Nessuna allocazione per frame: solo struct e il buffer del chiamante.
    /// </summary>
    public static class PricePillLayout
    {
        public const float DodgeSeconds = 0.15f;
        public const float RestHeight = 1.55f;
        public const float HighHeight = 1.05f;
        public const float HighScreenFrac = 0.72f;
        /// Larghezza e altezza della colonna in dp (freccia 29 + barra 22 + pillola ~41), come il rettangolo di #224.
        public const float ColumnWidthDp = 140f;
        public const float ColumnHeightDp = 92f;
        public const float HudTopDp = 206f;
        public const float EdgePadDp = 10f;
        public const float HalfChipDp = 70f;
        public const float GapDp = 8f;
        /// Capsula di Kaia (SliceSceneBuilder): centro locale y 0,8, altezza 1,6, raggio 0,3. I piedi sono all'origine.
        public const float KaiaHeight = 1.6f;
        public const float KaiaRadius = 0.3f;
        /// Metri oltre il bordo del disco, sul lato lontano dalla camera.
        public const float BeyondPad = 0.05f;

        public static float Smooth(float u)
        {
            if (u < 0f) u = 0f;
            else if (u > 1f) u = 1f;
            return u * u * (3f - 2f * u);
        }

        public static bool WithinRadius(Vector3 pad, Vector3 feet, float radius)
        {
            float dx = feet.x - pad.x, dz = feet.z - pad.z;
            float r = radius > 0.05f ? radius : 0.7f;
            return dx * dx + dz * dz <= r * r;
        }

        public static Vector3 Away(Transform cam)
        {
            Vector3 f = cam.forward;
            float m = f.x * f.x + f.z * f.z;
            if (m < 1e-8f) return Vector3.forward;
            float inv = 1f / Mathf.Sqrt(m);
            return new Vector3(f.x * inv, 0f, f.z * inv);
        }

        public static float ScaleOf(int screenW, int screenH) => ResponsivePanel.ComputeDpScale(screenW, screenH);

        public static Vector3 RestWorld(Vector3 pad, float bob) => new Vector3(pad.x, pad.y + RestHeight + bob, pad.z);

        public static bool PreferHigh(float screenY, float screenZ, float screenH) =>
            screenZ > 0f && screenY > screenH * HighScreenFrac;

        /// <summary>Proiezione a riposo (1,55 m, oppure 1,05 m se il punto è nel 28% alto). y dello schermo verso l'alto.</summary>
        public static Vector3 ProjectRest(Camera cam, Vector3 pad, float bob, float screenH)
        {
            Vector3 sp = cam.WorldToScreenPoint(RestWorld(pad, bob));
            if (PreferHigh(sp.y, sp.z, screenH))
                sp = cam.WorldToScreenPoint(new Vector3(pad.x, pad.y + HighHeight + bob * 0.5f, pad.z));
            return sp;
        }

        /// <summary>AABB a schermo della capsula di Kaia (8 angoli). y verso l'alto.</summary>
        public static Rect KaiaScreenBounds(Camera cam, Vector3 feet)
        {
            float minX = float.PositiveInfinity, minY = float.PositiveInfinity;
            float maxX = float.NegativeInfinity, maxY = float.NegativeInfinity;
            for (int i = 0; i < 8; i++)
            {
                var c = new Vector3(
                    feet.x + ((i & 1) == 0 ? -KaiaRadius : KaiaRadius),
                    feet.y + ((i & 2) == 0 ? 0f : KaiaHeight),
                    feet.z + ((i & 4) == 0 ? -KaiaRadius : KaiaRadius));
                Vector3 s = cam.WorldToScreenPoint(c);
                if (s.x < minX) minX = s.x;
                if (s.y < minY) minY = s.y;
                if (s.x > maxX) maxX = s.x;
                if (s.y > maxY) maxY = s.y;
            }
            return new Rect(minX, minY, maxX > minX ? maxX - minX : 0f, maxY > minY ? maxY - minY : 0f);
        }

        public static Rect ColumnRect(Vector2 anchorBottomCenter, float scale)
        {
            float w = ColumnWidthDp * scale, h = ColumnHeightDp * scale;
            return new Rect(anchorBottomCenter.x - w * 0.5f, anchorBottomCenter.y, w, h);
        }

        /// <summary>
        /// Ancora della colonna in pixel della camera (y verso l'alto). z = profondità del punto a riposo.
        /// dodge01 = 0 formula storica; 1 del tutto spostata. otherAnchors = ancore a riposo delle altre pillole.
        /// </summary>
        public static Vector3 AnchorScreen(Camera cam, Vector3 pad, float radius, Vector3 kaiaFeet, float dodge01, float bob, int screenW, int screenH, Vector2[] otherAnchors, int otherCount)
        {
            if (screenW < 2) screenW = 2;
            if (screenH < 2) screenH = 2;
            Vector3 restSp = ProjectRest(cam, pad, bob, screenH);
            if (dodge01 <= 0.0001f) return restSp;

            float scale = ScaleOf(screenW, screenH);
            float colW = ColumnWidthDp * scale, colH = ColumnHeightDp * scale, gap = GapDp * scale;
            float edge = EdgePadDp * scale, half = HalfChipDp * scale, hud = HudTopDp * scale;
            float minX = half + edge, maxX = screenW - half - edge;
            if (maxX < minX) minX = maxX = screenW * 0.5f;
            float minY = edge, maxY = screenH - hud - colH;
            if (maxY < minY) maxY = minY;

            float reach = (radius > 0.05f ? radius : 0.7f) + BeyondPad;
            Vector3 away = Away(cam.transform);
            Vector3 farSp = cam.WorldToScreenPoint(new Vector3(pad.x + away.x * reach, pad.y + RestHeight + bob, pad.z + away.z * reach));
            Rect kaia = KaiaScreenBounds(cam, kaiaFeet);
            float prefX = farSp.x;
            float prefY = farSp.y > kaia.yMax + gap ? farSp.y : kaia.yMax + gap;
            Vector2 solved = Solve(prefX, prefY, kaia, colW, colH, minX, maxX, minY, maxY, gap, otherAnchors, otherCount, 2f * scale);
            float k = dodge01 < 0f ? 0f : (dodge01 > 1f ? 1f : dodge01);
            return new Vector3(Mathf.Lerp(restSp.x, solved.x, k), Mathf.Lerp(restSp.y, solved.y, k), restSp.z);
        }

        /// <summary>Contrasto WCAG fra due colori opachi.</summary>
        public static float Contrast(Color a, Color b)
        {
            float la = Lum(a), lb = Lum(b);
            float hi = la > lb ? la : lb, lo = la > lb ? lb : la;
            return (hi + 0.05f) / (lo + 0.05f);
        }

        static float Lum(Color c) => 0.2126f * Chan(c.r) + 0.7152f * Chan(c.g) + 0.0722f * Chan(c.b);
        static float Chan(float c) => c <= 0.04045f ? c / 12.92f : Mathf.Pow((c + 0.055f) / 1.055f, 2.4f);

        static Vector2 Solve(float prefX, float prefY, Rect kaia, float colW, float colH, float minX, float maxX, float minY, float maxY, float gap, Vector2[] others, int n, float otherGap)
        {
            float yAbove = Clamp(prefY, minY, maxY);
            if (BestX(yAbove, prefX, minX, maxX, kaia, colW, colH, gap, others, n, otherGap, out float xAbove))
                return new Vector2(xAbove, yAbove);
            float yBelow = Clamp(kaia.yMin - gap - colH, minY, maxY);
            if (yBelow != yAbove && BestX(yBelow, prefX, minX, maxX, kaia, colW, colH, gap, others, n, otherGap, out float xBelow))
                return new Vector2(xBelow, yBelow);
            return new Vector2(Clamp(prefX, minX, maxX), yAbove);
        }

        static bool BestX(float y, float prefX, float minX, float maxX, Rect kaia, float colW, float colH, float gap, Vector2[] others, int n, float otherGap, out float bestX)
        {
            float bestD = float.MaxValue;
            bestX = prefX;
            bool ok = false;
            TryX(prefX, y, prefX, minX, maxX, kaia, colW, colH, gap, others, n, otherGap, ref bestD, ref bestX, ref ok);
            TryX(kaia.xMax + gap + colW * 0.5f, y, prefX, minX, maxX, kaia, colW, colH, gap, others, n, otherGap, ref bestD, ref bestX, ref ok);
            TryX(kaia.xMin - gap - colW * 0.5f, y, prefX, minX, maxX, kaia, colW, colH, gap, others, n, otherGap, ref bestD, ref bestX, ref ok);
            if (others != null)
            {
                for (int i = 0; i < n; i++)
                {
                    TryX(others[i].x + colW + otherGap, y, prefX, minX, maxX, kaia, colW, colH, gap, others, n, otherGap, ref bestD, ref bestX, ref ok);
                    TryX(others[i].x - colW - otherGap, y, prefX, minX, maxX, kaia, colW, colH, gap, others, n, otherGap, ref bestD, ref bestX, ref ok);
                }
            }
            float span = maxX - minX;
            for (int i = 0; i <= 32; i++)
                TryX(minX + span * (i / 32f), y, prefX, minX, maxX, kaia, colW, colH, gap, others, n, otherGap, ref bestD, ref bestX, ref ok);
            return ok;
        }

        static void TryX(float x, float y, float prefX, float minX, float maxX, Rect kaia, float colW, float colH, float gap, Vector2[] others, int n, float otherGap, ref float bestD, ref float bestX, ref bool ok)
        {
            if (x < minX) x = minX;
            else if (x > maxX) x = maxX;
            if (!Separated(x, y, colW, colH, kaia, gap, others, n, otherGap)) return;
            float d = x > prefX ? x - prefX : prefX - x;
            if (ok && d >= bestD) return;
            bestD = d;
            bestX = x;
            ok = true;
        }

        static bool Separated(float x, float y, float colW, float colH, Rect kaia, float gap, Vector2[] others, int n, float otherGap)
        {
            float left = x - colW * 0.5f, bottom = y, right = left + colW, top = bottom + colH;
            if (left < kaia.xMax + gap && right > kaia.xMin - gap && bottom < kaia.yMax + gap && top > kaia.yMin - gap)
                return false;
            if (others == null || n <= 0) return true;
            for (int i = 0; i < n; i++)
            {
                float ol = others[i].x - colW * 0.5f, ob = others[i].y, oright = ol + colW, otop = ob + colH;
                if (left < oright + otherGap && right > ol - otherGap && bottom < otop + otherGap && top > ob - otherGap)
                    return false;
            }
            return true;
        }

        static float Clamp(float v, float min, float max) => v < min ? min : (v > max ? max : v);
    }
}
