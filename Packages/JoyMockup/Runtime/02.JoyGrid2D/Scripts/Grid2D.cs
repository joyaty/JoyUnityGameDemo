using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Joy.Grid2D
{ 
    public class Grid2D : MonoBehaviour
    {
        public float gridWidth;
        public float girdHeight;
        public bool isHinder;
        public Color color;
        public event System.Action onClick;

        private MeshRenderer m_MeshRender;

        // Start is called before the first frame update
        private void Start()
        {
            m_MeshRender = gameObject.GetComponent<MeshRenderer>();
        }

        // Update is called once per frame
        private void Update()
        {
            m_MeshRender.material.color = color;
        }

        private void OnMouseDown()
        {
            onClick?.Invoke();
        }
    }
}