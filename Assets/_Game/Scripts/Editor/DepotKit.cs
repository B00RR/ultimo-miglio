using UnityEditor;
using UnityEngine;

namespace UltimoMiglio.EditorTools
{
    /// <summary>Dresses the Ch1 depot with the 3D Artist kit (Environment/Kit, ThirdParty props, Solara houses).
    /// Placement is by renderer bounds (pivot-agnostic): bounds centre XZ at the target, bottom at target.y. Solid props get a BoxCollider.</summary>
    public static class DepotKit
    {
        const string Root = "Assets/_Game/Art/Environment/";
        static Transform _parent; static Material _mat;

        public static bool Build(Material world)
        {
            if (!AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Kit/env_floor_2x2_a.fbx")) { Debug.LogWarning("[Kit] kit missing: graybox"); return false; }
            _mat = world; _parent = new GameObject("DepotKit").transform; _parent.gameObject.isStatic = true;
            // floor 18x22: 2x2 tiles
            for (int x = 0; x < 9; x++) for (int z = 0; z < 11; z++)
                Place((x + z) % 2 == 0 ? "Kit/env_floor_2x2_a" : "Kit/env_floor_2x2_b", _parent, new Vector3(-8 + x * 2, -0.02f, -10 + z * 2), 0, false, false, 2f);
            // back walls (far from the camera: N and E), low plinth on the near sides (S, W) so they never hide Nina
            for (int i = 0; i < 9; i++) Place(i == 4 ? "Kit/env_wall_2m_b" : "Kit/env_wall_2m_a", _parent, new Vector3(-8 + i * 2, 0, 11.15f), 0, false, false, 2f, true);
            for (int i = 0; i < 11; i++) Place(i % 3 == 1 ? "Kit/env_wall_2m_b" : "Kit/env_wall_2m_a", _parent, new Vector3(9.15f, 0, -10 + i * 2), 90, false, false, 2f, true);
            Place("Kit/env_wall_corner", _parent, new Vector3(9.15f, 0, 11.15f), 0, false);
            for (int i = 0; i < 9; i++) Place("Kit/env_plinth_edge_2m", _parent, new Vector3(-8 + i * 2, 0, -11.15f), 0, false, false, 2f, true);
            for (int i = 0; i < 11; i++) Place("Kit/env_plinth_edge_2m", _parent, new Vector3(-9.15f, 0, -10 + i * 2), 90, false, false, 2f, true);
            // wall dressing (N wall), sign and lanterns
            Place("Kit/prop_sign_vela_figli", _parent, new Vector3(0, 2.2f, 10.9f), 0, false);
            foreach (var x in new[] { -6f, 3f, 7f }) Place("Kit/prop_lantern_wall", _parent, new Vector3(x, 1.9f, 10.9f), 0, false);
            Place("Kit/prop_wallshelf", _parent, new Vector3(-2.5f, 1.4f, 10.85f), 0, false);
            Place("Kit/prop_books", _parent, new Vector3(-2.5f, 1.55f, 10.85f), 0, false, true);
            // props along the edges (solid)
            Place("Kit/prop_lemontree_pot", _parent, new Vector3(8.2f, 0, 10.2f), 0, true);
            Place("Kit/prop_plant_small", _parent, new Vector3(-8.3f, 0, 10.3f), 0, true);
            Place("Kit/prop_safe", _parent, new Vector3(8.3f, 0, 4.5f), -90, true);
            Place("Kit/prop_carico0", _parent, new Vector3(-8.0f, 0, 2.5f), 90, true);
            Place("Kit/prop_box_m", _parent, new Vector3(-8.2f, 0, 8.6f), 15, true);
            Place("Kit/prop_box_s", _parent, new Vector3(-7.6f, 0, 9.4f), -10, true);
            Place("Kit/prop_box_m", _parent, new Vector3(-3.0f, 0, 10.2f), 5, true);
            Place("ThirdParty/Props/prop_tp_crate_open", _parent, new Vector3(-8.2f, 0, 6.8f), 0, true);
            Place("ThirdParty/Props/prop_tp_barrel", _parent, new Vector3(5.5f, 0, 10.3f), 0, true);
            Place("ThirdParty/Props/prop_tp_bench", _parent, new Vector3(8.3f, 0, -4f), 90, true);
            Place("ThirdParty/Props/prop_tp_plant_potted", _parent, new Vector3(8.3f, 0, -7.5f), 0, true);
            Place("ThirdParty/Props/prop_tp_lamp_wall", _parent, new Vector3(8.9f, 1.9f, 0f), -90, false);
            Place("ThirdParty/Props/prop_tp_basket", _parent, new Vector3(-8.2f, 0, -6f), 0, true);
            Place("ThirdParty/Props/prop_tp_bucket", _parent, new Vector3(-8.3f, 0, -8.5f), 0, true);
            // Solara houses as backdrop behind N and E walls (no colliders: outside the boundary)
            var houses = new[] { "a", "b", "c", "d", "e" };
            for (int i = 0; i < 5; i++) Place($"Solara/Houses/env_solara_house_{houses[i]}", _parent, new Vector3(-8 + i * 4.2f, -0.5f, 15.5f + (i % 2) * 1.5f), 180, false);
            for (int i = 0; i < 4; i++) Place($"Solara/Houses/env_solara_house_{houses[(i + 2) % 5]}", _parent, new Vector3(13.5f + (i % 2) * 1.5f, -0.5f, 7 - i * 5f), -90, false);
            Debug.Log("[Kit] depot dressed: " + _parent.childCount + " pieces");
            return true;
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
            if (_mat) foreach (var r in go.GetComponentsInChildren<Renderer>()) r.sharedMaterials = System.Array.ConvertAll(r.sharedMaterials, m => m && m.name.StartsWith("M_Diorama") ? m : _mat);
            foreach (var c in go.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            if (solid) { b = Bounds(go); var bc = go.AddComponent<BoxCollider>(); bc.center = go.transform.InverseTransformPoint(b.center); var ls = go.transform.lossyScale; var sz = Quaternion.Inverse(go.transform.rotation) * b.size; bc.size = new Vector3(Mathf.Abs(sz.x / ls.x), Mathf.Abs(sz.y / ls.y), Mathf.Abs(sz.z / ls.z)); }
            return true;
        }

        static Bounds Bounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) return default;
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b;
        }
    }
}
