// Ultimo Miglio - #191: prewarm degli shader al boot. Su WebGL il programma GPU di un materiale viene compilato/linkato
// solo al primo disegno: anelli dei pad che si sbloccano dopo, faro del tutorial, icone pad nascoste e particelle in pool
// (tutti inattivi all'avvio) causavano uno scatto la prima volta che comparivano. Qui ogni materiale distinto
// (shader + keyword) dei renderer della scena, anche inattivi, viene disegnato una volta su un quad minuscolo davanti
// alla camera, durante i primi frame (dietro al caricamento). Nessuna allocazione dopo il boot, nessun file di #198 toccato.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UltimoMiglio.VFX
{
    public sealed class UM_ShaderWarmup : MonoBehaviour
    {
        const int PerFrame = 8; // distribuisce le compilazioni su più frame

#if !UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var g = new GameObject("UM_ShaderWarmup") { hideFlags = HideFlags.HideAndDontSave };
            g.AddComponent<UM_ShaderWarmup>();
        }
#endif

        IEnumerator Start()
        {
            yield return null; // i pool FX/icone vengono creati in Awake/Start: raccogli dopo
            var mats = CollectDistinct();
            var mesh = BuildQuad();
            var props = new MaterialPropertyBlock();
            for (int w = 0; w < 300 && !Camera.main; w++) yield return null; // aspetta la camera (max ~5 s)
            int done = 0;
            for (int i = 0; i < mats.Count; i++)
            {
                var cam = Camera.main;
                if (!cam) break;
                // quad di 1 mm a 1 m davanti alla camera: nel frustum (niente culling) ma invisibile
                var t = cam.transform;
                var m = Matrix4x4.TRS(t.position + t.forward * (cam.nearClipPlane + 1f), t.rotation, Vector3.one * 0.001f);
                Graphics.DrawMesh(mesh, m, mats[i], 0, cam, 0, props, false, false, false);
                if (++done % PerFrame == 0) yield return null;
            }
            yield return null;
            Debug.Log("[UM VFX] shader prewarm: " + mats.Count + " materiali");
            Destroy(mesh);
            Destroy(gameObject);
        }

        static List<Material> CollectDistinct()
        {
            var seen = new HashSet<string>();
            var list = new List<Material>();
            foreach (var r in FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                foreach (var m in r.sharedMaterials)
                {
                    if (!m || !m.shader) continue;
                    string key = m.shader.name + "|" + string.Join(",", m.shaderKeywords) + "|" + m.renderQueue;
                    if (seen.Add(key)) list.Add(m);
                }
            return list;
        }

        static Mesh BuildQuad()
        {
            var q = new Mesh { name = "UM_WarmupQuad", hideFlags = HideFlags.HideAndDontSave };
            q.vertices = new[] { new Vector3(-.5f, -.5f, 0), new Vector3(.5f, -.5f, 0), new Vector3(-.5f, .5f, 0), new Vector3(.5f, .5f, 0) };
            q.uv = new[] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one };
            q.normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
            q.colors = new[] { Color.white, Color.white, Color.white, Color.white };
            q.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            q.RecalculateBounds();
            return q;
        }
    }
}
