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
                if (arch && (i == 1 || i == 2 || i == 6 || i == 7)) { if (i == 1 || i == 6) Place("Kit/env_wall_arch_4m", _parent, new Vector3(-8 + i * 2 + 1, 0, 8.35f), 0, false, false, true); continue; }
                Place(i == 4 ? "Kit/env_wall_2m_b" : "Kit/env_wall_2m_a", _parent, new Vector3(-8 + i * 2, 0, 8.35f), 0, false, false, true);
            }
            for (int i = 0; i < 11; i++)
            {
                // Concept PR#55: muro E più vicino all'area di gioco (camera ortho ~6.5)
                if (arch && (i == 4 || i == 5)) { if (i == 4) Place("Kit/env_wall_arch_4m", _parent, new Vector3(7.6f, 0, -10 + i * 2 + 1), 90, false, false, true); continue; }
                Place(i % 3 == 1 ? "Kit/env_wall_2m_b" : "Kit/env_wall_2m_a", _parent, new Vector3(7.6f, 0, -10 + i * 2), 90, false, false, true);
            }
            Place("Kit/env_wall_corner", _parent, new Vector3(7.6f, 0, 8.35f), 0, false);
            for (int i = 0; i < 9; i++) Place("Kit/env_plinth_edge_2m", _parent, new Vector3(-8 + i * 2, 0, -11.15f), 0, false, false, true);
            for (int i = 0; i < 11; i++) Place("Kit/env_plinth_edge_2m", _parent, new Vector3(-9.15f, 0, -10 + i * 2), 90, false, false, true);
            // wall dressing (N wall), sign and lanterns
            Place("Kit/prop_sign_sunn_depot", _parent, new Vector3(0, 1.95f, 8.08f), 0, false); DressSignFace(); // faro + Vela & Figli (piu basso, in camera)
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
            // #65: angolo SO. Con yaw 45 il BoxCollider di Place usciva sottile (dimensioni AABB ruotate) e Nina passava dentro il palo.
            // Ora il palo sta a ridosso dell'angolo (fessura palo-staccionata < diametro di Nina) e ha un collider proprio, allineato alla mesh.
            if (Place("Kit/prop_streetlamp", _parent, new Vector3(-8.4f, 0, -10.2f), 45, false)) PoleCollider(_parent.GetChild(_parent.childCount - 1), new Vector3(-8.62f, 0, -10.62f));
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
            Place("Kit/prop_lemontree_pot", _parent, banPos + new Vector3(-1.75f, 0, -0.5f), 0, true); // #97: varco ovest chiuso (vaso a 0.18 dalla pila, 0.23 dal Bancone): spostarlo di 0.35 a ovest lasciava 0.58 m
            Place("Kit/prop_parcel_stack_a", _parent, banPos + new Vector3(-1.8f, 0, 0.6f), 25, true);
            Place("Kit/prop_sign_board_aperto", _parent, banPos + new Vector3(-1.2f, 0, -2.4f), 30, true);
            Place("Kit/prop_streetlamp", _parent, banPos + new Vector3(-1.9f, 0, -3.4f), 0, true);
            Place("Kit/prop_plant_pot_round", _parent, banPos + new Vector3(-2.4f, 0, 1.6f), 0, true); // #187: spostato a ovest, fra Bancone e Cassa non c'era spazio con anello = trigger
            // Concept: pezzo di muro/arco visibile dietro il Bancone (beauty shot)
            Place("Kit/env_wall_2m_a", _parent, banPos + new Vector3(-0.2f, 0, 1.45f), 0, false, false, true); // #82 opzione A: muro a ridosso, niente sacca
            Place("Kit/env_wall_2m_b", _parent, banPos + new Vector3(2.0f, 0, 1.45f), 0, false, false, true);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Kit/env_wall_arch_4m.fbx"))
                Place("Kit/env_wall_arch_4m", _parent, banPos + new Vector3(3.2f, 0, -0.3f), -90, false, false, true); // #97: a 3.6 restava una fessura di ~0.45 m tra muro_b e arco: i clienti (r 0.3) ci si incastravano e non arrivavano al Bancone
            Place("Kit/prop_sign_sunn_depot", _parent, banPos + new Vector3(0.6f, 2.0f, 1.2f), 0, false); DressSignFace();
            Place("Kit/prop_lantern_wall", _parent, banPos + new Vector3(-1.4f, 1.85f, 1.2f), 0, false);
            Debug.Log("[Kit] gameplay dressing: " + _parent.childCount + " pieces");
        }

        /// <summary>Instantiates Root+rel+".fbx". wall: ruota di 90 se serve perché il lato lungo segua X (o Z con yaw=90).
        /// #82: bounds calcolati a yaw 0 (locali), yaw applicato dopo, così i collider non si gonfiano; gli archi hanno 3 collider (pilastri + architrave).</summary>
        public static bool Place(string rel, Transform parent, Vector3 pos, float yaw, bool solid, bool onTop = false, bool wall = false)
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(Root + rel + ".fbx");
            if (!src) { Debug.LogWarning("[Kit] missing " + rel); return false; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(src);
            if (parent || _parent) go.transform.SetParent(parent ? parent : _parent, true);
            foreach (var c in go.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            go.transform.rotation = Quaternion.identity; go.transform.position = Vector3.zero;
            var b = Bounds(go); if (b.size == Vector3.zero) { Object.DestroyImmediate(go); return false; }
            bool localX = b.size.x >= b.size.z;
            if (wall)
            {
                bool wantX = Mathf.Abs(Mathf.Sin(yaw * Mathf.Deg2Rad)) < 0.7071f; // #82: anche yaw -90 vuol dire lato lungo su Z (arco del set a fianco del Bancone)
                bool side = Mathf.Abs(Mathf.DeltaAngle(yaw, 90)) <= 45 || Mathf.Abs(Mathf.DeltaAngle(yaw, -90)) <= 45;
                bool worldX = localX != side;
                if (wantX != worldX) yaw += 90;
            }
            var rot = Quaternion.Euler(0, yaw, 0);
            var off = rot * new Vector3(b.center.x, 0, b.center.z);
            go.transform.rotation = rot; go.transform.position = new Vector3(pos.x - off.x, pos.y - b.min.y, pos.z - off.z);
            go.isStatic = true;
            if (_mat) foreach (var r in go.GetComponentsInChildren<Renderer>()) r.sharedMaterials = System.Array.ConvertAll(r.sharedMaterials, m => m && m.name.Contains("Emissive") && _emi ? _emi : _mat);
            // #58: muri/archi sempre solidi; prop a terra alti >25cm (vasi, casse, set) solidi; tappeti/decal no
            float minY = pos.y;
            if (!solid && (wall || (!onTop && minY < 0.3f && b.size.y > 0.25f && Mathf.Max(b.size.x, b.size.z) < 6f))) solid = true;
            if (!solid) return true;
            var ls = go.transform.lossyScale; var inv = new Vector3(1f / ls.x, 1f / ls.y, 1f / ls.z);
            if (rel.Contains("wall_arch"))
            {
                // pilastri 0-0.2L e 0.8L-L a tutta altezza, architrave 0.2L-0.8L da 0.75H (2.4 su 3.2) in su
                float L = localX ? b.size.x : b.size.z, H = b.size.y;
                Vector3 ax = localX ? Vector3.right : Vector3.forward, th = localX ? new Vector3(0, 0, b.size.z) : new Vector3(b.size.x, 0, 0);
                AddBox(go, b.center - ax * (0.4f * L), ax * (0.2f * L) + Vector3.up * H + th, inv);
                AddBox(go, b.center + ax * (0.4f * L), ax * (0.2f * L) + Vector3.up * H + th, inv);
                AddBox(go, new Vector3(b.center.x, b.min.y + 0.875f * H, b.center.z), ax * (0.6f * L) + Vector3.up * (0.25f * H) + th, inv);
            }
            else AddBox(go, b.center, b.size, inv);
            return true;
        }

        static void AddBox(GameObject go, Vector3 localCenter, Vector3 localSize, Vector3 inv)
        {
            var bc = go.AddComponent<BoxCollider>(); bc.center = Vector3.Scale(localCenter, inv); bc.size = Vector3.Scale(localSize, inv);
        }

        /// <summary>#65: lampione. Il pivot dell'FBX è alla base del palo (mesh: palo r 0.146 all'origine, braccio +X 0.62 a 2.3-2.6 m).
        /// Sposta il pivot del palo su polePos (XZ) e mette un BoxCollider solido sul solo palo (0.36x2.6x0.36, assi mondo): sotto il braccio non c'è niente da urtare.</summary>
        static void PoleCollider(Transform lamp, Vector3 polePos)
        {
            if (!lamp || !lamp.name.Contains("streetlamp")) return;
            foreach (var c in lamp.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            lamp.position = new Vector3(polePos.x, lamp.position.y, polePos.z);
            var col = new GameObject("prop_streetlamp_col");
            col.transform.SetParent(_parent, false);
            col.transform.position = new Vector3(polePos.x, lamp.position.y + 1.3f, polePos.z);
            col.isStatic = true;
            col.AddComponent<BoxCollider>().size = new Vector3(0.36f, 2.6f, 0.36f);
        }

        // #70: l'insegna è la mesh 3D prop_sign_sunn_depot v05 «Sunn Depot» (#130; ex prop_sign_vela_figli, stesso GUID) (cornice terracotta, tavola noce, faro in rilievo con lanterna emissiva,
        // scritta crema in rilievo in Fredoka). Prima qui si spegnevano i renderer della mesh e si metteva un quad Unlit piatto
        // (tex_sign_vela_figli.png, fondo crema): era il «pannello bianco piatto senza faro». Ora si tiene la mesh e si toglie l'eventuale decal.
        static void DressSignFace()
        {
            if (!_parent) return;
            for (int i = _parent.childCount - 1; i >= 0; i--)
            {
                var c = _parent.GetChild(i);
                if (!c.name.Contains("prop_sign_sunn_depot")) continue;
                foreach (var r in c.GetComponentsInChildren<Renderer>(true)) r.enabled = true;
                var d = c.Find("SignFaceDecal"); if (d) Object.DestroyImmediate(d.gameObject);
            }
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
