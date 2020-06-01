using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Linq;

namespace ProjectMaze
{
    [ExecuteInEditMode]
    //BLOCK BASED APPROACH
    public class GenMaze : MonoBehaviour
    {
        #region Tooltips + Variables
        public enum MazeDimension
        {
            ThreeDimensional
        }
        [Tooltip("The dimensional space the maze will be generated in")]
        public MazeDimension mazeDimension;
        [Tooltip("If set to true, you may enter a seed value to control the generation of the maze. Assuming the variables on this Maze Generator are identical, setting the seed value will result in the exact same maze being generated every time. If any of the settings have changed however (i.e. the width or length is different), then the maze will generate differently.")]
        public bool useSeedValue;
        [Tooltip("The seed value used to generate this maze. Entering the same seed value will result in the same placement of floor tiles, walls, and details.")]
        public int seedValue;
        [Tooltip("How many units wide is the maze?")]
        [GreaterThanInt(0, false)]
        public int mazeWidth;
        [Tooltip("How many units long is the maze?")]
        [GreaterThanInt(0, false)]
        public int mazeLength;
        [Tooltip("Where do you want the maze to be located? This will be the coordinates of the corner of the maze.")]
        public Vector3 mazePosition;
        [Tooltip("If checked, dead ends will be deleted in order to turn the maze into a braid maze (there is more than one path to any point in the maze).")]
        public bool makeBraidMaze;
        [Tooltip("Scale to determine how braid the maze will be \n1 = 100% fully braid. \n0 = Not braid.")]
        [Range(0f, 1f)]
        public float braidFrequency = 1f; //fully braid to begin with unless the user specifies otherwise

        public bool makeRooms;
        [Tooltip("How many rooms to attempt to place")]
        public int numberOfRooms;
        [Tooltip("How many times to attempt placing each room. If the algorithm attempts to place a room and it will intersect another one that counts as an attempt.")]
        [GreaterThanInt(1, true)]
        public int roomPlacementAttempts;
        [Tooltip("An array of the potential rooms that can be placed.")]
        public RoomPrefab[] rooms = new RoomPrefab[0];
        [Tooltip("The width and length of all wall pieces, floor pieces, and other tiles in the maze. Mazes without square tiles look bad, so I got rid of those options to simplify for prototype purpose.")]
        [GreaterThanFloat(0f, false)]
        public float mazeTileWidthAndLength;
        [Tooltip("The number of unique normal wall piece variations in your maze.\nPress the plus/minus buttons to adjust.")]
        public int numberOfWallPieces = 1;

        [Tooltip("The number of unique corner wall piece variations in your maze.\nPress the plus/minus buttons to adjust.")]
        public int numberOfCornerWallPieces = 1;
        [Tooltip("The number of unique end wall piece variations in your maze.\nPress the plus/minus buttons to adjust.")]
        public int numberOfEndWallPieces = 1;
        [Tooltip("Use a different set of pieces to place in corners.")]
        public bool differentCorners;
        [Tooltip("Use a different set of pieces to place at the ends of walls.")]
        public bool differentEnds;
        [Tooltip("The number of unique floor pieces in your maze.\nPress the plus/minus buttons to adjust.")]
        public int numberOfFloorPieces = 1;

        [Tooltip("What can possibly be placed at the exit of the maze. If you're generating an exit and leave this null, an empty tile will be left here as a sort of floor exit. Your imagination is the limit - triggers, an elevator, whatever you can fit into a prefab that will take up the same space as your other tiles will do the trick.")]
        public GameObject[] exitPieces = new GameObject[1];
        [Tooltip("What can possibly be placed at the start of the maze. If you're generating an start and leave this null, an empty tile will be left here. Your imagination is the limit - triggers, an elevator, whatever you can fit into a prefab that will take up the same space as your other tiles will do the trick.")]
        public GameObject[] startPieces = new GameObject[1];

        [Tooltip("If checked, you will be able to populate your maze with details.")]
        public bool addDetails = false;
        [Tooltip("All of the details to be placed along the walls (randomly distributed).")]
        public WallDetail[] wallDetails = new WallDetail[1];
        [Tooltip("All of the details to be placed throughout the maze (randomly distributed and rotated around the y-axis).")]
        public OtherDetail[] otherDetails = new OtherDetail[1];
        [Tooltip("The name of the maze, will be given to the GameObject created in the scene view. When generating or loading, if there is already a maze with this name in the scene, the new maze will replace it. If there is not a maze with this name in the scene, a new one will be instantiated.")]
        public string mazeName;
        [Tooltip("Enabling this will allow you to use Unity's built-in undo functions on maze generation in the editor. The drawback is that it slows down generation in the editor A LOT. It also causes memory issues after generating a number of large mazes since Unity's undo feature is extremely RAM hungry when it comes to a lot of instantiation and destruction.")]
        public bool allowEditorUndo;

        #region 3D Specific Variables
        [Tooltip("The default height of your wall pieces. Note that it is assumed your pivot point is in the center of the model in the algorithm calculations.")]
        [GreaterThanFloat(0f, false)]
        public float defaultWallHeight;
        [Tooltip("The default thickness of your floor pieces. Note that it is assumed your pivot point is in the center of the model in the algorithm calculations.")]
        [GreaterThanFloat(0f, false)]
        public float defaultFloorThickness;
        [Tooltip("All of the possible wall pieces to be placed (randomly distributed).")]
        public WallPiece3D[] wallPieces = new WallPiece3D[1];
        [Tooltip("All of the possible corner wall pieces to be placed (randomly distributed).")]
        public WallPiece3D[] cornerWallPieces = new WallPiece3D[1];
        [Tooltip("All of the possible end wall pieces to be placed (randomly distributed).")]
        public WallPiece3D[] endWallPieces = new WallPiece3D[1];
        [Tooltip("All of the possible floor pieces to be placed (randomly distributed). Note that they must all have the same dimensions you entered above.")]
        public FloorPiece[] floorPieces = new FloorPiece[1];
        #endregion

        public GameObject theMaze; //the root GameObject of the most current maze being generated in the editor (will have a Maze component once generated)

        public MazeMap latestMazeMap;//the map of the maze that was most recently generated or loaded in the editor

        private Dictionary<Vector2, UniqueTile> uniqueTileDict; //used to keep track of where each Unique Tile goes

        //used during generation to test a specific direction to generate into
        internal enum TestDirection
        {
            left = 1,
            up,
            right,
            down
        }

        public const string MAZE_SETTINGS_EXTENSION = "MazeSettings";
        public const string DEFAULT_SETTINGS_DIRECTORY = "Assets/Settings/SavedSettings"; //WILL BE USED TO SAVE/LOAD MAZE THROUGH A FILE
        #region Custom Inspector Foldouts
        //the following varibles are just used to store the state of foldouts in the custom inspector ---- Try to find a cleaner way to do this?
        [HideInInspector]
        public bool showFloorPieces = false;
        [HideInInspector]
        public bool showExitPieces = false;
        [HideInInspector]
        public bool showStartPieces = false;
        [HideInInspector]
        public bool showWallPieces = false;
        [HideInInspector]
        public bool showCornerWallPieces = false;
        [HideInInspector]
        public bool showEndWallPieces = false;
        [HideInInspector]
        public bool showDetails = false;
        #endregion

        #region Initialize & Basic Checks
        void Awake() {

            #region Validating Prefab Arrays
            //Default 3D Walls Start
            if (wallPieces == null || wallPieces.Length == 0) {
                wallPieces = new WallPiece3D[1];
                wallPieces[0] = new WallPiece3D(null, defaultWallHeight);
            }

            if (cornerWallPieces == null || cornerWallPieces.Length == 0) {
                cornerWallPieces = new WallPiece3D[1];
                cornerWallPieces[0] = new WallPiece3D(null, defaultWallHeight);
            }

            if (endWallPieces == null || endWallPieces.Length == 0) {
                endWallPieces = new WallPiece3D[1];
                endWallPieces[0] = new WallPiece3D(null, defaultWallHeight);
            }
            #endregion

            //makes sure that we're not under the minimum number of wall/floor pieces
            if (numberOfWallPieces < 1) {
                numberOfWallPieces = 1;
            }

            if (numberOfFloorPieces < 1) {
                numberOfFloorPieces = 1;
            }

            uniqueTileDict = new Dictionary<Vector2, UniqueTile>();

            if (!Directory.Exists(DEFAULT_SETTINGS_DIRECTORY)) {
                Directory.CreateDirectory(DEFAULT_SETTINGS_DIRECTORY);
            }
        }
        #endregion

        #region EditorChecks
        void OnValidate() {
            //ensures maze width and height will not be too small or even value (custom maze generation algorithm has to have odd value to work correctly)
            if (mazeWidth < 3) {
                mazeWidth = 3;
            }
            if (mazeLength < 3) {
                mazeLength = 3;
            }
            if (mazeWidth % 2 != 1) {
                mazeWidth++;
            }
            if (mazeLength % 2 != 1) {
                mazeLength++;
            }

            if (rooms == null) {
                rooms = new RoomPrefab[0];
            }

            //sets a default name to generated maze if the user doesn't set one
            if (string.IsNullOrEmpty(mazeName)) {
                mazeName = "DefaultName";
            }
            //if we're going to place unique tiles, better make sure we have a dictionary for it ------- Never implemented due to time
            //if (makeUniqueTiles) {
            //    if (uniqueTileDict == null) {
            //        uniqueTileDict = new Dictionary<Vector2, UniqueTile>();
            //    }
            //}

            if (addDetails) {
                //clamps wall detail positions to a specific face of wall
                foreach (WallDetail detail in wallDetails) {
                    if (detail.detailPrefab) {
                        //Name string check
                        if (string.IsNullOrEmpty(detail.detailName)) {
                            detail.detailName = detail.detailPrefab.name;
                        }

                        //horizontal clamping
                        if (detail.minHorizontalOffset < -mazeTileWidthAndLength / 2 + detail.width / 2) {
                            detail.minHorizontalOffset = -mazeTileWidthAndLength / 2 + detail.width / 2;
                        }
                        if (detail.maxHorizontalOffset < -mazeTileWidthAndLength / 2 + detail.width / 2) {
                            detail.maxHorizontalOffset = -mazeTileWidthAndLength / 2 + detail.width / 2;
                        }
                        if (detail.minHorizontalOffset > mazeTileWidthAndLength / 2 - detail.width / 2) {
                            detail.minHorizontalOffset = mazeTileWidthAndLength / 2 - detail.width / 2;
                        }
                        if (detail.maxHorizontalOffset > mazeTileWidthAndLength / 2 - detail.width / 2) {
                            detail.maxHorizontalOffset = mazeTileWidthAndLength / 2 - detail.width / 2;
                        }
                        if (detail.maxHorizontalOffset < detail.minHorizontalOffset) {
                            detail.maxHorizontalOffset = detail.minHorizontalOffset;
                        }

                        //vertical clamping
                        if (detail.minHeight < detail.height / 2) {
                            detail.minHeight = detail.height / 2;
                        }
                        if (detail.maxHeight > defaultWallHeight - detail.height / 2) {
                            detail.maxHeight = defaultWallHeight - detail.height / 2;
                        }
                        if (detail.minHeight > defaultWallHeight - detail.height / 2) {
                            detail.minHeight = defaultWallHeight - detail.height / 2;
                        }
                        if (detail.maxHeight < detail.height / 2) {
                            detail.maxHeight = detail.height / 2;
                        }
                        if (detail.maxHeight < detail.minHeight) {
                            detail.maxHeight = detail.minHeight;
                        }
                    }
                }

                //clamps Other Details to a specific tile-space
                foreach (OtherDetail detail in otherDetails) {
                    if (detail.detailPrefab) {
                        //Name string Check
                        if (string.IsNullOrEmpty(detail.detailName)) {
                            detail.detailName = detail.detailPrefab.name;
                        }

                        //vertical clamping
                        if (detail.minHeight < detail.height / 2) {
                            detail.minHeight = detail.height / 2;
                        }
                        if (detail.maxHeight < detail.minHeight) {
                            detail.maxHeight = detail.minHeight;
                        }

                        //x-axis clamping
                        if (detail.minXOffset < -mazeTileWidthAndLength / 2 + detail.width / 2) {
                            detail.minXOffset = -mazeTileWidthAndLength / 2 + detail.width / 2;
                        }
                        if (detail.maxXOffset < -mazeTileWidthAndLength / 2 + detail.width / 2) {
                            detail.maxXOffset = -mazeTileWidthAndLength / 2 + detail.width / 2;
                        }
                        if (detail.minXOffset > mazeTileWidthAndLength / 2 - detail.width / 2) {
                            detail.minXOffset = mazeTileWidthAndLength / 2 - detail.width / 2;
                        }
                        if (detail.maxXOffset > mazeTileWidthAndLength / 2 - detail.width / 2) {
                            detail.maxXOffset = mazeTileWidthAndLength / 2 - detail.width / 2;
                        }
                        if (detail.maxXOffset < detail.minXOffset) {
                            detail.maxXOffset = detail.minXOffset;
                        }

                        //z-axis clamping
                        if (detail.minZOffset < -mazeTileWidthAndLength / 2 + detail.width / 2) {
                            detail.minZOffset = -mazeTileWidthAndLength / 2 + detail.width / 2;
                        }
                        if (detail.maxZOffset < -mazeTileWidthAndLength / 2 + detail.width / 2) {
                            detail.maxZOffset = -mazeTileWidthAndLength / 2 + detail.width / 2;
                        }
                        if (detail.minZOffset > mazeTileWidthAndLength / 2 - detail.width / 2) {
                            detail.minZOffset = mazeTileWidthAndLength / 2 - detail.width / 2;
                        }
                        if (detail.maxZOffset > mazeTileWidthAndLength / 2 - detail.width / 2) {
                            detail.maxZOffset = mazeTileWidthAndLength / 2 - detail.width / 2;
                        }
                        if (detail.maxZOffset < detail.minZOffset) {
                            detail.maxZOffset = detail.minZOffset;
                        }
                    }
                }
            }
        }


        #endregion
        #endregion

        #region Generate Maze - Single Frame
        /// <summary>
        /// Generates a new maze
        /// </summary>
        /// <returns>
        /// Returns the root GameObject of the generated maze which will have the Maze component
        /// </returns>
        public GameObject GenerateNewMaze() {
            System.Diagnostics.Stopwatch st = new System.Diagnostics.Stopwatch();
            st.Start();
            int rowTracker;
            int columnTracker;
            int indexTracker;
            bool keepGoing = true; //used to indicate completeness of each function

            StartMazeMap(); //starts the maze map at a blank slate and deletes current one if there is one existing

            Vector2 currentTileCoordinates = new Vector2(1, 1); //sets the current tile to the bottom-left corner

            //if we're not using a pre-set seed value, it needs to be set now
            if (!useSeedValue) {
                seedValue = Random.Range(int.MinValue, int.MaxValue);
            }
            Random.InitState(seedValue); //this is what keeps the generation the same if we use a set seed value
            int i = 0;
            do {
                keepGoing = GetNextTile(latestMazeMap, ref currentTileCoordinates); //generates maze, single tile at a time using random generator and sets every tile a state for all post generation functionality
                i++;
            } while (keepGoing);

            if (makeBraidMaze && braidFrequency > 0f) {
                keepGoing = true;
                rowTracker = 0;
                columnTracker = 0;

                do {
                    keepGoing = EraseSomeDeadEnds(latestMazeMap, 10000, ref rowTracker, ref columnTracker); //erases dead ends
                } while (keepGoing);
            }

            if (Application.isPlaying) {
                CheckDuplicateMazeNames(mazeName);
            }
            theMaze = new GameObject(mazeName, typeof(Maze)); //either we've checked for duplicate names here or in editor code, so now is a safe time to create the new GameObject
            theMaze.transform.position = mazePosition; //since we're generating a new maze, better move it to the right spot
            //MAZE IS DONE

            //Post Generation below
            if (makeRooms && rooms.Length > 0) {
                for (int counter = 0; counter < numberOfRooms; counter++) {
                    bool validAvailableChoice = false;
                    foreach (RoomPrefab room in rooms) {
                        if (room) {
                            if (!room.fixedPosition || !room.placedOnce) {
                                validAvailableChoice = true;
                                break;
                            }
                        }
                    }
                    if (validAvailableChoice) {
                        RoomPrefab chosenRoom = rooms[Random.Range(0, rooms.Length)];
                        while (chosenRoom.fixedPosition && chosenRoom.placedOnce) {
                            chosenRoom = rooms[Random.Range(0, rooms.Length)];
                        }
                        keepGoing = true;
                        int timesTested = 0;
                        do {
                            keepGoing = SetRoom(latestMazeMap, chosenRoom, mazeLength, mazeWidth);
                            timesTested++;
                        } while (keepGoing && timesTested < roomPlacementAttempts);
                    }
                }
                foreach (RoomPrefab room in rooms) {
                    room.placedOnce = false;
                }
            }

            if (differentCorners) {
                rowTracker = 0;
                columnTracker = 0;
                keepGoing = true;
                do {
                    keepGoing = FindCornerWalls(latestMazeMap, -1, ref rowTracker, ref columnTracker);
                } while (keepGoing);
            }

            if (differentEnds) {
                rowTracker = 0;
                columnTracker = 0;
                keepGoing = true;
                do {
                    keepGoing = FindEndWalls(latestMazeMap, -1, ref rowTracker, ref columnTracker);
                } while (keepGoing);
            }

            rowTracker = 0;
            columnTracker = 0;
            keepGoing = true;
            MazeNode[,] nodes = new MazeNode[mazeWidth, mazeLength];
            do
            {
                keepGoing = InstantiateMaze3D(latestMazeMap, theMaze.transform, wallPieces, cornerWallPieces,
                    endWallPieces, floorPieces, exitPieces, startPieces, mazeTileWidthAndLength, defaultFloorThickness,
                    -1, ref rowTracker, ref columnTracker, nodes);
            } while (keepGoing);

            if (addDetails) {
                
                    rowTracker = 0;
                    columnTracker = 0;
                    indexTracker = 0;
                    keepGoing = true;
                    do {
                        keepGoing = PlaceRandomWallDetails(latestMazeMap, wallDetails, mazeTileWidthAndLength, theMaze.transform, -1, ref indexTracker, ref rowTracker, ref columnTracker); //this adds details randomly throughout the maze (attached to the walls) based on the settings in wallDetails
                    } while (keepGoing);

                    rowTracker = 0;
                    columnTracker = 0;
                    indexTracker = 0;
                    keepGoing = true;
                    do {
                        keepGoing = PlaceRandomOtherDetails(latestMazeMap, otherDetails, mazeTileWidthAndLength, theMaze.transform, -1, ref indexTracker, ref rowTracker, ref columnTracker); //this adds details randomly throughout the maze based on the settings in otherDetails
                    } while (keepGoing);
                
            }

            theMaze.GetComponent<Maze>().mazeMap = latestMazeMap; //stores the maze information in a mazeMap located on the maze itself
            st.Stop();
            Debug.Log(string.Format("Finished: {0}ms", st.ElapsedMilliseconds));
            return theMaze;
        }

        #endregion

        #region Clear Stage and Start New Maze Map
        /// <summary>
        /// Clears the current map and starts a new one
        /// </summary>
        public void StartMazeMap() {

            latestMazeMap = new MazeMap {
                //sets the width and height properties of the maze map to make sure they're current
                mazeWidth = mazeWidth,
                mazeLength = mazeLength
            };
            latestMazeMap.map = new MazeMap.TileState[(latestMazeMap.mazeWidth) * (latestMazeMap.mazeLength)]; //creates a new map
            for (int row = 0; row < latestMazeMap.mazeLength; row++) {
                for (int column = 0; column < latestMazeMap.mazeWidth; column++) {
                    if (row % 2 == 0 || column % 2 == 0) {
                        latestMazeMap.map[(row * latestMazeMap.mazeWidth) + column] = MazeMap.TileState.wall; //sets walls on every second square (looks like a grid)
                    } else {
                        latestMazeMap.map[(row * latestMazeMap.mazeWidth) + column] = MazeMap.TileState.unexplored; //flags every empty space as unexplored
                    }
                }
            }
        }
        #endregion

        #region Find Valid Tile - This creates the maze
        /// <summary>
        /// Finds a new, valid, tile to look at. Generates a maze one tile at a time by carving paths
        /// </summary>
        /// <returns>True if not finished, and false if the maze is finished generating</returns>
        private bool GetNextTile(MazeMap mazeMapToCheck, ref Vector2 currentTileCoordinates) {
            //declares and initializes the values to meaningless states
            bool foundValidTile = false; //have we found a valid tile yet?
            bool checkingDeadEnds = false; //changes how a second look at surrounding tiles is processed (i.e. backtracking along path)
            MazeMap.TileState testResults = MazeMap.TileState.outOfBounds; //the results of looking in the above direction
            Vector2 move = Vector2.zero; //if we've found a valid tile, the move we have to make to get there
            bool keepGoing = true; //assume that the generation process hasn't finished
            testResults = MazeMap.TileState.outOfBounds; //default the results to out of bounds

            //generate list of directions and shuffle it for more efficient random directions
            List<TestDirection> testDirections = new List<TestDirection> { TestDirection.left, TestDirection.up, TestDirection.right, TestDirection.down };
            for (int index = 0; index < testDirections.Count; index++) {
                int newIndex = Random.Range(0, 4);
                TestDirection temp = testDirections[newIndex];
                testDirections[newIndex] = testDirections[index];
                testDirections[index] = temp;
            }

            for (int directionIndex = 0; directionIndex < testDirections.Count && !foundValidTile; directionIndex++) {
                //choose offset from current tile based on given direction
                //when backtracking, we have to go one space at a time, not jump by twos
                switch (testDirections[directionIndex]) {
                    case TestDirection.left: //one is left
                        move = checkingDeadEnds ? new Vector2(-1, 0) : new Vector2(-2, 0);
                        break;

                    case TestDirection.up: //two is up
                        move = checkingDeadEnds ? new Vector2(0, 1) : new Vector2(0, 2);
                        break;

                    case TestDirection.right: //three is right
                        move = checkingDeadEnds ? new Vector2(1, 0) : new Vector2(2, 0);
                        break;

                    case TestDirection.down: //four is down
                        move = checkingDeadEnds ? new Vector2(0, -1) : new Vector2(0, -2);
                        break;
                }
                Debug.Log(testDirections.Count);
                testResults = CheckTile(move, mazeMapToCheck, currentTileCoordinates); //check the state of the tile in the given direction
                //the first round of checks needs to find a completely new, unexplored tile to make sure we don't have any "dead zones" in the maze that don't get developed
                if (!checkingDeadEnds) {
                    //if we've found an unexplored tile, we're done and can break down the wall between here and there as well as marking this tile as visited
                    if (testResults == MazeMap.TileState.unexplored) {
                        foundValidTile = true;
                        mazeMapToCheck.map[((int)(currentTileCoordinates.y) * mazeMapToCheck.mazeWidth) + (int)(currentTileCoordinates.x)] = MazeMap.TileState.visitedOnce; //flags the current tile as visited
                        mazeMapToCheck.map[((int)(currentTileCoordinates.y + move.y / 2) * mazeMapToCheck.mazeWidth) + (int)(currentTileCoordinates.x + move.x / 2)] = MazeMap.TileState.brokenWall; //flags the wall you moved through as broken down
                        currentTileCoordinates += move; //moves the "pointer" to the new spot
                    }

                    //if we didn't find any unexplored tiles in any direction, we can start the backtracking process
                    if (directionIndex == testDirections.Count - 1 && !foundValidTile) {
                        checkingDeadEnds = true;
                        directionIndex = -1;
                    }
                }
                //the second round of checks just needs to find a tile that's only been visited once, or a broken wall (backtracking)
                else {
                    if (testResults == MazeMap.TileState.visitedOnce || testResults == MazeMap.TileState.brokenWall) {
                        foundValidTile = true;
                        if (mazeMapToCheck.map[((int)(currentTileCoordinates.y) * mazeMapToCheck.mazeWidth) + (int)(currentTileCoordinates.x)] == MazeMap.TileState.unexplored) {
                            mazeMapToCheck.map[((int)(currentTileCoordinates.y) * mazeMapToCheck.mazeWidth) + (int)(currentTileCoordinates.x)] = MazeMap.TileState.deadEnd; //marks our dead ends in case we want to make a braid maze
                        } else {
                            mazeMapToCheck.map[((int)(currentTileCoordinates.y) * mazeMapToCheck.mazeWidth) + (int)(currentTileCoordinates.x)] = MazeMap.TileState.visitedTwice; //marks tiles we've visited twice so that we don't go back to them
                        }
                        currentTileCoordinates += move;
                    }

                    //if we've checked every direction and there aren't any valid tiles, the generation process has finished. We backtracked all the way to the start ((1,1) by default)
                    if (directionIndex == testDirections.Count - 1 && !foundValidTile) {
                        mazeMapToCheck.map[((int)(currentTileCoordinates.y) * mazeMapToCheck.mazeWidth) + (int)(currentTileCoordinates.x)] = MazeMap.TileState.deadEnd; //mark as dead end for braiding as well
                        keepGoing = false;
                    }
                }
            }
            return keepGoing;
        }
        #region Helper - Tile out of bounds?
        /// <summary>
        /// Checks the tile in the specified direction for it's state
        /// </summary>
        /// <returns>The state of the tile</returns>
        /// <param name="move">The vector 2 representing the place to check in relation to the current position</param>
        private MazeMap.TileState CheckTile(Vector2 move, MazeMap mapToCheck, Vector2 currentTileCoordinates) {
            //if we've moved out of the bounds of the maze, return out of bounds, otherwise return the tile information
            if (currentTileCoordinates.x + move.x < 0 || currentTileCoordinates.y + move.y < 0) {
                return MazeMap.TileState.outOfBounds;
            } else if (currentTileCoordinates.x + move.x >= mapToCheck.mazeWidth || currentTileCoordinates.y + move.y >= mapToCheck.mazeLength) {
                return MazeMap.TileState.outOfBounds;
            } else {
                return mapToCheck.map[((int)(currentTileCoordinates.y + move.y) * mapToCheck.mazeWidth) + (int)(currentTileCoordinates.x + move.x)];
            }
        }
        #endregion
        #endregion

        #region Instantiation
        /// <summary>
        /// Instantiates all basic parts of the 3D maze (floor/walls)
        /// </summary>
        /// <param name="mazeMap">The map to read from for instantiation</param>
        /// <param name="mazeTransform">The parent the pieces will be tied to</param>
        /// <param name="walls">The pool of wall pieces to pull from</param>
        /// <param name="cornerWalls">The pool of wall pieces to pull from for corners</param>
        /// <param name="endWalls">The pool of wall pieces to pull from for ends</param>
        /// <param name="floors">The pool of floor pieces to pull from</param>
        /// <param name="exits">The pool of exits to pull from</param>
        /// <param name="tileWidthAndLength">The width/length of every tile in the maze</param>
        /// <param name="defaultFloorThickness">The default thickness of floor pieces</param>
        /// <param name="instantiationsPerFrame">The number of instantiations to perform every frame. A value of 0 or less will result the entire maze being instantiated in one frame.</param>
        /// <param name="row">The current row of the map we're reading</param>
        /// <param name="column">The current column of the map we're reading</param>
        /// <param name="mazeNodes">An empty 2D array of MazeNode objects - used to persistently keep track of nodes for linkage.</param>
        /// <returns>Returns true if it should be called again, returns false if it has finished</returns>
        private bool InstantiateMaze3D(MazeMap mazeMap, Transform mazeTransform, WallPiece3D[] walls, WallPiece3D[] cornerWalls, WallPiece3D[] endWalls, FloorPiece[] floors, GameObject[] exits, GameObject[] starts, float tileWidthAndLength, float defaultFloorThickness, int instantiationsPerFrame, ref int row, ref int column, MazeNode[,] mazeNodes) {
            int instantiationCounter = 0;
            bool keepGoing = true;
            for (; row < mazeMap.mazeLength && (instantiationCounter < instantiationsPerFrame || instantiationsPerFrame < 1); row++) {
                for (; column < mazeMap.mazeWidth && (instantiationCounter < instantiationsPerFrame || instantiationsPerFrame < 1); column++) {
                    int randomGenerator;
                    GameObject tempHolder;
                    switch (mazeMap.map[(row * mazeMap.mazeWidth) + column]) {
                        case MazeMap.TileState.wall:
                            do {
                                randomGenerator = Random.Range(0, walls.Length);
                            } while (!walls[randomGenerator].wallPrefab);
                            tempHolder = Instantiate(walls[randomGenerator].wallPrefab, new Vector3(column * tileWidthAndLength, walls[randomGenerator].wallHeight / 2f, row * tileWidthAndLength) + mazeTransform.position, mazeTransform.rotation);
                            tempHolder.transform.parent = mazeTransform;
                            goto default;

                        case MazeMap.TileState.cornerWall:
                            do {
                                randomGenerator = Random.Range(0, cornerWalls.Length);
                            } while (!cornerWalls[randomGenerator].wallPrefab);
                            tempHolder = Instantiate(cornerWalls[randomGenerator].wallPrefab, new Vector3(column * tileWidthAndLength, cornerWalls[randomGenerator].wallHeight / 2f, row * tileWidthAndLength) + mazeTransform.position, mazeTransform.rotation);
                            tempHolder.transform.parent = mazeTransform;
                            goto default;

                        case MazeMap.TileState.endWallUp:
                            do {
                                randomGenerator = Random.Range(0, endWalls.Length);
                            } while (!endWalls[randomGenerator].wallPrefab);
                            tempHolder = Instantiate(endWalls[randomGenerator].wallPrefab, new Vector3(column * tileWidthAndLength, endWalls[randomGenerator].wallHeight / 2f, row * tileWidthAndLength) + mazeTransform.position, mazeTransform.rotation);
                            tempHolder.transform.parent = mazeTransform;
                            goto default;

                        case MazeMap.TileState.endWallRight:
                            do {
                                randomGenerator = Random.Range(0, endWalls.Length);
                            } while (!endWalls[randomGenerator].wallPrefab);
                            tempHolder = Instantiate(endWalls[randomGenerator].wallPrefab, new Vector3(column * tileWidthAndLength, endWalls[randomGenerator].wallHeight / 2f, row * tileWidthAndLength) + mazeTransform.position, mazeTransform.rotation);
                            tempHolder.transform.Rotate(transform.up, 90f);
                            tempHolder.transform.parent = mazeTransform;
                            goto default;

                        case MazeMap.TileState.endWallDown:
                            do {
                                randomGenerator = Random.Range(0, endWalls.Length);
                            } while (!endWalls[randomGenerator].wallPrefab);
                            tempHolder = Instantiate(endWalls[randomGenerator].wallPrefab, new Vector3(column * tileWidthAndLength, endWalls[randomGenerator].wallHeight / 2f, row * tileWidthAndLength) + mazeTransform.position, mazeTransform.rotation);
                            tempHolder.transform.Rotate(transform.up, 180f);
                            tempHolder.transform.parent = mazeTransform;
                            goto default;

                        case MazeMap.TileState.endWallLeft:
                            do {
                                randomGenerator = Random.Range(0, endWalls.Length);
                            } while (!endWalls[randomGenerator].wallPrefab);
                            tempHolder = Instantiate(endWalls[randomGenerator].wallPrefab, new Vector3(column * tileWidthAndLength, endWalls[randomGenerator].wallHeight / 2f, row * tileWidthAndLength) + mazeTransform.position, mazeTransform.rotation);
                            tempHolder.transform.Rotate(transform.up, 270f);
                            tempHolder.transform.parent = mazeTransform;
                            goto default;

                        case MazeMap.TileState.finish:
                            randomGenerator = Random.Range(0, exits.Length);
                            if (exits[randomGenerator]) {
                                tempHolder = Instantiate(exits[randomGenerator], new Vector3(column * tileWidthAndLength, -(defaultFloorThickness / 2f), row * tileWidthAndLength) + mazeTransform.position, mazeTransform.rotation);
                                tempHolder.transform.parent = mazeTransform;
                            }
                            instantiationCounter++;
                            break;

                        case MazeMap.TileState.start:
                            randomGenerator = Random.Range(0, starts.Length);
                            if (starts[randomGenerator]) {
                                tempHolder = Instantiate(starts[randomGenerator], new Vector3(column * tileWidthAndLength, -(defaultFloorThickness / 2f), row * tileWidthAndLength) + mazeTransform.position, mazeTransform.rotation);
                                tempHolder.transform.parent = mazeTransform;
                            }
                            instantiationCounter++;
                            break;

                        case MazeMap.TileState.unique:
                            UniqueTile thisTile = uniqueTileDict[new Vector2(column, row)];
                            randomGenerator = Random.Range(0, thisTile.tileVariations.Length);
                            if (thisTile.tileVariations[randomGenerator]) {
                                tempHolder = Instantiate(thisTile.tileVariations[randomGenerator], new Vector3(column * tileWidthAndLength, -(defaultFloorThickness / 2f), row * tileWidthAndLength) + mazeTransform.position, mazeTransform.rotation);
                                tempHolder.transform.parent = mazeTransform;
                                tempHolder.gameObject.name = string.IsNullOrEmpty(thisTile.uniqueTileName) ? tempHolder.gameObject.name : thisTile.uniqueTileName;
                            }
                            instantiationCounter++;
                            break;

                        default:
                            do {
                                randomGenerator = Random.Range(0, floors.Length);
                            } while (!floors[randomGenerator].floorPrefab);
                            tempHolder = Instantiate(floors[randomGenerator].floorPrefab, new Vector3(column * tileWidthAndLength, -(floors[randomGenerator].floorThickness / 2f), row * tileWidthAndLength) + mazeTransform.position, mazeTransform.rotation);
                            tempHolder.transform.parent = mazeTransform;
                            instantiationCounter++;
                            break;
                    }
                }
                if (instantiationCounter >= instantiationsPerFrame && instantiationsPerFrame > 0) {
                    column--;
                } else if (column >= mazeMap.mazeWidth) {
                    column = 0;
                }
            }
            if (instantiationCounter >= instantiationsPerFrame && instantiationsPerFrame > 0) {
                row--;
            } else if (row >= mazeMap.mazeLength) {
                keepGoing = false;
            }
            return keepGoing;
        }
        #endregion

        #region Delete DeadEnds
        /// <summary>
        /// Erases some dead ends if you want a braid maze. Does so over a number of frames influenced by the provided calculationsPerFrame variable
        /// </summary>
        /// <param name="mapToModify">The MazeMap being edited to remove dead ends</param>
        /// <param name="calculationsPerFrame">The number of calculations we can perform per frame. A value of 0 or lower means to continue until finished.</param>
        /// <param name="row">Passed by reference to keep track of position in looping through 2D array</param>
        /// <param name="column">Passed by reference to keep track of position in looping through 2D array</param>
        /// <returns>Returns true if it should be called again, returns false if it has finished</returns>
        private bool EraseSomeDeadEnds(MazeMap mapToModify, int calculationsPerFrame, ref int row, ref int column) {
            MazeMap.TileState floorResults = MazeMap.TileState.outOfBounds;
            MazeMap.TileState wallResults = MazeMap.TileState.outOfBounds;
            Vector2 move = Vector2.zero;
            bool keepGoing = true;


            for (; row < mapToModify.mazeLength; row++) {
                for (; column < mapToModify.mazeWidth; column++) {
                    if (mapToModify.map[(row * mapToModify.mazeWidth) + column] == MazeMap.TileState.deadEnd) {  //looks through the maze for all dead ends flagged during the generation process
                        Vector2 currentTileCoordinates = new Vector2(column, row); //sets the current tile variable to the current tile (the dead end that's been found)

                        TestDirection testDirection = (TestDirection)1; //start testing at the lowest value (1 is left)
                        bool foundIt = false; //used to indicate when the wall that needs to be broken down has been found
                        while ((int)testDirection <= 4 && !foundIt) {
                            switch (testDirection) {
                                case TestDirection.left: //one is left
                                    move = new Vector2(-1, 0);
                                    break;

                                case TestDirection.up: //two is up
                                    move = new Vector2(0, 1);
                                    break;

                                case TestDirection.right: //three is right
                                    move = new Vector2(1, 0);
                                    break;

                                case TestDirection.down: //four is down
                                    move = new Vector2(0, -1);
                                    break;
                            }
                            wallResults = CheckTile(move, mapToModify, currentTileCoordinates); //checks if there is a wall in that direction
                            floorResults = CheckTile(move * 2, mapToModify, currentTileCoordinates); //checks if the tile after is a floor (ensures passage if we break the wall)

                            if (IsFloor(floorResults) && IsWall(wallResults)) {
                                if (Random.Range(0f, 1f) <= braidFrequency) {//random picker determines how braid the maze will be
                                    foundIt = true;
                                    mapToModify.map[((int)(currentTileCoordinates.y + move.y) * mapToModify.mazeWidth) + (int)(currentTileCoordinates.x + move.x)] = MazeMap.TileState.brokenWall;
                                }
                            }
                            testDirection++;
                        }
                    }
                } 
                if (column >= mapToModify.mazeWidth) {
                    column = 0;
                }
            } 
            if (row >= mapToModify.mazeLength) {
                keepGoing = false;
            }
            return keepGoing;
        }
        #endregion

        #region Find Corners/Ends
        /// <summary>
        /// Finds all wall pieces that are at the junctions of wall segments and labels them as such.
        /// </summary>
        /// <param name="mapToModify">The maze map we're modifying.</param>
        /// <param name="calculationsPerFrame">The number of calculations we can perform per frame. A value of 0 or lower means to continue until finished.</param>
        /// <param name="row">Passed by reference to keep track of position in looping through 2D array</param>
        /// <param name="column">Passed by reference to keep track of position in looping through 2D array</param>
        /// <returns>Returns true if it should be called again, returns false if it has finished</returns>
        private bool FindCornerWalls(MazeMap mapToModify, int calculationsPerFrame, ref int row, ref int column) {
            bool keepGoing = true;
            int calculationCounter = 0;

            //looks through the maze for all dead ends flagged during the generation process
            for (; row < mapToModify.mazeLength && (calculationCounter < calculationsPerFrame || calculationsPerFrame < 1); row++) {
                for (; column < mapToModify.mazeWidth && (calculationCounter < calculationsPerFrame || calculationsPerFrame < 1); column++) {
                    if (mapToModify.map[(row * mapToModify.mazeWidth) + column] == MazeMap.TileState.wall) {
                        Vector2 currentTileCoordinates = new Vector2(column, row);
                        // storing state of all surrounding tiles
                        MazeMap.TileState left = CheckTile(new Vector2(-1, 0), mapToModify, currentTileCoordinates);
                        MazeMap.TileState right = CheckTile(new Vector2(1, 0), mapToModify, currentTileCoordinates);
                        MazeMap.TileState up = CheckTile(new Vector2(0, 1), mapToModify, currentTileCoordinates);
                        MazeMap.TileState down = CheckTile(new Vector2(0, -1), mapToModify, currentTileCoordinates);
                        // any tile that has a left or right neighbour AND an up or down neighbour is considered a corner wall
                        // if it does not have left and right OR up and down neighbour (which would be a t-intersection or cross-intersection)
                        if ((IsWall(left) ^ IsWall(right)) && (IsWall(up) ^ IsWall(down))) {
                            mapToModify.map[(row * mapToModify.mazeWidth) + column] = MazeMap.TileState.cornerWall;
                        }
                    }
                    calculationCounter++;
                }
                if (calculationCounter >= calculationsPerFrame && calculationsPerFrame > 0) {
                    column--;
                } else if (column >= mapToModify.mazeWidth) {
                    column = 0;
                }
            }
            if (calculationCounter >= calculationsPerFrame && calculationsPerFrame > 0) {
                row--;
            } else if (row >= mapToModify.mazeLength) {
                keepGoing = false;
            }
            return keepGoing;
        }

        /// <summary>
        /// Finds all wall pieces that are at the ends of wall segments and labels them as such.
        /// </summary>
        /// <param name="mapToModify">The maze map we're modifying.</param>
        /// <param name="calculationsPerFrame">The number of calculations we can perform per frame. A value of 0 or lower means to continue until finished.</param>
        /// <param name="row">Passed by reference to keep track of position in looping through 2D array</param>
        /// <param name="column">Passed by reference to keep track of position in looping through 2D array</param>
        /// <returns>Returns true if it should be called again, returns false if it has finished</returns>
        private bool FindEndWalls(MazeMap mapToModify, int calculationsPerFrame, ref int row, ref int column) {
            bool keepGoing = true;
            int calculationCounter = 0;

            //looks through the maze for all dead ends flagged during the generation process
            for (; row < mapToModify.mazeLength && (calculationCounter < calculationsPerFrame || calculationsPerFrame < 1); row++) {
                for (; column < mapToModify.mazeWidth && (calculationCounter < calculationsPerFrame || calculationsPerFrame < 1); column++) {
                    if (mapToModify.map[(row * mapToModify.mazeWidth) + column] == MazeMap.TileState.wall) {
                        Vector2 currentTileCoordinates = new Vector2(column, row);
                        //storing state of all surrounding tiles
                        MazeMap.TileState left = CheckTile(new Vector2(-1, 0), mapToModify, currentTileCoordinates);
                        MazeMap.TileState right = CheckTile(new Vector2(1, 0), mapToModify, currentTileCoordinates);
                        MazeMap.TileState up = CheckTile(new Vector2(0, 1), mapToModify, currentTileCoordinates);
                        MazeMap.TileState down = CheckTile(new Vector2(0, -1), mapToModify, currentTileCoordinates);
                        //tally up neighbouring walls
                        int neighbours = 0;
                        MazeMap.TileState newState = MazeMap.TileState.wall;
                        if (IsWall(up)) {
                            newState = MazeMap.TileState.endWallDown;
                            neighbours++;
                        }
                        if (IsWall(right)) {
                            newState = MazeMap.TileState.endWallLeft;
                            neighbours++;
                        }
                        if (IsWall(down)) {
                            newState = MazeMap.TileState.endWallUp;
                            neighbours++;
                        }
                        if (IsWall(left)) {
                            newState = MazeMap.TileState.endWallRight;
                            neighbours++;
                        }
                        //any wall that has 0 or 1 neighbours is considered an end wall
                        if (neighbours <= 1) {
                            mapToModify.map[(row * mapToModify.mazeWidth) + column] = newState;
                        }
                    }
                    calculationCounter++;
                }
                if (calculationCounter >= calculationsPerFrame && calculationsPerFrame > 0) {
                    column--;
                } else if (column >= mapToModify.mazeWidth) {
                    column = 0;
                }
            }
            if (calculationCounter >= calculationsPerFrame && calculationsPerFrame > 0) {
                row--;
            } else if (row >= mapToModify.mazeLength) {
                keepGoing = false;
            }
            return keepGoing;
        }
        #endregion

        #region Check TileState + Name
        /// <summary>
        /// Determines if there should not be a wall on the tile that has the provided state
        /// </summary>
        /// <param name="state">The state of the tile</param>
        /// <returns>True if there shouldn't be a wall on this tile, false otherwise</returns>
        public bool IsFloor(MazeMap.TileState state) {
            return (state == MazeMap.TileState.brokenWall || state == MazeMap.TileState.deadEnd || state == MazeMap.TileState.finish ||
                    state == MazeMap.TileState.room || state == MazeMap.TileState.unexplored || state == MazeMap.TileState.visitedOnce ||
                    state == MazeMap.TileState.visitedTwice || state == MazeMap.TileState.start || state == MazeMap.TileState.unique);
        }

        /// <summary>
        /// Determines if there should be a wall on a tile that has the provided state
        /// </summary>
        /// <param name="state">The state of the tile</param>
        /// <returns>True if their should be a wall on this tile, false otherwise</returns>
        public bool IsWall(MazeMap.TileState state) {
            return (state == MazeMap.TileState.cornerWall || state == MazeMap.TileState.endWallUp || state == MazeMap.TileState.endWallRight ||
                    state == MazeMap.TileState.endWallDown || state == MazeMap.TileState.endWallLeft || state == MazeMap.TileState.wall);
        }
        #region Check Duplicate Name
        /// <summary>
        /// Checks for existing mazes with the proposed name and deletes them to facilitate "overwriting". Used when the application is playing.
        /// </summary>
        /// <param name="nameToCheck">The name to be checked against</param>
        private void CheckDuplicateMazeNames(string nameToCheck) {
            if (Application.isPlaying) {
                Maze[] mazes = FindObjectsOfType<Maze>();
                foreach (Maze maze in mazes) {
                    if (maze.gameObject.name.Equals(nameToCheck)) {
                        Destroy(maze.gameObject);
                    }
                }
            } else {
                Debug.LogError("CheckDuplicateMazeNames(string nameToCheck) was called from the editor - this is not allowed as seperate editor logic handles this when the game isn't running.");
            }
        }
        #endregion
        #endregion

        #region Room Set
        /// <summary>
        /// Finds tiles to clear out in order to create space for user specified rooms. Also instantiates said rooms.
        /// </summary>
        /// <param name="mapToModify">The MazeMap being edited.</param>
        /// <param name="room">The object containing all room data and the room prefab.</param>
        /// <param name="rootMazeTransform">The maze that this room will be attached to.</param>
        /// <param name="mazeLength">The length of the maze.</param>
        /// <param name="mazeWidth">The width of the maze.</param>
        /// <param name="mazeDimension">Whether the maze is 2D or 3D</param>
        /// <returns></returns>
        private bool SetRoom(MazeMap mapToModify, RoomPrefab room, int mazeLength, int mazeWidth) {
            if (!room.fixedPosition) {
                room.xPosition = Random.Range(1, mazeWidth - room.width);
                room.zPosition = Random.Range(1, mazeLength - room.length);
            }
            Vector2 startingTile = new Vector2(room.xPosition, room.zPosition);
            for (int row = 0; row < room.length; row++) {
                for (int column = 0; column < room.width; column++) {
                    if (mapToModify.map[(((int)startingTile.y + row) * mapToModify.mazeWidth) + (int)startingTile.x + column] == MazeMap.TileState.room) {
                        return true; //returning true is an indicator to keep going (There's enough space for the room)
                    }
                }
            }
            for (int row = 0; row < room.length; row++) {
                for (int column = 0; column < room.width; column++) {
                    mapToModify.map[(((int)startingTile.y + row) * mapToModify.mazeWidth) + (int)startingTile.x + column] = MazeMap.TileState.room;
                }
            }

            GameObject tempRoom = Instantiate(room.gameObject);
            tempRoom.transform.localPosition = new Vector3(room.xPosition * mazeTileWidthAndLength, room.height, room.zPosition * mazeTileWidthAndLength);
            room.placedOnce = true;

            return false; //returning false is an indicator to stop
        }
        #endregion

        #region Place Details
        private bool PlaceRandomWallDetails(MazeMap mapToModify, WallDetail[] objectsForWalls, float tileWidthAndLength, Transform mazeTransform, int instantiationsPerFrame, ref int detailIndex, ref int row, ref int column) {
            int instantiationCounter = 0;
            bool keepGoing = true;
            for (; detailIndex < wallDetails.Length && (instantiationCounter < instantiationsPerFrame || instantiationsPerFrame < 1); detailIndex++) {
                MazeMap.TileState testResults = MazeMap.TileState.outOfBounds;
                Vector2 move = Vector2.zero;
                //search through the maze until you find a wall since these details will only be placed on walls
                for (; row < mapToModify.mazeLength && (instantiationCounter < instantiationsPerFrame || instantiationsPerFrame < 1); row++) {
                    for (; column < mapToModify.mazeWidth && (instantiationCounter < instantiationsPerFrame || instantiationsPerFrame < 1); column++) {
                        if (mapToModify.map[(row * mapToModify.mazeWidth) + column] == MazeMap.TileState.wall) {
                            if (Random.Range(0f, 1f) <= wallDetails[detailIndex].frequency)//random picker determines if the object will be placed on this wall or not depending on your frequency value
                            {
                                Vector2 currentTileCoordinates = new Vector2(column, row);
                                TestDirection testDirection = (TestDirection)1;
                                Quaternion detailRotation = mazeTransform.rotation;
                                Vector3 detailZDirection = Vector3.zero;
                                Vector3 detailXDirection = Vector3.zero;

                                //test every direction from the wall so that you only place the detail on faces of the wall piece that can be seen
                                //if you've found a valid placement, the rotation (z-out relative to the normal of the wall face) is stored, as well
                                //as the randomly selected vertical/horizontal position
                                while ((int)testDirection <= 4) {
                                    switch (testDirection) {
                                        case TestDirection.left: //one is left
                                            move = new Vector2(-1, 0);
                                            detailRotation = Quaternion.LookRotation(-mazeTransform.right, mazeTransform.up);
                                            detailZDirection = -mazeTransform.right;
                                            detailXDirection = -mazeTransform.forward;
                                            break;

                                        case TestDirection.up: //two is up
                                            move = new Vector2(0, 1);
                                            detailRotation = Quaternion.LookRotation(mazeTransform.forward, mazeTransform.up);
                                            detailZDirection = mazeTransform.forward;
                                            detailXDirection = -mazeTransform.right;
                                            break;

                                        case TestDirection.right: //three is right
                                            move = new Vector2(1, 0);
                                            detailRotation = Quaternion.LookRotation(mazeTransform.right, mazeTransform.up);
                                            detailZDirection = mazeTransform.right;
                                            detailXDirection = mazeTransform.forward;
                                            break;

                                        case TestDirection.down: //four is down
                                            move = new Vector2(0, -1);
                                            detailRotation = Quaternion.LookRotation(-mazeTransform.forward, mazeTransform.up);
                                            detailZDirection = -mazeTransform.forward;
                                            detailXDirection = mazeTransform.right;
                                            break;
                                    }
                                    testResults = CheckTile(move, mapToModify, currentTileCoordinates);
                                    if (testResults != MazeMap.TileState.wall && testResults != MazeMap.TileState.outOfBounds) {
                                        GameObject tempDetail = Instantiate(wallDetails[detailIndex].detailPrefab, new Vector3(column * tileWidthAndLength, Random.Range(wallDetails[detailIndex].minHeight, wallDetails[detailIndex].maxHeight), row * mazeTileWidthAndLength) + mazeTransform.position + ((mazeTileWidthAndLength / 2f) + wallDetails[detailIndex].thickness / 2f) * detailZDirection + Random.Range(wallDetails[detailIndex].minHorizontalOffset, wallDetails[detailIndex].maxHorizontalOffset) * detailXDirection, detailRotation);
                                        tempDetail.transform.parent = mazeTransform;
                                        tempDetail.name = "Detail(" + wallDetails[detailIndex].detailName + ")";
                                    }
                                    testDirection++;
                                }
                            }
                        }
                        instantiationCounter++;
                    }
                    if (instantiationCounter >= instantiationsPerFrame && instantiationsPerFrame > 0) {
                        column--;
                    } else if (column >= mapToModify.mazeWidth) {
                        column = 0;
                    }
                }
                if (instantiationCounter >= instantiationsPerFrame && instantiationsPerFrame > 0) {
                    row--;
                } else if (row >= mapToModify.mazeLength) {
                    row = 0;
                }
            }
            if (instantiationCounter >= instantiationsPerFrame && instantiationsPerFrame > 0) {
                detailIndex--;
            } else if (detailIndex >= wallDetails.Length) {
                keepGoing = false;
            }
            return keepGoing;
        }

        /// <summary>
        /// Places random details throughout the maze
        /// </summary>
        /// <param name="mapToModify">The maze map that will store information about where the details are placed</param>
        /// <param name="otherObjects">The array of details that will be pulled from to place the objects</param>
        /// <param name="tileWidthAndLength">The length and width of the tiles (used to calculate positioning)</param>
        /// <param name="mazeTransform">The transform that these details will need to be parented to</param>
        /// <param name="instantiationsPerFrame">The number of these objects that will be instantiated every frame until this is done</param>
        /// <param name="detailIndex">Used to keep track of what's been instantiated</param>
        /// <param name="row">Used to keep track of where things have been instantiated</param>
        /// <param name="column">Used to keep track of where things have been instantiated</param>
        /// <returns>True if it isn't finished and false if it is</returns>
        private bool PlaceRandomOtherDetails(MazeMap mapToModify, OtherDetail[] otherObjects, float tileWidthAndLength, Transform mazeTransform, int instantiationsPerFrame, ref int detailIndex, ref int row, ref int column) {
            int instantiationCounter = 0;
            bool keepGoing = true;
            for (; detailIndex < otherObjects.Length && (instantiationCounter < instantiationsPerFrame || instantiationsPerFrame < 1); detailIndex++) {
                //search through the maze until you find a floor since details will be placed on tiles that aren't wall pieces or finish squares
                for (; row < mapToModify.mazeLength && (instantiationCounter < instantiationsPerFrame || instantiationsPerFrame < 1); row++) {
                    for (; column < mapToModify.mazeWidth && (instantiationCounter < instantiationsPerFrame || instantiationsPerFrame < 1); column++) {
                        Vector2 currentTileCoordinates = new Vector2(column, row); //sets the current tile to our current coordinates
                        MazeMap.TileState testResults = CheckTile(Vector2.zero, mapToModify, currentTileCoordinates); //we're just checking the tile we're on
                        if (testResults != MazeMap.TileState.wall && testResults != MazeMap.TileState.finish && testResults != MazeMap.TileState.start && testResults != MazeMap.TileState.room) //if the tile isn't a wall, the finish square, or a room we can procede
                        {
                            if (Random.Range(0f, 1f) <= otherObjects[detailIndex].frequency)//random picker determines if the object will be placed on this tile or not depending on your frequency value
                            {
                                GameObject tempDetail = Instantiate(otherObjects[detailIndex].detailPrefab, new Vector3(column * tileWidthAndLength + Random.Range(otherObjects[detailIndex].minXOffset, otherObjects[detailIndex].maxXOffset), Random.Range(otherObjects[detailIndex].minHeight, otherObjects[detailIndex].maxHeight), row * mazeTileWidthAndLength + Random.Range(otherObjects[detailIndex].minZOffset, otherObjects[detailIndex].maxZOffset)) + mazeTransform.position, mazeTransform.rotation);
                                tempDetail.transform.Rotate(transform.up, Random.Range(0f, 360f));
                                tempDetail.transform.parent = mazeTransform;
                                tempDetail.name = "Detail(" + otherObjects[detailIndex].detailName + ")";
                            }
                        }
                        instantiationCounter++;
                    }
                    if (instantiationCounter >= instantiationsPerFrame && instantiationsPerFrame > 0) {
                        column--;
                    } else if (column >= mapToModify.mazeWidth) {
                        column = 0;
                    }
                }
                if (instantiationCounter >= instantiationsPerFrame && instantiationsPerFrame > 0) {
                    row--;
                } else if (row >= mapToModify.mazeLength) {
                    row = 0;
                }
            }
            if (instantiationCounter >= instantiationsPerFrame && instantiationsPerFrame > 0) {
                detailIndex--;
            } else if (detailIndex >= otherObjects.Length) {
                keepGoing = false;
            }
            return keepGoing;
        }

        /// <summary>
        /// Places random two dimensional details throughout the maze
        /// </summary>
        /// <param name="mapToModify">The maze map that will store information about where the details are placed</param>
        /// <param name="twoDimensionalObjects">The array of details that will be pulled from to place the objects</param>
        /// <param name="tileWidthAndLength">The length and width of the tiles (used to calculate positioning)</param>
        /// <param name="mazeTransform">The transform that these details will need to be parented to</param>
        /// <param name="instantiationsPerFrame">The number of these objects that will be instantiated every frame until this is done</param>
        /// <param name="detailIndex">Used to keep track of what's been instantiated</param>
        /// <param name="row">Used to keep track of where things have been instantiated</param>
        /// <param name="column">Used to keep track of where things have been instantiated</param>
        /// <returns>True if it isn't finished and false if it is</returns>
        private bool PlaceRandomTwoDimensionalDetails(MazeMap mapToModify, TwoDimensionalDetail[] twoDimensionalObjects, float tileWidthAndLength, Transform mazeTransform, int instantiationsPerFrame, ref int detailIndex, ref int row, ref int column) {
            int instantiationCounter = 0;
            bool keepGoing = true;
            for (; detailIndex < twoDimensionalObjects.Length; detailIndex++) {
                //search through the maze until you find a floor since details will be placed on tiles that aren't wall pieces or finish squares
                for (; row < mapToModify.mazeLength && (instantiationCounter < instantiationsPerFrame || instantiationsPerFrame < 1); row++) {
                    for (; column < mapToModify.mazeWidth && (instantiationCounter < instantiationsPerFrame || instantiationsPerFrame < 1); column++) {
                        Vector2 currentTileCoordinates = new Vector2(column, row); //sets the current tile to our current coordinates
                        MazeMap.TileState testResults = CheckTile(Vector2.zero, mapToModify, currentTileCoordinates); //we're just checking the tile we're on
                        //if (mapToModify.map[(row * mapToModify.mazeWidth) + column] != MazeMap.TileState.finish && mapToModify.map[(row * mapToModify.mazeWidth) + column] != MazeMap.TileState.wall)
                        if (testResults != MazeMap.TileState.wall && testResults != MazeMap.TileState.finish && testResults != MazeMap.TileState.start && testResults != MazeMap.TileState.room) //if the tile isn't a wall, the finish square, or a room, we can procede
                        {
                            if (Random.Range(0f, 1f) <= twoDimensionalObjects[detailIndex].frequency)//random picker determines if the object will be placed on this tile or not depending on your frequency value
                            {
                                GameObject tempDetail = (GameObject)Instantiate(twoDimensionalObjects[detailIndex].detailPrefab, new Vector3(column * tileWidthAndLength + Random.Range(twoDimensionalObjects[detailIndex].minXOffset, twoDimensionalObjects[detailIndex].maxXOffset), row * mazeTileWidthAndLength + Random.Range(twoDimensionalObjects[detailIndex].minYOffset, twoDimensionalObjects[detailIndex].maxYOffset), twoDimensionalObjects[detailIndex].zPlane) + mazeTransform.position, mazeTransform.rotation);
                                tempDetail.transform.Rotate(transform.up, Random.Range(0f, 360f));
                                tempDetail.transform.parent = mazeTransform;
                                tempDetail.name = "Detail(" + twoDimensionalObjects[detailIndex].detailName + ")";
                            }
                        }
                        instantiationCounter++;
                    }
                    if (instantiationCounter >= instantiationsPerFrame && instantiationsPerFrame > 0) {
                        column--;
                    } else if (column >= mapToModify.mazeWidth) {
                        column = 0;
                    }
                }
                if (instantiationCounter >= instantiationsPerFrame && instantiationsPerFrame > 0) {
                    row--;
                } else if (row >= mapToModify.mazeLength) {
                    row = 0;
                }
            }
            if (instantiationCounter >= instantiationsPerFrame && instantiationsPerFrame > 0) {
                detailIndex--;
            } else if (detailIndex >= twoDimensionalObjects.Length) {
                keepGoing = false;
            }
            return keepGoing;
        }
        #endregion
    }
}
