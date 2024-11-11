using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Rendering;
using UnityEngine.Rendering;
using UnityEngine;

namespace CartoonFX
{
    class CFXR_BuildShaderPreprocessor : IPreprocessShaders, IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        static int shaderVariantsRemoved;
        static bool isUsingURP;

        // --------------------------------------------------------------------------------------------------------------------------------
        // IPreprocessBuildWithReport, IPostprocessBuildWithReport

        public void OnPreprocessBuild(BuildReport report)
        {
            // Figure out if we're using built-in or URP
#if UNITY_2019_3_OR_NEWER
            var renderPipeline = GraphicsSettings.currentRenderPipeline;
#else
            var renderPipeline = GraphicsSettings.renderPipelineAsset;
#endif
            isUsingURP = renderPipeline != null && renderPipeline.GetType().Name.Contains("Universal");
            shaderVariantsRemoved = 0;
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            if (shaderVariantsRemoved > 0)
            {
                string currentPipeline = isUsingURP ? "Universal" : "Built-in";
                Debug.Log(string.Format("<color=#ec7d38>[Cartoon FX Remaster]</color> {0} Render Pipeline detected, {1} Shader variants have been stripped from the build.", currentPipeline, shaderVariantsRemoved));
            }
        }

        // --------------------------------------------------------------------------------------------------------------------------------
        // IPreprocessShaders

        public int callbackOrder
        {
            get { return 1000; }
        }

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> shaderCompilerData)
        {
            if (snippet.passType == PassType.ShadowCaster)
            {
                return;
            }
            
            if (shader.name.Contains("Cartoon FX/Remaster"))
            {
                // Strip Cartoon FX Remaster Shader variants based on current render pipeline
                if ((isUsingURP && snippet.passType != PassType.ScriptableRenderPipeline) ||
                    (!isUsingURP && snippet.passType == PassType.ScriptableRenderPipeline))
                {
                    shaderVariantsRemoved += shaderCompilerData.Count;
                    shaderCompilerData.Clear();
                }
            }
        }
    }
}