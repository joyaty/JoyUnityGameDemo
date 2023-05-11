
using UnityEngine;

namespace Joy.Grid2D
{
    public class Grid2DMeshCreator : MonoBehaviour
    {
        [System.Serializable]
        public struct MeshRange
        {
            public int width;
            public int height;
        }
        public MeshRange meshRange;
        public Vector3 startPos;
        public Transform parentTrans;
        public GameObject gridPrefab;
        public event System.Action<Grid2D> gridEvent;
        private Grid2D[,] m_Grid2Ds;
        public Grid2D[,] grid2Ds => m_Grid2Ds;

        public void CreateMesh()
        {
            if (meshRange.width == 0 || meshRange.height == 0)
            {
                return;
            }
            ClearMesh();
            m_Grid2Ds = new Grid2D[meshRange.width, meshRange.height];
            for (int i = 0; i < meshRange.width; i++)
            {
                for (int j = 0; j < meshRange.height; j++)
                {
                    CreateGrid(i, j);
                }
            }
        }

        /// <summary>
        /// 重载，基于传入宽高数据来创建网格
        /// </summary>
        /// <param name="height"></param>
        /// <param name="widght"></param>
        public void CreateMesh(int height, int widght)
        {
            if (widght == 0 || height == 0)
            {
                return;
            }
            ClearMesh();
            m_Grid2Ds = new Grid2D[widght, height];
            for (int i = 0; i < widght; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    CreateGrid(i, j);
                }
            }
        }

        /// <summary>
        /// 根据位置创建一个基本的Grid物体
        /// </summary>
        /// <param name="row">x轴坐标</param>
        /// <param name="column">y轴坐标</param>
        public void CreateGrid(int row, int column)
        {
            GameObject go = GameObject.Instantiate(gridPrefab, parentTrans);
            Grid2D grid = go.GetComponent<Grid2D>();

            float posX = -meshRange.width * 0.5f + grid.gridWidth * row;
            float posY = -meshRange.height * 0.5f + grid.girdHeight * column;
            go.transform.position = new Vector3(posX, posY, startPos.z);
            m_Grid2Ds[row, column] = grid;
            gridEvent?.Invoke(grid);
        }

        /// <summary>
        /// 删除网格地图，并清除缓存数据
        /// </summary>
        public void ClearMesh()
        {
            if (m_Grid2Ds == null || m_Grid2Ds.Length == 0)
            {
                return;
            }
            foreach (Grid2D grid in m_Grid2Ds)
            {
                if (grid.gameObject != null)
                {
                    Destroy(grid.gameObject);
                }
            }
            System.Array.Clear(m_Grid2Ds, 0, m_Grid2Ds.Length);
        }
    }
}
