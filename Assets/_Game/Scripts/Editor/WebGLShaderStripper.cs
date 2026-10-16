using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace UltimoMiglio.EditorTools
{
    /// <summary>Su WebGL rimuove shader Hidden URP/Core che su SwiftShader loggano
    /// "ERROR: Shader … not supported on this GPU" senza servire al gameplay.</summary>
    public sealed class WebGLShaderStripper : IPreprocessShaders
    {
        public int callbackOrder => 0;
        static readonly string[] Drop =
        {
            "Hidden/CoreSRP/CoreCopy",
            "Hidden/Universal Render Pipeline/StencilDitherMaskSeed",
            "Hidden/Universal/HDRDebugView",
            "Hidden/Universal Render Pipeline/Debug/DebugReplacement",
        };

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            if (shader == null || data == null || data.Count == 0) return;
            var n = shader.name;
            for (int i = 0; i < Drop.Length; i++)
            {
                if (n == Drop[i])
                {
                    data.Clear();
                    return;
                }
            }
        }
    }
}
