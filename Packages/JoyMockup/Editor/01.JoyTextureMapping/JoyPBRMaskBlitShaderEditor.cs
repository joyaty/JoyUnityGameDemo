
#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace Joy.Editor.Shader
{ 
    public class JoyPBRMaskBlitShaderEditor : BaseShaderGUI
    {
        private MaterialProperty m_PBRTextureProp;
        private MaterialProperty m_MaskTextureProp;

        public override void FindProperties(MaterialProperty[] properties)
        {
            Material material = materialEditor != null ? materialEditor.target as Material : null;
            if (material == null)
            {
                return;
            }
            m_PBRTextureProp = FindProperty("_BaseMap", properties);
            m_MaskTextureProp = FindProperty("_MaskMap", properties);

        }

        public override void DrawSurfaceOptions(Material material)
        {
            base.DrawSurfaceOptions(material);
            materialEditor.ShaderProperty(m_PBRTextureProp, new GUIContent("RGB通道源纹理"));
            materialEditor.ShaderProperty(m_MaskTextureProp, new GUIContent("Alpha通道源纹理"));
        }
    }
}

#endif