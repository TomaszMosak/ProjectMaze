using UnityEngine;

namespace ProjectMaze
{
    [System.Serializable]
    public class WallPiece2D
    {
        public GameObject wallPrefab;
        public float wallZPlane;

        public WallPiece2D(GameObject wallPrefab, float wallZPlane) {
            this.wallPrefab = wallPrefab;
            this.wallZPlane = wallZPlane;
        }
    }
}
