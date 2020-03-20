using UnityEngine;

//Purpose: To keep track of connections between nodes of the maze (most likely for pathfinding purposes only) -- Implement if enough time

namespace ProjectMaze
{
    public class ReadOnlyInInspectorAttribute : PropertyAttribute
    {
        //this class has no special properties, it is just used to access a property drawer
        //Allows the user to see the properties without being able to mess with them

        //Found this trick on stackoverflow, this isn't my work.
    }
    public class MazeNode : MonoBehaviour
    {
        [ReadOnlyInInspector]
        [Tooltip("The state of this tile in the maze map")]
        public MazeMap.TileState type;
        [ReadOnlyInInspector]
        [Tooltip("The (column, row) coordinates of this tile in the maze map")]
        public Vector2 coordinates;
        [ReadOnlyInInspector]
        [Tooltip("The tile up from this one")]
        public MazeNode up;
        [ReadOnlyInInspector]
        [Tooltip("The tile right from this one")]
        public MazeNode right;
        [ReadOnlyInInspector]
        [Tooltip("The tile down from this one")]
        public MazeNode down;
        [ReadOnlyInInspector]
        [Tooltip("The tile left from this one")]
        public MazeNode left;

        public void AddConnectionUp(MazeNode connectedNode) {
            up = connectedNode;
            connectedNode.down = this;
        }

        public void AddConnectionRight(MazeNode connectedNode) {
            right = connectedNode;
            connectedNode.left = this;
        }

        public void AddConnectionDown(MazeNode connectedNode) {
            down = connectedNode;
            connectedNode.up = this;
        }

        public void AddConnectionLeft(MazeNode connectedNode) {
            left = connectedNode;
            connectedNode.right = this;
        }
    }
}