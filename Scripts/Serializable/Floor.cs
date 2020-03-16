using UnityEngine;

namespace ProjectMaze
{
    [System.Serializable]
    public class FloorPiece
    {
        public GameObject floorPrefab;
        public float floorThickness;

        public FloorPiece(GameObject floorPrefab, float floorThickness) {
            this.floorPrefab = floorPrefab;
            this.floorThickness = floorThickness;
        }
    }
}
