using UnityEditor;
using System.IO;
using UnityEngine;

namespace UltimoMiglio.EditorTools
{
    /// <summary>Dresses the Ch1 depot with the 3D Artist kit (Environment/Kit, ThirdParty props, Solara houses).
    /// Placement is by renderer bounds (pivot-agnostic): bounds centre XZ at the target, bottom at target.y. Solid props get a BoxCollider.</summary>
    public static class DepotKit
    {
        const string Root = "Assets/_Game/Art/Environment/";
        static Transform _parent; static Material _mat, _emi;

        public static bool Build(Material world)
        {
            if (!AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Kit/env_floor_2x2_a.fbx")) { Debug.LogWarning("[Kit] kit missing: graybox"); return false; }
            _mat = world; _emi = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/VFX/Materials/M_Diorama_Atlas_Emissive.mat"); _parent = new GameObject("DepotKit").transform; _parent.gameObject.isStatic = true;
            // Un'unica lastra piatta 18x22: fughe solo dallo shader (no giunti tra cube, no FBX densissimo)
            PlaceFlatFloor(_parent, new Vector3(0f, -0.02f, -0.0f), new Vector3(18f, 0.04f, 22f));
            // back walls N/E avvicinati all'area di gioco (Concept: set #47 in camera con ortho ~6.5); bounds collisione restano a 11.15
            // #47: archi in tufo sul muro N (x -5 e +5) e sul muro E (z -1); se env_wall_arch_4m manca, muri pieni come prima
            bool arch = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Kit/env_wall_arch_4m.fbx");
            for (int i = 0; i < 9; i++)
            {
                if (arch && (i == 1 || i == 2 || i == 6 || i == 7)) { if (i == 1 || i == 6) Place("Kit/env_wall_arch_4m", _parent, new Vector3(-8 + i * 2 + 1, 0, 8.35f), 0, false, false, 4f, true); continue; }
                Place(i == 4 ? "Kit/env_wall_2m_b" : "Kit/env_wall_2m_a", _parent, new Vector3(-8 + i * 2, 0, 8.35f), 0, false, false, 2f, true);
            }
            for (int i = 0; i < 11; i++)
            {
                // Concept PR#55: muro E più vicino all'area di gioco (camera ortho ~6.5)
                if (arch && (i == 4 || i == 5)) { if (i == 4) Place("Kit/env_wall_arch_4m", _parent, new Vector3(7.6f, 0, -10 + i * 2 + 1), 90, false, false, 4f, true); continue; }
                Place(i % 3 == 1 ? "Kit/env_wall_2m_b" : "Kit/env_wall_2m_a", _parent, new Vector3(7.6f, 0, -10 + i * 2), 90, false, false, 2f, true);
            }
            Place("Kit/env_wall_corner", _parent, new Vector3(7.6f, 0, 8.35f), 0, false);
            for (int i = 0; i < 9; i++) Place("Kit/env_plinth_edge_2m", _parent, new Vector3(-8 + i * 2, 0, -11.15f), 0, false, false, 2f, true);
            for (int i = 0; i < 11; i++) Place("Kit/env_plinth_edge_2m", _parent, new Vector3(-9.15f, 0, -10 + i * 2), 90, false, false, 2f, true);
            // wall dressing (N wall), sign and lanterns
            Place("Kit/prop_sign_vela_figli", _parent, new Vector3(0, 1.95f, 8.08f), 0, false); DressSignFace(); // faro + Vela & Figli (piu basso, in camera)
            foreach (var x in new[] { -6f, 3f, 7f }) Place("Kit/prop_lantern_wall", _parent, new Vector3(x, 1.9f, 8.1f), 0, false);
            Place("Kit/prop_wallshelf", _parent, new Vector3(-2.5f, 1.4f, 8.05f), 0, false);
            Place("Kit/prop_books", _parent, new Vector3(-2.5f, 1.55f, 8.05f), 0, false, true);
            // props along the edges (solid)
            Place("Kit/prop_lemontree_pot", _parent, new Vector3(6.9f, 0, 7.4f), 0, true);
            Place("Kit/prop_plant_small", _parent, new Vector3(-7.6f, 0, 7.5f), 0, true);
            Place("Kit/prop_safe", _parent, new Vector3(6.9f, 0, 4.5f), -90, true);
            // #47: Carico 0 ora sta sul Bancone (SliceSceneBuilder), non più nell'angolo
            // #47: pacchi v03 (fallback ai vecchi prop_box se mancano), vasi di limoni dentro il deposito
            if (!Place("../Props/prop_parcel_box", _parent, new Vector3(-8.2f, 0, 8.6f), 15, true)) Place("Kit/prop_box_m", _parent, new Vector3(-8.2f, 0, 8.6f), 15, true);
            if (!Place("../Props/prop_parcel_box_flat", _parent, new Vector3(-8.2f, 0, 8.6f), -20, false, true)) Place("Kit/prop_box_s", _parent, new Vector3(-7.6f, 0, 9.4f), -10, true);
            if (!Place("Kit/prop_parcel_stack_a", _parent, new Vector3(-3.0f, 0, 7.5f), 5, true)) Place("Kit/prop_box_m", _parent, new Vector3(-3.0f, 0, 7.5f), 5, true);
            Place("Kit/prop_lemontree_pot", _parent, new Vector3(-6.9f, 0, 7.4f), 0, true);
            Place("Kit/prop_lemontree_pot", _parent, new Vector3(-3.1f, 0, 7.4f) + new Vector3(1.4f, 0, 0), 0, true);
            Place("Kit/prop_lemontree_pot", _parent, new Vector3(6.9f, 0, -3.3f), 0, true);
            Place("Kit/prop_lemontree_pot", _parent, new Vector3(6.9f, 0, 1.3f), 0, true);
            Place("Kit/prop_plant_pot_tall", _parent, new Vector3(3.1f, 0, 7.5f), 0, true);
            Place("Kit/prop_plant_pot_round", _parent, new Vector3(5.4f, 0, 7.5f), 0, true);
            Place("Kit/prop_planter_box", _parent, new Vector3(0, 0, 7.7f), 0, true);
            Place("Kit/prop_sign_hanging_parcel", _parent, new Vector3(7.4f, 2.4f, -3.1f), -90, false);
            Place("Kit/prop_streetlamp", _parent, new Vector3(-8.4f, 0, -10.2f), 45, true);
            Place("ThirdParty/Props/prop_tp_crate_open", _parent, new Vector3(-8.2f, 0, 6.8f), 0, true);
            Place("ThirdParty/Props/prop_tp_barrel", _parent, new Vector3(5.5f, 0, 7.5f), 0, true);
            Place("ThirdParty/Props/prop_tp_bench", _parent, new Vector3(6.9f, 0, -4f), 90, true);
            Place("ThirdParty/Props/prop_tp_plant_potted", _parent, new Vector3(6.9f, 0, -7.5f), 0, true);
            Place("ThirdParty/Props/prop_tp_lamp_wall", _parent, new Vector3(7.4f, 1.9f, 0f), -90, false);
            Place("ThirdParty/Props/prop_tp_basket", _parent, new Vector3(-8.2f, 0, -6f), 0, true);
            Place("ThirdParty/Props/prop_tp_bucket", _parent, new Vector3(-8.3f, 0, -8.5f), 0, true);
            // Solara houses as backdrop behind N and E walls (no colliders: outside the boundary)
            var houses = new[] { "a", "b", "c", "d", "e" };
            // #47 Borgo Gradoni: case sul gradone in tufo (env_solara_terrace_base_4x4) con scalinate tra una casa e l'altra
            bool tb = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Solara/Houses/env_solara_terrace_base_4x4.fbx");
            for (int i = 0; i < 5; i++)
            {
                var hp = new Vector3(-8 + i * 4.2f, -0.5f, 15.5f + (i % 2) * 1.5f);
                if (tb && i % 2 == 1) { Place("Solara/Houses/env_solara_terrace_base_4x4", _parent, hp, 0, false); hp.y += 1.6f; }
                Place($"Solara/Houses/env_solara_house_{houses[i]}", _parent, hp, 180, false);
                if (tb && i < 4) Place("Solara/Houses/env_solara_steps_2m", _parent, new Vector3(-8 + i * 4.2f + 2.1f, -0.5f, 13.4f), 180, false);
            }
            for (int i = 0; i < 4; i++) Place($"Solara/Houses/env_solara_house_{houses[(i + 2) % 5]}", _parent, new Vector3(13.5f + (i % 2) * 1.5f, -0.5f, 7 - i * 5f), -90, false);
            Debug.Log("[Kit] depot dressed: " + _parent.childCount + " pieces");
            return true;
        }

        /// <summary>#47 – set dressing denso vicino alle zone di gioco. SliceSceneBuilder lo chiama dopo aver calcolato arrPos/shelfC/banPos.
        /// Ogni Place è null-safe (FBX mancante = warning e salto). Le posizioni restano fuori dalle zone, dalla coda clienti (diagonale +x/-z dal CounterSlot) e dal pad NONNO.</summary>
        public static void DressGameplay(Vector3 arrPos, Vector3 shelfC, Vector3 banPos)
        {
            if (!_parent) _parent = new GameObject("DepotKit").transform;
            // Arrivi: pile e casse sul lato ovest/nord, lato est libero per il pad ASSUMI NONNO
            Place("Kit/prop_parcel_stack_a", _parent, arrPos + new Vector3(-2.3f, 0, 1.7f), 12, true);
            Place("Kit/prop_parcel_stack_a", _parent, arrPos + new Vector3(-0.6f, 0, 2.3f), -8, true);
            Place("../Props/prop_parcel_box_long", _parent, arrPos + new Vector3(-2.3f, 0, -0.2f), 85, true);
            Place("../Props/prop_parcel_box", _parent, arrPos + new Vector3(-2.2f, 0, -1.4f), 20, true);
            Place("../Props/prop_parcel_box_flat", _parent, arrPos + new Vector3(-2.2f, 0, -1.4f), -15, false, true);
            Place("ThirdParty/Props/prop_tp_crate_s", _parent, arrPos + new Vector3(0.9f, 0, 2.3f), 5, true);
            Place("ThirdParty/Props/prop_tp_cart", _parent, arrPos + new Vector3(1.9f, 0, 2.2f), -30, true);
            // corsia scaffali: vasi in testa, pacchi dietro
            Place("Kit/prop_lemontree_pot", _parent, shelfC + new Vector3(-3.6f, 0, -0.6f), 0, true);
            Place("Kit/prop_plant_pot_round", _parent, shelfC + new Vector3(3.6f, 0, -0.6f), 0, true);
            Place("../Props/prop_parcel_box_cube", _parent, shelfC + new Vector3(-1.1f, 0, -1.4f), 10, true);
            Place("../Props/prop_parcel_box_cube", _parent, shelfC + new Vector3(1.2f, 0, -1.4f), -25, true);
            Place("../Props/prop_parcel_box_flat", _parent, shelfC + new Vector3(1.2f, 0, -1.4f), 30, false, true);
            // Bancone: limoni e pila ai lati, lavagna e lampione lungo la coda (lato -x, la coda va verso +x)
            Place("Kit/prop_lemontree_pot", _parent, banPos + new Vector3(-1.75f, 0, -0.9f), 0, true);
            Place("Kit/prop_parcel_stack_a", _parent, banPos + new Vector3(-1.8f, 0, 0.6f), 25, true);
            Place("Kit/prop_sign_board_aperto", _parent, banPos + new Vector3(-1.2f, 0, -2.4f), 30, true);
            Place("Kit/prop_streetlamp", _parent, banPos + new Vector3(-1.9f, 0, -3.4f), 0, true);
            Place("Kit/prop_plant_pot_round", _parent, banPos + new Vector3(1.6f, 0, 0.7f), 0, true);
            // Concept: pezzo di muro/arco visibile dietro il Bancone (beauty shot)
            Place("Kit/env_wall_2m_a", _parent, banPos + new Vector3(-0.2f, 0, 2.8f), 0, false, false, 2f, true);
            Place("Kit/env_wall_2m_b", _parent, banPos + new Vector3(1.8f, 0, 2.8f), 0, false, false, 2f, true);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Kit/env_wall_arch_4m.fbx"))
                Place("Kit/env_wall_arch_4m", _parent, banPos + new Vector3(3.6f, 0, 1.2f), -90, false, false, 4f, true);
            Place("Kit/prop_sign_vela_figli", _parent, banPos + new Vector3(0.6f, 2.0f, 2.55f), 0, false); DressSignFace();
            Place("Kit/prop_lantern_wall", _parent, banPos + new Vector3(-1.4f, 1.85f, 2.55f), 0, false);
            Debug.Log("[Kit] gameplay dressing: " + _parent.childCount + " pieces");
        }

        /// <summary>Instantiates Root+rel+".fbx". fitLen>0 + alongX: rotate so the long side is along X (or Z when yaw=90).</summary>
        public static bool Place(string rel, Transform parent, Vector3 pos, float yaw, bool solid, bool onTop = false, float fitLen = 0, bool wall = false)
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(Root + rel + ".fbx");
            if (!src) { Debug.LogWarning("[Kit] missing " + rel); return false; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(src);
            if (parent || _parent) go.transform.SetParent(parent ? parent : _parent, true);
            go.transform.rotation = Quaternion.Euler(0, yaw, 0); go.transform.position = Vector3.zero;
            var b = Bounds(go); if (b.size == Vector3.zero) { Object.DestroyImmediate(go); return false; }
            if (wall) { bool wantX = Mathf.Abs(Mathf.DeltaAngle(yaw, 90)) > 45; bool isX = b.size.x >= b.size.z; if (wantX != isX) { go.transform.rotation = Quaternion.Euler(0, yaw + 90, 0); b = Bounds(go); } }
            go.transform.position += new Vector3(pos.x - b.center.x, pos.y - b.min.y, pos.z - b.center.z);
            go.isStatic = true;
            if (_mat) foreach (var r in go.GetComponentsInChildren<Renderer>()) r.sharedMaterials = System.Array.ConvertAll(r.sharedMaterials, m => m && m.name.Contains("Emissive") && _emi ? _emi : _mat);
            foreach (var c in go.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            b = Bounds(go);
            // #58: muri/archi sempre solidi; prop a terra alti >25cm (vasi, casse, set) solidi; tappeti/decal no
            if (!solid && (wall || (!onTop && b.min.y < 0.3f && b.size.y > 0.25f && Mathf.Max(b.size.x, b.size.z) < 6f))) solid = true;
            if (solid) { var bc = go.AddComponent<BoxCollider>(); bc.center = go.transform.InverseTransformPoint(b.center); var ls = go.transform.lossyScale; var sz = Quaternion.Inverse(go.transform.rotation) * b.size; bc.size = new Vector3(Mathf.Abs(sz.x / ls.x), Mathf.Abs(sz.y / ls.y), Mathf.Abs(sz.z / ls.z)); }
            return true;
        }

        // Concept: insegna con faro + "Vela & Figli" (texture generata, Fredoka).
        static void DressSignFace()
        {
            if (!_parent) return;
            Transform sign = null;
            for (int i = _parent.childCount - 1; i >= 0; i--)
            {
                var c = _parent.GetChild(i);
                if (c.name.Contains("prop_sign_vela_figli")) { sign = c; break; }
            }
            if (!sign || sign.Find("SignFaceDecal")) return;
            const string texPath = "Assets/_Game/Art/Environment/Kit/tex_sign_vela_figli.png";
            const string matPath = "Assets/_Game/Art/Materials/M_Sign_VelaFigli.mat";
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (!tex) { Debug.LogWarning("[Kit] missing sign tex " + texPath); return; }
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (!mat)
            {
                var sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture");
                if (!sh) { Debug.LogWarning("[Kit] no Unlit shader for sign"); return; }
                mat = new Material(sh) { name = "M_Sign_VelaFigli" };
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
                if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
                Directory.CreateDirectory(Path.GetDirectoryName(matPath));
                AssetDatabase.CreateAsset(mat, matPath);
            }
            else
            {
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
                if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            }
            var b = Bounds(sign.gameObject);
            foreach (var r in sign.GetComponentsInChildren<Renderer>()) r.enabled = false;
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.name = "SignFaceDecal";
            quad.transform.SetParent(sign, true);
            float w = Mathf.Clamp(b.size.x * 1.05f, 2.0f, 3.0f);
            float h = Mathf.Clamp(b.size.y * 1.05f, 0.7f, 1.15f);
            // Quad Unity: faccia frontale = -Z locale. Identity = guarda il deposito (verso -Z).
            quad.transform.position = new Vector3(b.center.x, Mathf.Max(b.center.y, 2.35f), Mathf.Min(b.min.z, 8.05f) - 0.06f);
            quad.transform.rotation = Quaternion.identity;
            quad.transform.localScale = new Vector3(w, h, 1f);
            var qr = quad.GetComponent<Renderer>();
            qr.sharedMaterial = mat;
            qr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            quad.isStatic = true;
            Debug.Log($"[Kit] SignFaceDecal at {quad.transform.position} size={w:F2}x{h:F2}");
        }

        static void PlaceFlatFloor(Transform parent, Vector3 center, Vector3 size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.name = "FloorSlab";
            go.transform.SetParent(parent ? parent : _parent, true);
            go.transform.position = center;
            go.transform.localScale = size;
            go.isStatic = true;
            if (_mat)
            {
                var r = go.GetComponent<Renderer>();
                r.sharedMaterial = _mat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        static Bounds Bounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) return default;
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b;
        }
    }
}
