
using UnityEngine;

namespace Joy.Tool.Editor
{ 
    [UnityEditor.CustomEditor(typeof(TextureMappingTool))]
    public class TextureMappingToolEditor : UnityEditor.Editor
    {
        private TextureMappingTool m_Script;

        private void OnEnable()
        {
            m_Script = target as TextureMappingTool;
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
