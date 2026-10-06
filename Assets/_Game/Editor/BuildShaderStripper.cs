#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Strips unused 3D engine shaders (such as SpeedTree, Probe Volumes, and Debug Occluders)
    /// during standalone player builds for this 2D project.
    /// This eliminates shader compilation warnings and reduces player build time and artifact size.
    /// </summary>
    public sealed class BuildShaderStripper : IPreprocessShaders
    {
        public int callbackOrder => 0;

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            if (shader == null || string.IsNullOrEmpty(shader.name))
            {
                return;
            }

            string shaderName = shader.name;

            if (shaderName.IndexOf("SpeedTree", StringComparison.OrdinalIgnoreCase) >= 0 ||
                shaderName.IndexOf("ProbeVolume", StringComparison.OrdinalIgnoreCase) >= 0 ||
                shaderName.IndexOf("DebugOccluder", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                data.Clear();
            }
        }
    }
}
#endif
