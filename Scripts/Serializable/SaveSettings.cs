using UnityEngine;

namespace ProjectMaze
{
    [System.Serializable]
    public class MazeSettings
    {
        private string  _mazeName;
        public string MazeName
        {
            get { return _mazeName; }
        }
        private GenMaze.MazeDimension _mazeDimension;
        public GenMaze.MazeDimension MazeDimension
        {
            get { return _mazeDimension; }
        }
        private int _seedValue;
        public int SeedValue
        {
            get { return _seedValue; }
        }
        private int _mazeWidth;
        public int MazeWidth
        {
            get { return _mazeWidth; }
        }
        private int _mazeLength;
        public int MazeLength
        {
            get { return _mazeLength; }
        }
        private float[] _mazePosition;
        public Vector3 MazePosition
        {
            get { return SerializeUnityThings.BackToVector3(_mazePosition); }
        }
        private float _mazeTileWidthAndLength;
        public float MazeTileWidthAndLength
        {
            get { return _mazeTileWidthAndLength; }
        }
        private float _defaultWallHeight;
        public float DefaultWallHeight
        {
            get { return _defaultWallHeight; }
        }
        private float _defaultFloorThickness;
        public float DefaultFloorThickness
        {
            get { return _defaultFloorThickness; }
        }
        private float _wallZPlane;
        public float WallZPlane
        {
            get { return _wallZPlane; }
        }
        private float _floorZPlane;
        public float FloorZPlane
        {
            get { return _floorZPlane; }
        }
        private bool _makeBraidMaze;
        public bool MakeBraidMaze
        {
            get { return _makeBraidMaze; }
        }
        private float _braidFrequency;
        public float braidFrequency {
            get { return _braidFrequency; }
        }
        private bool _deleteOutsideWalls;
        public bool DeleteOutsideWalls
        {
            get { return _deleteOutsideWalls; }
        }
        public int _outsideWallPiecesToDelete;
        public int OutsideWallPiecesToDelete
        {
            get { return _outsideWallPiecesToDelete; }
        }
        public GenMaze.OutsideWallDeleteMode _outsideWallDeleteMode;
        public GenMaze.OutsideWallDeleteMode OutsideWallDeleteMode
        {
            get { return _outsideWallDeleteMode; }
        }
        private bool _makeFloorExit;
        public bool MakeFloorExit
        {
            get { return _makeFloorExit; }
        }
        private GenMaze.FloorExitType _exitType;
        public GenMaze.FloorExitType ExitType
        {
            get { return _exitType; }
        }
        private bool _makeRooms;
        public bool MakeRooms
        {
            get { return _makeRooms; }
        }
        private int _numberOfRooms;
        public int NumberOfRooms
        {
            get { return _numberOfRooms; }
        }
        private int _roomPlacementAttempts;
        public int RoomPlacementAttempts
        {
            get { return _roomPlacementAttempts; }
        }
        private bool _differentCorners;
        public bool DifferentCorners
        {
            get { return _differentCorners; }
        }
        private bool _differentEnds;
        public bool DifferentEnds
        {
            get { return _differentEnds; }
        }

        public MazeSettings(string mazeName, GenMaze.MazeDimension mazeDimension, int seedValue, int mazeWidth, int mazeLength, Vector3 mazePosition, float mazeTileWidthAndLength,
            float defaultWallHeight, float defaultFloorThickness, float wallZPlane, float floorZPlane, bool makeBraidMaze, float braidFrequency, bool makeFloorExit, GenMaze.FloorExitType exitType, 
            bool deleteOutsideWalls, int outsideWallPiecesToDelete, GenMaze.OutsideWallDeleteMode outsideWallDeleteMode, bool makeRooms, int numberOfRooms, int roomPlacementAttempts, bool differentCorners, bool differentEnds)
        {
            _mazeName = mazeName;
            _mazeDimension = mazeDimension;
            _seedValue = seedValue;
            _mazeWidth = mazeWidth;
            _mazeLength = mazeLength;
            _mazePosition = SerializeUnityThings.VectorConversion(mazePosition);
            _mazeTileWidthAndLength = mazeTileWidthAndLength;
            _defaultWallHeight = defaultWallHeight;
            _defaultFloorThickness = defaultFloorThickness;
            _wallZPlane = wallZPlane;
            _floorZPlane = floorZPlane;
            _makeBraidMaze = makeBraidMaze;
            _braidFrequency = braidFrequency;
            _makeFloorExit = makeFloorExit;
            _exitType = exitType;
            _deleteOutsideWalls = deleteOutsideWalls;
            _outsideWallPiecesToDelete = outsideWallPiecesToDelete;
            _outsideWallDeleteMode = outsideWallDeleteMode;
            _makeRooms = makeRooms;
            _numberOfRooms = numberOfRooms;
            _roomPlacementAttempts = roomPlacementAttempts;
            _differentCorners = differentCorners;
            _differentEnds = differentEnds;
        }
    }
}
