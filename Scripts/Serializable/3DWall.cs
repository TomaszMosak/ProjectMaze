using UnityEngine;

namespace ProjectMaze
{
    [System.Serializable]
    public class WallPiece3D
    {
        public GameObject wallPrefab;
        public float wallHeight;

        public WallPiece3D(GameObject wallPrefab, float wallHeight) {
            this.wallPrefab = wallPrefab;
            this.wallHeight = wallHeight;
        }
    }
}