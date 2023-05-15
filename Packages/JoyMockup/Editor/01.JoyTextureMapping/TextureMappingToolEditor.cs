
using UnityEngine;

namespace Joy.Tool.Editor
{ 
    [UnityEditor.CustomEditor(typeof(JoyTextureMappingTool))]
    public class TextureMappingToolEditor : UnityEditor.Editor
    {
        private JoyTextureMappingTool m_Script;

        private void OnEnable()
        {
            m_Script = target as JoyTextureMappingTool;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("测试功能"))
            {
                m_Script?.OdinButtonForTestFunc();
            }
        }
    }
}
