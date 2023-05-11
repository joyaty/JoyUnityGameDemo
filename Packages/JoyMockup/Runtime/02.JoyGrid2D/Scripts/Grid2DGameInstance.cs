
using UnityEngine;

namespace Joy.Grid2D
{
    public class Grid2DGameInstance : MonoBehaviour
    {
        //获取网格创建脚本
        public Grid2DMeshCreator gridMeshCreate;
        //控制网格元素grid是障碍的概率
        [Range(0, 1)]
        public float probability;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Run();
            }
        }
        private void Run()
        {
            gridMeshCreate.gridEvent += GridEvent;
            gridMeshCreate.CreateMesh();
        }

        /// <summary>
        /// 创建grid时执行的方法，通过委托传入
        /// </summary>
        /// <param name="grid"></param>
        private void GridEvent(Grid2D grid)
        {
            //概率随机决定该元素是否为障碍
            float f = Random.Range(0, 1.0f);
            Debug.Log(f.ToString());
            grid.color = f <= probability ? Color.red : Color.white;
            grid.isHinder = f <= probability;
            //模板元素点击事件
            grid.onClick += () =>
            {
                if (!grid.isHinder)
                    grid.color = Color.blue;
            };
        }
    }
}
