using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Linq;

namespace ProjectMaze
{
    [ExecuteInEditMode]
    public class GenMaze : MonoBehaviour
    {

        #region Tooltips + Variables
        public enum MazeDimension
        {
            TwoDimensional,
            ThreeDimensional
        }
        [Tooltip("The dimensional space the maze will be generated in")]
        public MazeDimension mazeDimension;
        [Tooltip("If set to true, you may enter a seed value to control the generation of the maze. Assuming the variables on this Maze Generator are identical, setting the seed value will result in the exact same maze being generated every time. If any of the settings have changed however (i.e. the width or length is different), then the maze will generate differently.")]
        public bool useSeedValue;
        [Tooltip("The seed value used to generate this maze. Entering the same seed value will result in the same placement of floor tiles, walls, and details.")]
        public int seedValue;
        [Tooltip("Shows the space the maze will take up in the inspector once generated as a cube gizmo.")]
        public bool showDimensionsGizmo;
        [Tooltip("Sets the colour of the gizmo that shows what the generated maze dimensions will be.")]
        public Color dimensionsGizmoColour = Color.green; //bright green is easily visible. This can be changed by the user later
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
        [Tooltip("If checked, a floor tile will be removed or replaced for the exit.")]
        public bool makeFloorExit;
        [Tooltip("If checked, a floor tile will be removed or replaced for the start.")]
        public bool makeFloorStart;
        [Tooltip("If checked, any specified unique tiles will be added to the maze.")]
        public bool makeUniqueTiles;
        [Tooltip("Each of these unique tiles will be placed somewhere throughout the maze.")]
        public UniqueTile[] uniqueTiles = new UniqueTile[1];
        public enum FloorExitType
        {
            random,     //random inside the maze
            center,     //random, center 1/3 of the maze
            outside     //random, outer 1/3 of the maze
        }
        [Tooltip("Center: tile will be located in roughly the center third of the maze.\nOutside: tile will be placed in the ring around that center\nRandom: tile can be anywhere.")]
        public FloorExitType exitType = FloorExitType.random;
        [Tooltip("Center: tile will be located in roughly the center third of the maze.\nOutside: tile will be placed in the ring around that center\nRandom: tile can be anywhere.")]
        public FloorExitType startType = FloorExitType.random;
        [Tooltip("If true, the generator will attempt to place pre-defined rooms throughout the maze (think open gardens or dungeon rooms)")]
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
        public enum OutsideWallDeleteMode
        {
            random,
            opposite,
            symmetric,
            classic,
            elite

        }
        [Tooltip("The manner in which to delete outer walls. Random is random, opposite deletes pieces in pairs on opposite sides. Symmetric deletes pieces in pairs on opposite sides in symmetric locations.")]
        public OutsideWallDeleteMode outsideWallDeleteMode = OutsideWallDeleteMode.random;
        public bool deleteOutsideWalls = false;
        [Tooltip("The number of wall pieces around the outside edge to delete. Useful for entrances and exits.")]
        public int outsideWallPiecesToDelete = 0;
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
        [Tooltip("The number of possible unique exits that could be instantiated in your maze.\nPress the plus/minus buttons to adjust.")]
        public int numberOfExitPieces = 1;
        [Tooltip("The number of possible unique starts that could be instantiated in your maze.\nPress the plus/minus buttons to adjust.")]
        public int numberOfStartPieces = 1;
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
        #endregion

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

        #region 2D Specific Variables
        [Tooltip("The default z-value of the wall pieces")]
        public float defaultWallZPlane;
        [Tooltip("The z-value of the floor pieces")]
        public float floorZPlane;
        public GameObject[] floorPieces2D = new GameObject[1];
        public WallPiece2D[] wallPieces2D = new WallPiece2D[1];
        public WallPiece2D[] cornerWallPieces2D = new WallPiece2D[1];
        public WallPiece2D[] endWallPieces2D = new WallPiece2D[1];
        [Tooltip("The two-dimensional details that will be scattered throughout a two-dimensional maze")]
        public TwoDimensionalDetail[] twoDimensionalDetails = new TwoDimensionalDetail[1];
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

        #region Gizmo - Outline box
        void OnDrawGizmosSelected() {
            if (showDimensionsGizmo) {
                float offset = 0.1f;
                if (mazeDimension == MazeDimension.TwoDimensional) {
                    Gizmos.color = dimensionsGizmoColour;
                    Gizmos.DrawCube(mazePosition + new Vector3(((mazeWidth / 2f) * mazeTileWidthAndLength) - mazeTileWidthAndLength / 2f, ((mazeLength / 2f) * mazeTileWidthAndLength) - mazeTileWidthAndLength / 2f, 0f), new Vector3(mazeWidth * mazeTileWidthAndLength, mazeLength * mazeTileWidthAndLength, 1.3f));
                    Gizmos.color = Color.black;
                    Gizmos.DrawWireCube(mazePosition + new Vector3(((mazeWidth / 2f) * mazeTileWidthAndLength) - mazeTileWidthAndLength / 2f, ((mazeLength / 2f) * mazeTileWidthAndLength) - mazeTileWidthAndLength / 2f, 0f), new Vector3(mazeWidth * mazeTileWidthAndLength, mazeLength * mazeTileWidthAndLength, 1.3f));
                } else {
                    Gizmos.color = dimensionsGizmoColour;
                    Gizmos.DrawCube(mazePosition + new Vector3(((mazeWidth / 2f) * mazeTileWidthAndLength) - mazeTileWidthAndLength / 2f, (defaultWallHeight / 2f) - (defaultFloorThickness / 2f), ((mazeLength / 2f) * mazeTileWidthAndLength) - mazeTileWidthAndLength / 2f), new Vector3(mazeWidth * mazeTileWidthAndLength + offset, defaultWallHeight + defaultFloorThickness + offset, mazeLength * mazeTileWidthAndLength + offset));
                    Gizmos.color = Color.black;
                    Gizmos.DrawWireCube(mazePosition + new Vector3(((mazeWidth / 2f) * mazeTileWidthAndLength) - mazeTileWidthAndLength / 2f, (defaultWallHeight / 2f) - (defaultFloorThickness / 2f), ((mazeLength / 2f) * mazeTileWidthAndLength) - mazeTileWidthAndLength / 2f), new Vector3(mazeWidth * mazeTileWidthAndLength + offset, defaultWallHeight + defaultFloorThickness + offset, mazeLength * mazeTileWidthAndLength + offset));
                }
            }
        }
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


            //Default 2D Walls Start
            if (wallPieces2D == null || wallPieces2D.Length == 0) {
                wallPieces2D = new WallPiece2D[1];
                wallPieces2D[0] = new WallPiece2D(null, defaultWallZPlane);
            }

            if (cornerWallPieces2D == null || cornerWallPieces2D.Length == 0) {
                cornerWallPieces2D = new WallPiece2D[1];
                cornerWallPieces2D[0] = new WallPiece2D(null, defaultWallZPlane);
            }

            if (endWallPieces2D == null || endWallPieces2D.Length == 0) {
                endWallPieces2D = new WallPiece2D[1];
                endWallPieces2D[0] = new WallPiece2D(null, defaultWallZPlane);
            }

            //Default Floors
            if (floorPieces == null || floorPieces.Length == 0) {
                floorPieces = new FloorPiece[1];
                floorPieces[0] = new FloorPiece(null, defaultFloorThickness);
            }

            if (floorPieces2D == null || floorPieces2D.Length == 0) {
                floorPieces2D = new GameObject[1];
                floorPieces2D[0] = null;
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

            if (deleteOutsideWalls) {
                int totalOutsideWalls = (mazeLength * 2) + (mazeWidth * 2) - 4;
                if (outsideWallPiecesToDelete > totalOutsideWalls) {
                    outsideWallPiecesToDelete = totalOutsideWalls;
                }
                //has to delete a multiple of 2 walls
                if (outsideWallDeleteMode == OutsideWallDeleteMode.opposite || outsideWallDeleteMode == OutsideWallDeleteMode.symmetric) {
                    if (outsideWallPiecesToDelete % 2 == 1) {
                        outsideWallPiecesToDelete++;
                    }
                }
            }

            //sets a default name to generated maze if the user doesn't set one
            if (string.IsNullOrEmpty(mazeName)) {
                mazeName = "DefaultName";
            }
            //if we're going to place unique tiles, better make sure we have a dictionary for it
            if (makeUniqueTiles) {
                if (uniqueTileDict == null) {
                    uniqueTileDict = new Dictionary<Vector2, UniqueTile>();
                }
            }

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

                foreach (TwoDimensionalDetail detail in twoDimensionalDetails) {
                    if (detail.detailPrefab) {
                        //Name string Check
                        if (string.IsNullOrEmpty(detail.detailName)) {
                            detail.detailName = detail.detailPrefab.name;
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

                        //y-axis clamping
                        if (detail.minYOffset < -mazeTileWidthAndLength / 2 + detail.width / 2) {
                            detail.minYOffset = -mazeTileWidthAndLength / 2 + detail.width / 2;
                        }
                        if (detail.maxYOffset < -mazeTileWidthAndLength / 2 + detail.width / 2) {
                            detail.maxYOffset = -mazeTileWidthAndLength / 2 + detail.width / 2;
                        }
                        if (detail.minYOffset > mazeTileWidthAndLength / 2 - detail.width / 2) {
                            detail.minYOffset = mazeTileWidthAndLength / 2 - detail.width / 2;
                        }
                        if (detail.maxYOffset > mazeTileWidthAndLength / 2 - detail.width / 2) {
                            detail.maxYOffset = mazeTileWidthAndLength / 2 - detail.width / 2;
                        }
                        if (detail.maxYOffset < detail.minYOffset) {
                            detail.maxYOffset = detail.minYOffset;
                        }
                    }
                }
            }
        }


        #endregion

        #region Generate Maze - Single Frame
        /// <summary>
        /// Generates a new maze
        /// </summary>
        /// <returns>
        /// Returns the root GameObject of the generated maze which will have the Maze component
        /// </returns>
        public GameObject GenerateNewMaze() {
            int rowTracker;
            int columnTracker;
            int indexTracker;
            bool keepGoing; //used to indicate completeness of each function

            StartMazeMap(); //starts the maze map at a blank slate and deletes current one if there is one existing

            Vector2 currentTileCoordinates = new Vector2(1, 1); //sets the current tile to the bottom-left corner

            //if we're not using a pre-set seed value, it needs to be set now
            if (!useSeedValue) {
                seedValue = Random.Range(int.MinValue, int.MaxValue);
            }
            Random.InitState(seedValue); //this is what keeps the generation the same if we use a set seed value

            keepGoing = true; //a bool that creates a gate for the following loop to exit  
            do {
                keepGoing = GetNextTile(latestMazeMap, ref currentTileCoordinates); //generates maze, single tile at a time using random generator and sets every tile a state for all post generation functionality
            } while (keepGoing);

            if (makeBraidMaze && braidFrequency > 0f) {
                keepGoing = true;
                rowTracker = 0;
                columnTracker = 0;

                do {
                    keepGoing = EraseSomeDeadEnds(latestMazeMap, 10000, ref rowTracker, ref columnTracker); //erases dead ends
                } while (keepGoing);
            }

            if (deleteOutsideWalls) {
                EraseSomeOuterWalls(latestMazeMap, outsideWallPiecesToDelete, outsideWallDeleteMode);
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
                            keepGoing = SetRoom(latestMazeMap, chosenRoom, theMaze.transform, mazeLength, mazeWidth, mazeDimension);
                            timesTested++;
                        } while (keepGoing && timesTested < roomPlacementAttempts);
                    }
                }
                foreach (RoomPrefab room in rooms) {
                    room.placedOnce = false;
                }
            }

            if (makeFloorExit) {
                SetStartOrFinish(latestMazeMap, exitType, false); //sets an exit point in the maze
            }

            if (makeFloorStart) {
                SetStartOrFinish(latestMazeMap, startType, true); //sets a start point in the maze
            }

            if (makeUniqueTiles) {
                SetUniqueTiles(latestMazeMap, uniqueTiles);
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
            do {
                if (mazeDimension == MazeDimension.TwoDimensional) {
                    keepGoing = InstantiateMaze2D(latestMazeMap, theMaze.transform, wallPieces2D, cornerWallPieces2D, endWallPieces2D, floorPieces2D, exitPieces, startPieces, mazeTileWidthAndLength, floorZPlane, -1, ref rowTracker, ref columnTracker, nodes);
                } else {
                    keepGoing = InstantiateMaze3D(latestMazeMap, theMaze.transform, wallPieces, cornerWallPieces, endWallPieces, floorPieces, exitPieces, startPieces, mazeTileWidthAndLength, defaultFloorThickness, -1, ref rowTracker, ref columnTracker, nodes);
                }
            } while (keepGoing);

            if (addDetails) {
                if (mazeDimension == MazeDimension.TwoDimensional) {
                    rowTracker = 0;
                    columnTracker = 0;
                    indexTracker = 0;
                    keepGoing = true;
                    do {
                        keepGoing = PlaceRandomTwoDimensionalDetails(latestMazeMap, twoDimensionalDetails, mazeTileWidthAndLength, theMaze.transform, -1, ref indexTracker, ref rowTracker, ref columnTracker);
                    } while (keepGoing);
                } else {
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
            }

            theMaze.GetComponent<Maze>().mazeMap = latestMazeMap; //stores the maze information in a mazeMap located on the maze itself

            return theMaze;
        }
        #endregion

        #region Clear Stage and Start New Maze Map
        /// <summary>
        /// Clears the current map and starts a new one (editor generation only)
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

        #region Find Valid Tile
        /// <summary>
        /// Finds a new, valid, tile to look at
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

                    //if we've checked every direction and there aren't any valid tiles, the generation process has finished
                    if (directionIndex == testDirections.Count - 1 && !foundValidTile) {
                        mazeMapToCheck.map[((int)(currentTileCoordinates.y) * mazeMapToCheck.mazeWidth) + (int)(currentTileCoordinates.x)] = MazeMap.TileState.deadEnd; //mark as dead end for braiding as well
                        keepGoing = false;
                    }
                }
            }
            return keepGoing;
        }
        #endregion

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
                    int rNGesus;
                    GameObject tempHolder;
                    switch (mazeMap.map[(row * mazeMap.mazeWidth) + column]) {
                        case MazeMap.TileState.wall:
                            do {
                                rNGesus = Random.Range(0, walls.Length);
                            } while (!walls[rNGesus].wallPrefab);
                            tempHolder = Instantiate(walls[rNGesus].wallPrefab, new Vector3(column * tileWidthAndLength, walls[rNGesus].wallHeight / 2f, row * tileWidthAndLength) + mazeTransform.position, mazeTransform.rotation);
                            tempHolder.transform.parent = mazeTransform;
                            goto default;

                        case MazeMap.TileState.cornerWall:
                            do {
                                rNGesus = Random.Range(0, cornerWalls.Length);
                            } while (!cornerWalls[rNGesus].wallPrefab);
                            tempHolder = Instantiate(cornerWalls[rNGesus].wallPrefab, new Vector3(column * tileWidthAndLength, cornerWalls[rNGesus].wallHeight / 2f, row * tileWidthAndLength) + mazeTransform.position, mazeTransform.rotation);
                            tempHolder.transform.parent = mazeTransform;
                            goto default;

                        case MazeMap.TileState.endWallUp:
                            do {
                                rNGesus = Random.Range(0, endWalls.Length);
                            } while (!endWalls[rNGesus].wallPrefab);
                            tempHolder = Instantiate(endWalls[rNGesus].wallPrefab, new Vector3(column * tileWidthAndLength, endWalls[rNGesus].wallHeight / 2f, row * tileWidthAndLength) + mazeTransform.position, mazeTransform.rotation);
                            tempHolder.transform.parent = mazeTransform;
                            goto default;

                        case MazeMap.TileState.endWallRight:
                            do {
                                rNGesus = Random.Range(0, endWalls.Length);
                            } while (!endWalls[rNGesus].wallPrefab);
                            tempHolder = Instantiate(endWalls[rNGesus].wallPrefab, new Vector3(column * tileWidthAndLength, endWalls[rNGesus].wallHeight / 2f, row * tileWidthAndLength) + mazeTransform.position, mazeTransform.rotation);
                            tempHolder.transform.Rotate(transform.up, 90f);
                            tempHolder.transform.parent = mazeTransform;
                            goto default;

                        case MazeMap.TileState.endWallDown:
                            do {
                                rNGesus = Random.Range(0, endWalls.Length);
                            } while (!endWalls[rNGesus].wallPrefab);
                            tempHolder = Instantiate(endWalls[rNGesus].wallPrefab, new Vector3(column * tileWidthAndLength, endWalls[rNGesus].wallHeight / 2f, row * tileWidthAndLength) + mazeTransform.position, mazeTransform.rotation);
                            tempHolder.transform.Rotate(transform.up, 180f);
                            tempHolder.transform.parent = mazeTransform;
                            goto default;

                        case MazeMap.TileState.endWallLeft:
                            do {
                                rNGesus = Random.Range(0, endWalls.Length);
                            } while (!endWalls[rNGesus].wallPrefab);
                            tempHolder = Instantiate(endWalls[rNGesus].wallPrefab, new Vector3(column * tileWidthAndLength, endWalls[rNGesus].wallHeight / 2f, row * tileWidthAndLength) + mazeTransform.position, mazeTransform.rotation);
                            tempHolder.transform.Rotate(transform.up, 270f);
                            tempHolder.transform.parent = mazeTransform;
                            goto default;

                        case MazeMap.TileState.finish:
                            rNGesus = Random.Range(0, exits.Length);
                            if (exits[rNGesus]) {
                                tempHolder = Instantiate(exits[rNGesus], new Vector3(column * tileWidthAndLength, -(defaultFloorThickness / 2f), row * tileWidthAndLength) + mazeTransform.position, mazeTransform.rotation);
                                tempHolder.transform.parent = mazeTransform;
                                LinkNodes(tempHolder, mazeNodes, mazeMap, row, column);
                            }
                            instantiationCounter++;
                            break;

                        case MazeMap.TileState.start:
                            rNGesus = Random.Range(0, starts.Length);
                            if (starts[rNGesus]) {
                                tempHolder = Instantiate(starts[rNGesus], new Vector3(column * tileWidthAndLength, -(defaultFloorThickness / 2f), row * tileWidthAndLength) + mazeTransform.position, mazeTransform.rotation);
                                tempHolder.transform.parent = mazeTransform;
                                LinkNodes(tempHolder, mazeNodes, mazeMap, row, column);
                            }
                            instantiationCounter++;
                            break;

                        case MazeMap.TileState.unique:
                            UniqueTile thisTile = uniqueTileDict[new Vector2(column, row)];
                            rNGesus = Random.Range(0, thisTile.tileVariations.Length);
                            if (thisTile.tileVariations[rNGesus]) {
                                tempHolder = Instantiate(thisTile.tileVariations[rNGesus], new Vector3(column * tileWidthAndLength, -(defaultFloorThickness / 2f), row * tileWidthAndLength) + mazeTransform.position, mazeTransform.rotation);
                                tempHolder.transform.parent = mazeTransform;
                                tempHolder.gameObject.name = string.IsNullOrEmpty(thisTile.uniqueTileName) ? tempHolder.gameObject.name : thisTile.uniqueTileName;
                                LinkNodes(tempHolder, mazeNodes, mazeMap, row, column);
                            }
                            instantiationCounter++;
                            break;

                        default:
                            do {
                                rNGesus = Random.Range(0, floors.Length);
                            } while (!floors[rNGesus].floorPrefab);
                            tempHolder = Instantiate(floors[rNGesus].floorPrefab, new Vector3(column * tileWidthAndLength, -(floors[rNGesus].floorThickness / 2f), row * tileWidthAndLength) + mazeTransform.position, mazeTransform.rotation);
                            tempHolder.transform.parent = mazeTransform;
                            LinkNodes(tempHolder, mazeNodes, mazeMap, row, column);
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

        /// <summary>
        /// Instantiates all basic parts of the 2D maze (floor/walls)
        /// </summary>
        /// <param name="mazeMap">The map to read from for instantiation</param>
        /// <param name="mazeTransform">The parent the pieces will be tied to</param>
        /// <param name="walls">The pool of wall pieces to pull from</param>
        /// <param name="cornerWalls">The pool of wall pieces to pull from for corners</param>
        /// <param name="endWalls">The pool of wall pieces to pull from for ends</param>
        /// <param name="floors">The pool of floor pieces to pull from</param>
        /// <param name="exits">The pool of exits to pull from</param>
        /// <param name="tileWidthAndLength">The width/length of every tile in the maze</param>
        /// <param name="floorZPlane">The plane that all floor tiles will occupy</param>
        /// <param name="instantiationsPerFrame">The number of instantiations to perform every frame. A value of 0 or less will result the entire maze being instantiated in one frame.</param>
        /// <param name="row">The current row of the map we're reading</param>
        /// <param name="column">The current column of the map we're reading</param>
        /// <param name="mazeNodes">An empty 2D array of MazeNode objects - used to persistently keep track of nodes for linkage.</param>
        /// <returns>Returns true if it should be called again, returns false if it has finished</returns>
        private bool InstantiateMaze2D(MazeMap mazeMap, Transform mazeTransform, WallPiece2D[] walls, WallPiece2D[] cornerWalls, WallPiece2D[] endWalls, GameObject[] floors, GameObject[] exits, GameObject[] starts, float tileWidthAndLength, float floorZPlane, int instantiationsPerFrame, ref int row, ref int column, MazeNode[,] mazeNodes) {
            int instantiationCounter = 0;
            bool keepGoing = true;
            for (; row < mazeMap.mazeLength && (instantiationCounter < instantiationsPerFrame || instantiationsPerFrame < 1); row++) {
                for (; column < mazeMap.mazeWidth && (instantiationCounter < instantiationsPerFrame || instantiationsPerFrame < 1); column++) {
                    int rNGesus;
                    GameObject tempHolder;
                    switch (mazeMap.map[(row * mazeMap.mazeWidth) + column]) {
                        case MazeMap.TileState.wall:
                            do {
                                rNGesus = Random.Range(0, walls.Length);
                            } while (!walls[rNGesus].wallPrefab);
                            tempHolder = Instantiate(walls[rNGesus].wallPrefab, new Vector3(column * tileWidthAndLength, row * tileWidthAndLength, walls[rNGesus].wallZPlane) + mazeTransform.position, mazeTransform.rotation);
                            tempHolder.transform.parent = mazeTransform;
                            goto default;

                        case MazeMap.TileState.cornerWall:
                            do {
                                rNGesus = Random.Range(0, cornerWalls.Length);
                            } while (!cornerWalls[rNGesus].wallPrefab);
                            tempHolder = Instantiate(cornerWalls[rNGesus].wallPrefab, new Vector3(column * tileWidthAndLength, row * tileWidthAndLength, cornerWalls[rNGesus].wallZPlane) + mazeTransform.position, mazeTransform.rotation);
                            tempHolder.transform.parent = mazeTransform;
                            goto default;

                        case MazeMap.TileState.endWallUp:
                            do {
                                rNGesus = Random.Range(0, endWalls.Length);
                            } while (!endWalls[rNGesus].wallPrefab);
                            tempHolder = Instantiate(endWalls[rNGesus].wallPrefab, new Vector3(column * tileWidthAndLength, row * tileWidthAndLength, endWalls[rNGesus].wallZPlane) + mazeTransform.position, mazeTransform.rotation);
                            tempHolder.transform.parent = mazeTransform;
                            goto default;

                        case MazeMap.TileState.endWallRight:
                            do {
                                rNGesus = Random.Range(0, endWalls.Length);
                            } while (!endWalls[rNGesus].wallPrefab);
                            tempHolder = Instantiate(endWalls[rNGesus].wallPrefab, new Vector3(column * tileWidthAndLength, row * tileWidthAndLength, endWalls[rNGesus].wallZPlane) + mazeTransform.position, mazeTransform.rotation);
                            tempHolder.transform.Rotate(transform.forward, 90f);
                            tempHolder.transform.parent = mazeTransform;
                            goto default;

                        case MazeMap.TileState.endWallDown:
                            do {
                                rNGesus = Random.Range(0, endWalls.Length);
                            } while (!endWalls[rNGesus].wallPrefab);
                            tempHolder = Instantiate(endWalls[rNGesus].wallPrefab, new Vector3(column * tileWidthAndLength, row * tileWidthAndLength, endWalls[rNGesus].wallZPlane) + mazeTransform.position, mazeTransform.rotation);
                            tempHolder.transform.Rotate(transform.forward, 180f);
                            tempHolder.transform.parent = mazeTransform;
                            goto default;

                        case MazeMap.TileState.endWallLeft:
                            do {
                                rNGesus = Random.Range(0, endWalls.Length);
                            } while (!endWalls[rNGesus].wallPrefab);
                            tempHolder = Instantiate(endWalls[rNGesus].wallPrefab, new Vector3(column * tileWidthAndLength, row * tileWidthAndLength, endWalls[rNGesus].wallZPlane) + mazeTransform.position, mazeTransform.rotation);
                            tempHolder.transform.Rotate(transform.forward, 270f);
                            tempHolder.transform.parent = mazeTransform;
                            goto default;

                        case MazeMap.TileState.finish:
                            rNGesus = Random.Range(0, exits.Length);
                            if (exits[rNGesus]) {
                                tempHolder = Instantiate(exits[rNGesus], new Vector3(column * tileWidthAndLength, row * tileWidthAndLength, floorZPlane) + mazeTransform.position, mazeTransform.rotation);
                                tempHolder.transform.parent = mazeTransform;
                                LinkNodes(tempHolder, mazeNodes, mazeMap, row, column);
                            }
                            instantiationCounter++;
                            break;

                        case MazeMap.TileState.start:
                            rNGesus = Random.Range(0, starts.Length);
                            if (starts[rNGesus]) {
                                tempHolder = Instantiate(starts[rNGesus], new Vector3(column * tileWidthAndLength, row * tileWidthAndLength, floorZPlane) + mazeTransform.position, mazeTransform.rotation);
                                tempHolder.transform.parent = mazeTransform;
                                LinkNodes(tempHolder, mazeNodes, mazeMap, row, column);
                            }
                            instantiationCounter++;
                            break;

                        case MazeMap.TileState.unique:
                            UniqueTile thisTile = uniqueTileDict[new Vector2(column, row)];
                            rNGesus = Random.Range(0, thisTile.tileVariations.Length);
                            if (thisTile.tileVariations[rNGesus]) {
                                tempHolder = Instantiate(thisTile.tileVariations[rNGesus], new Vector3(column * tileWidthAndLength, row * tileWidthAndLength, floorZPlane) + mazeTransform.position, mazeTransform.rotation);
                                tempHolder.transform.parent = mazeTransform;
                                tempHolder.gameObject.name = string.IsNullOrEmpty(thisTile.uniqueTileName) ? tempHolder.gameObject.name : thisTile.uniqueTileName;
                                LinkNodes(tempHolder, mazeNodes, mazeMap, row, column);
                            }
                            instantiationCounter++;
                            break;

                        default:
                            do {
                                rNGesus = Random.Range(0, floors.Length);
                            } while (!floors[rNGesus]);
                            tempHolder = Instantiate(floors[rNGesus], new Vector3(column * tileWidthAndLength, row * tileWidthAndLength, floorZPlane) + mazeTransform.position, mazeTransform.rotation);
                            tempHolder.transform.parent = mazeTransform;
                            LinkNodes(tempHolder, mazeNodes, mazeMap, row, column);
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

        #region Link Node Function
        /// <summary>
        /// Used to link all nodes together during maze instantiation
        /// </summary>
        /// <param name="nodeHost">The object that will hold the node component (generally a floor piece since there is a floor piece at every tile)</param>
        /// <param name="mazeNodes">The 2D array that persistently holds information about all nodes being linked</param>
        /// <param name="mazeMap">The map of the maze being generated</param>
        /// <param name="currentRow">The current y coordinate in the maze</param>
        /// <param name="currentColumn">The current x coordinate in the maze</param>
        private void LinkNodes(GameObject nodeHost, MazeNode[,] mazeNodes, MazeMap mazeMap, int currentRow, int currentColumn) {
            MazeNode node;
            node = nodeHost.AddComponent<MazeNode>(); //adds the MazeNode component to our host
            node.type = mazeMap.map[(currentRow * mazeMap.mazeWidth) + currentColumn]; //stores the Tile information (will make pathfinding easier)
            node.coordinates = new Vector2(currentColumn, currentRow); //stores the coordinates of this tile (might be useful for pathfinding or other applications)
            mazeNodes[currentColumn, currentRow] = node; //persistently stores information about this new node so that it can be linked to
                                                         //creates all possible links to neighbours
            if (currentColumn > 0) {
                if (mazeNodes[currentColumn - 1, currentRow]) {
                    mazeNodes[currentColumn - 1, currentRow].AddConnectionRight(node);
                }
            }
            if (currentRow > 0) {
                if (mazeNodes[currentColumn, currentRow - 1]) {
                    mazeNodes[currentColumn, currentRow - 1].AddConnectionUp(node);
                }
            }
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
            int calculationCounter = 0;

            //looks through the maze for all dead ends flagged during the generation process
            for (; row < mapToModify.mazeLength && (calculationCounter < calculationsPerFrame || calculationsPerFrame < 1); row++) {
                for (; column < mapToModify.mazeWidth && (calculationCounter < calculationsPerFrame || calculationsPerFrame < 1); column++) {
                    if (mapToModify.map[(row * mapToModify.mazeWidth) + column] == MazeMap.TileState.deadEnd) {
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

        #region Delete OuterWalls -- TODO HERE ((START/END POINTER IN OUTER WALL) + (Elite integrated with path size (if enough time))
        /// <summary>
        /// Erases pieces of the outer wall. Presumably to be used as entrances and exits.
        /// </summary>
        /// <param name="mapToModify">The maze map we're modifying.</param>
        private void EraseSomeOuterWalls(MazeMap mapToModify, int numberToDelete, OutsideWallDeleteMode deleteMode) {
            // all of bottom except the bottom-left corner
            List<int> randomColBottom = new List<int>();
            for (int count = 1; count < mapToModify.mazeWidth; count++) {
                randomColBottom.Add(count);
            }
            // randomly permute possibilities
            for (int index = 0; index < randomColBottom.Count; index++) {
                int randomGenerator = Random.Range(0, randomColBottom.Count);
                int temp = randomColBottom[index];
                randomColBottom[index] = randomColBottom[randomGenerator];
                randomColBottom[randomGenerator] = temp;
            }

            // all of top except the top-right corner
            List<int> randomColTop = new List<int>();
            for (int count = 0; count < mapToModify.mazeWidth - 1; count++) {
                randomColTop.Add(count);
            }
            // randomly permute possibilities
            for (int index = 0; index < randomColTop.Count; index++) {
                int randomGenerator = Random.Range(0, randomColTop.Count);
                int temp = randomColTop[index];
                randomColTop[index] = randomColTop[randomGenerator];
                randomColTop[randomGenerator] = temp;
            }

            // all of left except the top-left corner
            List<int> randomRowLeft = new List<int>();
            for (int count = 0; count < mapToModify.mazeLength - 1; count++) {
                randomRowLeft.Add(count);
            }
            // randomly permute possibilities
            for (int index = 0; index < randomRowLeft.Count; index++) {
                int randomGenerator = Random.Range(0, randomRowLeft.Count);
                int temp = randomRowLeft[index];
                randomRowLeft[index] = randomRowLeft[randomGenerator];
                randomRowLeft[randomGenerator] = temp;
            }

            // all of right except the bottom-right corner
            List<int> randomRowRight = new List<int>();
            for (int count = 1; count < mapToModify.mazeLength; count++) {
                randomRowRight.Add(count);
            }
            // randomly permute possibilities
            for (int index = 0; index < randomRowRight.Count; index++) {
                int randomGenerator = Random.Range(0, randomRowRight.Count);
                int temp = randomRowRight[index];
                randomRowRight[index] = randomRowRight[randomGenerator];
                randomRowRight[randomGenerator] = temp;
            }

            switch (deleteMode) {
                case OutsideWallDeleteMode.random:

                    for (int count = 0; count < numberToDelete; count++) {
                        // 0 is left wall
                        // 1 is top wall
                        // 2 is right wall
                        // 3 is bottom wall
                        int randomGenerator = Random.Range(0, 4); //selects random side
                        switch (randomGenerator) {
                            case 0:
                                if (randomRowLeft.Count > 0) {
                                    int leftRow = randomRowLeft[randomRowLeft.Count - 1];

                                    //Ensures that if this side is selected again that the deleted walls aren't next to each other
                                    randomRowLeft.RemoveAt(randomRowLeft.Count - 1);
                                    randomRowLeft.Remove(leftRow + 1);
                                    randomRowLeft.Remove(leftRow - 1);

                                    // make sure this space is accessible
                                    if (IsWall(mapToModify.map[(leftRow * mapToModify.mazeWidth) + 1])) {
                                        mapToModify.map[((leftRow + 1) * mapToModify.mazeWidth) + 0] = MazeMap.TileState.brokenWall;
                                    } else {
                                        mapToModify.map[(leftRow * mapToModify.mazeWidth) + 0] = MazeMap.TileState.brokenWall;
                                    }
                                } else {
                                    count--;
                                }
                                break;
                            case 1:
                                if (randomColTop.Count > 0) {
                                    int topCol = randomColTop[randomColTop.Count - 1];

                                    //Ensures that if this side is selected again that the deleted walls aren't next to each other
                                    randomColTop.RemoveAt(randomColTop.Count - 1);
                                    randomColTop.Remove(topCol + 1);
                                    randomColTop.Remove(topCol - 1);

                                    //make sure this space is accessible
                                    if (IsWall(mapToModify.map[((mapToModify.mazeLength - 2) * mapToModify.mazeWidth) + topCol])) {
                                        mapToModify.map[((mapToModify.mazeLength - 1) * mapToModify.mazeWidth) + (topCol + 1)] = MazeMap.TileState.brokenWall;
                                    } else {
                                        mapToModify.map[((mapToModify.mazeLength - 1) * mapToModify.mazeWidth) + topCol] = MazeMap.TileState.brokenWall;
                                    }
                                } else {
                                    count--;
                                }
                                break;
                            case 2:
                                if (randomRowRight.Count > 0) {
                                    int rightRow = randomRowRight[randomRowRight.Count - 1];

                                    //Ensures that if this side is selected again that the deleted walls aren't next to each other
                                    randomRowRight.RemoveAt(randomRowRight.Count - 1);
                                    randomRowRight.Remove(rightRow + 1);
                                    randomRowRight.Remove(rightRow - 1);
                                    // make sure this space is accessible
                                    if (IsWall(mapToModify.map[(rightRow * mapToModify.mazeWidth) + mapToModify.mazeWidth - 2])) {
                                        mapToModify.map[((rightRow - 1) * mapToModify.mazeWidth) + mapToModify.mazeWidth - 1] = MazeMap.TileState.brokenWall;
                                    } else {
                                        mapToModify.map[(rightRow * mapToModify.mazeWidth) + mapToModify.mazeWidth - 1] = MazeMap.TileState.brokenWall;
                                    }
                                } else {
                                    count--;
                                }
                                break;
                            case 3:
                                if (randomColBottom.Count > 0) {
                                    int bottomCol = randomColBottom[randomColBottom.Count - 1];

                                    //Ensures that if this side is selected again that the deleted walls aren't next to each other
                                    randomColBottom.RemoveAt(randomColBottom.Count - 1);
                                    randomColBottom.Remove(bottomCol + 1);
                                    randomColBottom.Remove(bottomCol - 1);

                                    // make sure this space is accessible
                                    if (IsWall(mapToModify.map[(1 * mapToModify.mazeWidth) + bottomCol])) {
                                        mapToModify.map[(0 * mapToModify.mazeWidth) + (bottomCol + 1)] = MazeMap.TileState.brokenWall;
                                    } else {
                                        mapToModify.map[(0 * mapToModify.mazeWidth) + bottomCol] = MazeMap.TileState.brokenWall;
                                    }
                                } else {
                                    count--;
                                }
                                break;
                        }
                    }
                    break;
                case OutsideWallDeleteMode.opposite:
                    for (int count = 0; count < numberToDelete; count += 2) {
                        int randomGenerator = Random.Range(0, 2);
                        switch (randomGenerator) {
                            // break random, opposite walls on the left and right
                            case 0:
                                if (randomRowRight.Count > 0 && randomRowLeft.Count > 0) {
                                    int leftRow = randomRowLeft[randomRowLeft.Count - 1];
                                    randomRowLeft.RemoveAt(randomRowLeft.Count - 1);
                                    mapToModify.map[(leftRow * mapToModify.mazeWidth) + 0] = MazeMap.TileState.brokenWall;
                                    // make sure this space is accessible
                                    if (IsWall(mapToModify.map[(leftRow * mapToModify.mazeWidth) + 1])) {
                                        mapToModify.map[(leftRow * mapToModify.mazeWidth) + 1] = MazeMap.TileState.brokenWall;
                                    }

                                    int rightRow = randomRowRight[randomRowRight.Count - 1];
                                    randomRowRight.RemoveAt(randomRowRight.Count - 1);
                                    mapToModify.map[(rightRow * mapToModify.mazeWidth) + mapToModify.mazeWidth - 1] = MazeMap.TileState.brokenWall;
                                    // make sure this space is accessible
                                    if (IsWall(mapToModify.map[(rightRow * mapToModify.mazeWidth) + mapToModify.mazeWidth - 2])) {
                                        mapToModify.map[(rightRow * mapToModify.mazeWidth) + mapToModify.mazeWidth - 2] = MazeMap.TileState.brokenWall;
                                    }
                                } else {
                                    // we don't have any walls to delete here, so override the randomness
                                    if (randomColTop.Count > 0 && randomColBottom.Count > 0) {
                                        goto case 1;
                                    } else {
                                        Debug.LogWarning("This is weird.");
                                        return;
                                    }
                                }
                                break;
                            // break random, opposite walls on the top and bottom
                            case 1:
                                if (randomColTop.Count > 0 && randomColBottom.Count > 0) {
                                    int topCol = randomColTop[randomColTop.Count - 1];
                                    randomColTop.RemoveAt(randomColTop.Count - 1);
                                    mapToModify.map[((mapToModify.mazeLength - 1) * mapToModify.mazeWidth) + topCol] = MazeMap.TileState.brokenWall;
                                    // make sure this space is accessible
                                    if (IsWall(mapToModify.map[((mapToModify.mazeLength - 2) * mapToModify.mazeWidth) + topCol])) {
                                        mapToModify.map[((mapToModify.mazeLength - 2) * mapToModify.mazeWidth) + topCol] = MazeMap.TileState.brokenWall;
                                    }

                                    int bottomCol = randomColBottom[randomColBottom.Count - 1];
                                    mapToModify.map[(0 * mapToModify.mazeWidth) + bottomCol] = MazeMap.TileState.brokenWall;
                                    randomColBottom.RemoveAt(randomColBottom.Count - 1);
                                    // make sure this space is accessible
                                    if (IsWall(mapToModify.map[(1 * mapToModify.mazeWidth) + bottomCol])) {
                                        mapToModify.map[(1 * mapToModify.mazeWidth) + bottomCol] = MazeMap.TileState.brokenWall;
                                    }
                                } else {
                                    // we don't have any walls to delete here, so override the randomness
                                    if (randomRowLeft.Count > 0 && randomRowRight.Count > 0) {
                                        goto case 0;
                                    } else {
                                        Debug.LogWarning("This is weird.");
                                        return;
                                    }
                                }
                                break;
                        }
                    }
                    break;
                case OutsideWallDeleteMode.symmetric:
                    for (int count = 0; count < numberToDelete; count += 2) {
                        // 0 is left/right
                        // 1 is top/bottom
                        int randomGenerator = Random.Range(0, 2);
                        switch (randomGenerator) {
                            // break symmetrically opposite walls on the left and right
                            case 0:
                                if (randomRowLeft.Count > 0 && randomRowRight.Count > 0) {
                                    int leftRow = randomRowLeft[randomRowLeft.Count - 1];
                                    randomRowLeft.RemoveAt(randomRowLeft.Count - 1);
                                    mapToModify.map[(leftRow * mapToModify.mazeWidth) + 0] = MazeMap.TileState.brokenWall;
                                    // make sure this space is accessible
                                    if (IsWall(mapToModify.map[(leftRow * mapToModify.mazeWidth) + 1])) {
                                        mapToModify.map[(leftRow * mapToModify.mazeWidth) + 1] = MazeMap.TileState.brokenWall;
                                    }

                                    int rightRow = mapToModify.mazeLength - 1 - leftRow;
                                    randomRowRight.Remove(rightRow);
                                    mapToModify.map[(rightRow * mapToModify.mazeWidth) + mapToModify.mazeWidth - 1] = MazeMap.TileState.brokenWall;
                                    // make sure this space is accessible
                                    if (IsWall(mapToModify.map[(rightRow * mapToModify.mazeWidth) + mapToModify.mazeWidth - 2])) {
                                        mapToModify.map[(rightRow * mapToModify.mazeWidth) + mapToModify.mazeWidth - 2] = MazeMap.TileState.brokenWall;
                                    }
                                } else {
                                    // we don't have any walls to delete here, so override the randomness
                                    if (randomColBottom.Count > 0 && randomColTop.Count > 0) {
                                        goto case 1; //go to the other case as we can't delete here
                                    } else {
                                        Debug.LogWarning("This is weird.");
                                        return;
                                    }
                                }
                                break;
                            // break symmetrically opposite walls on the top and bottom
                            case 1:
                                if (randomColTop.Count > 0 && randomColBottom.Count > 0) {
                                    int topCol = randomColTop[randomColTop.Count - 1];
                                    randomColTop.RemoveAt(randomColTop.Count - 1);
                                    mapToModify.map[((mapToModify.mazeLength - 1) * mapToModify.mazeWidth) + topCol] = MazeMap.TileState.brokenWall;
                                    // make sure this space is accessible
                                    if (IsWall(mapToModify.map[((mapToModify.mazeLength - 2) * mapToModify.mazeWidth) + topCol])) {
                                        mapToModify.map[((mapToModify.mazeLength - 2) * mapToModify.mazeWidth) + topCol] = MazeMap.TileState.brokenWall;
                                    }

                                    int bottomCol = mapToModify.mazeWidth - 1 - topCol;
                                    randomColBottom.Remove(bottomCol);
                                    mapToModify.map[(0 * mapToModify.mazeWidth) + bottomCol] = MazeMap.TileState.brokenWall;
                                    // make sure this space is accessible
                                    if (IsWall(mapToModify.map[(1 * mapToModify.mazeWidth) + bottomCol])) {
                                        mapToModify.map[(1 * mapToModify.mazeWidth) + bottomCol] = MazeMap.TileState.brokenWall;
                                    }
                                } else {
                                    // we don't have any walls to delete here, so override the randomness
                                    if (randomRowLeft.Count > 0 && randomRowRight.Count > 0) {
                                        goto case 0; //go to the other case as we can't delete here
                                    } else {
                                        Debug.LogWarning("This is weird.");
                                        return;
                                    }
                                }
                                break;
                        }
                    }
                    break;

                 //TODO ------------ IF ENOUGH TIME DO A SOLVE TO ENSURE THAT PATH BETWEEN IS SHORT
                case OutsideWallDeleteMode.elite:
                    // 0 is left wall
                    // 1 is top wall
                    // 2 is right wall
                    // 3 is bottom wall
                    int jump = 0;
                    bool attempt = false;
                    bool firstPass = false;
                    int firstWall = -1;
                    int randomGen = Random.Range(0,4); //selects random side
                        switch (randomGen) {
                            case 0:
                            do {
                                //finds a wall and deletes it from the list
                                int leftRow = randomRowLeft[randomRowLeft.Count - 1];
                                randomRowLeft.RemoveAt(randomRowLeft.Count - 1);

                                // make sure this space is accessible
                                if (IsWall(mapToModify.map[(leftRow * mapToModify.mazeWidth) + 1])) {
                                    leftRow++;
                                }

                                //we don't want walls right next to each other
                                randomColTop.Remove(leftRow + 1);
                                randomColTop.Remove(leftRow - 1);

                                //ensure that there is a wall between the 2 points
                                if (firstWall != -1) {
                                    attempt = eliteHelper(mapToModify, firstWall, leftRow, 0); //cleaner to check for walls between the 2 points in a seperate function
                                    if (attempt == true) {
                                        mapToModify.map[(leftRow * mapToModify.mazeWidth) + 0] = MazeMap.TileState.brokenWall;
                                        break; //we can break as we've got our 2 walls destroyed
                                    }
                                }
                                //on first wall we can just delete it
                                if (!firstPass) {
                                    mapToModify.map[(leftRow * mapToModify.mazeWidth) + 0] = MazeMap.TileState.brokenWall;
                                    firstPass = true;
                                    firstWall = leftRow;
                                }
                            } while (!attempt && randomRowLeft.Count > 0);
                            break;

                            case 1:
                            do {
                                int topCol = randomColTop[randomColTop.Count - 1];
                                //Ensures that if this side is selected again that the deleted walls aren't next to each other
                                randomColTop.RemoveAt(randomColTop.Count - 1);

                                //make sure this space is accessible
                                if (IsWall(mapToModify.map[((mapToModify.mazeLength - 2) * mapToModify.mazeWidth) + topCol])) {
                                    topCol++;
                                }
                                randomColTop.Remove(topCol + 1);
                                randomColTop.Remove(topCol - 1);

                                if (firstWall != -1) {
                                    attempt = eliteHelper(mapToModify, firstWall, topCol, 1); //cleaner to check for walls between the 2 points in a seperate function
                                    if (attempt == true) {
                                        mapToModify.map[((mapToModify.mazeLength - 1) * mapToModify.mazeWidth) + topCol] = MazeMap.TileState.brokenWall;
                                        break; //we can break as we've got our 2 walls destroyed
                                    }
                                }
                                if (!firstPass) {
                                    mapToModify.map[((mapToModify.mazeLength - 1) * mapToModify.mazeWidth) + topCol] = MazeMap.TileState.brokenWall;
                                    firstPass = true;
                                    firstWall = topCol;
                                }
                            } while (!attempt && randomColTop.Count > 0);
                            break;

                        case 2:
                            do {
                                int rightRow = randomRowRight[randomRowRight.Count - 1];
                                randomRowRight.RemoveAt(randomRowRight.Count - 1);

                                if (IsWall(mapToModify.map[(rightRow * mapToModify.mazeWidth) + mapToModify.mazeWidth - 2])) {
                                    rightRow--;
                                }
                                randomRowRight.Remove(rightRow + 1);
                                randomRowRight.Remove(rightRow - 1);

                                if (firstWall != -1) {
                                    attempt = eliteHelper(mapToModify, firstWall, rightRow, 2); //cleaner to check for walls between the 2 points in a seperate function
                                    if (attempt == true) {
                                        mapToModify.map[(rightRow * mapToModify.mazeWidth) + mapToModify.mazeWidth - 1] = MazeMap.TileState.brokenWall;
                                        break;
                                    }
                                }
                                if (!firstPass) {
                                    mapToModify.map[(rightRow * mapToModify.mazeWidth) + mapToModify.mazeWidth - 1] = MazeMap.TileState.brokenWall;
                                    firstPass = true;
                                    firstWall = rightRow;
                                }
                            } while (randomRowRight.Count > 0 && !attempt);
                            break;


                        case 3:
                            do {
                                int bottomCol = randomColBottom[randomColBottom.Count - 1];
                                randomColBottom.RemoveAt(randomColBottom.Count - 1);
                                //Ensures that if this side is selected again that the deleted walls aren't next to each other
                                if (IsWall(mapToModify.map[(1 * mapToModify.mazeWidth) + bottomCol])) {
                                    bottomCol++;
                                }
                                randomColBottom.Remove(bottomCol + 1);
                                randomColBottom.Remove(bottomCol - 1);
                                if (firstWall != -1) {
                                    attempt = eliteHelper(mapToModify, firstWall, bottomCol, 3); //cleaner to check for walls between the 2 points in a seperate function
                                    if (attempt == true) {
                                        mapToModify.map[(0 * mapToModify.mazeWidth) + bottomCol] = MazeMap.TileState.brokenWall;
                                        break;
                                    }

                                }
                                if (!firstPass) {
                                    mapToModify.map[(0 * mapToModify.mazeWidth) + bottomCol] = MazeMap.TileState.brokenWall;
                                    firstPass = true;
                                    firstWall = bottomCol;
                                }
                            } while (!attempt && randomColTop.Count > 0);
                            break;
                    }
                    break;

                //NON RANDOM CASE
                case OutsideWallDeleteMode.classic:
                    int rightSide = mapToModify.mazeWidth - 2;
                    int leftSide = 1;
                    mapToModify.map[((leftSide) * mapToModify.mazeWidth)] = MazeMap.TileState.brokenWall;
                    mapToModify.map[(rightSide * mapToModify.mazeWidth) + mapToModify.mazeWidth - 1] = MazeMap.TileState.brokenWall;
                    break;


            }
        }
        /// <summary>
        /// Ensures that there is a wall on the path between the 2 entrances. Means that the user within the maze doesn't see the exit instantly
        /// </summary>
        /// <param name="mapToModify"></param>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <param name="caseNo"></param>
        /// <returns>True if there is a wall, false otherwise</returns>
        private bool eliteHelper(MazeMap mapToModify, int first, int second, int caseNo) {
            if (second < first) {
                int temp = second;
                second = first;
                first = temp;
            }
                IEnumerable<int> numbers = Enumerable.Range(first, second);
                foreach (var num in Enumerable.Range(first, (second - first) + 1)) {
                    if (num > 0) {
                        switch (caseNo) {
                        case 0:
                            if (IsWall(mapToModify.map[(num * mapToModify.mazeWidth) + 1])) {
                                return true;
                            }
                                break;

                        case 1:
                            if (IsWall(mapToModify.map[((mapToModify.mazeLength - 2) * mapToModify.mazeWidth) + num])) {
                                return true;
                            }
                            break;

                        case 2:
                            if (IsWall(mapToModify.map[(num * mapToModify.mazeWidth) + mapToModify.mazeWidth - 2])) {
                                return true;
                            }
                            break;

                        case 3:
                            if (IsWall(mapToModify.map[(1 * mapToModify.mazeWidth) + num])) {
                                return true;
                            }
                            break;
                        }
                    } else {
                        Debug.LogError(string.Format("Something went wrong at tile: {0}\n between tiles: {1}, {2}", num, first, second));
                    }
                }
            return false;
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

        #region TileState
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
        private bool SetRoom(MazeMap mapToModify, RoomPrefab room, Transform rootMazeTransform, int mazeLength, int mazeWidth, MazeDimension mazeDimension) {
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
            tempRoom.transform.parent = rootMazeTransform;
            if (mazeDimension == MazeDimension.TwoDimensional) {
                tempRoom.transform.localPosition = new Vector3(room.xPosition * mazeTileWidthAndLength, room.zPosition * mazeTileWidthAndLength, room.height);
            } else {
                tempRoom.transform.localPosition = new Vector3(room.xPosition * mazeTileWidthAndLength, room.height, room.zPosition * mazeTileWidthAndLength);
            }
            room.placedOnce = true;

            return false; //returning false is an indicator to stop
        }
        #endregion

        #region Set Unique Points (Start/Finish)
        private void SetStartOrFinish(MazeMap mazeMap, FloorExitType typeOfExit, bool start) {
            List<Vector2> possibleCoords = new List<Vector2>();

            for (int yCoord = 0; yCoord < mazeMap.mazeLength; yCoord++) {
                for (int xCoord = 0; xCoord < mazeMap.mazeWidth; xCoord++) {
                    possibleCoords.Add(new Vector2(xCoord, yCoord));
                }
            }

            //randomizer ((allows for same seed))
            for (int index = 0; index < possibleCoords.Count; index++) {
                Vector2 temp = possibleCoords[index];
                int randomIndex = Random.Range(0, possibleCoords.Count);
                possibleCoords[index] = possibleCoords[randomIndex];
                possibleCoords[randomIndex] = temp;
            }

            bool found = false;
            for (int coordIndex = 0; coordIndex < possibleCoords.Count && !found; coordIndex++) {
                found = TestUniqueTile(possibleCoords[coordIndex], (UniqueTile.Placement)typeOfExit, mazeMap);
                if (found) {
                    mazeMap.map[((int)possibleCoords[coordIndex].y * mazeMap.mazeWidth) + (int)possibleCoords[coordIndex].x] = start ? MazeMap.TileState.start : MazeMap.TileState.finish; //mark this space as being used for a unique tile
                }
            }
            if (!found) {
                Debug.LogWarning("One of the start/end tiles could not be placed anywhere on the map. This could be a result of the current seed, the size of the map, or the tile settings.");
            }
        }

        private void SetUniqueTiles(MazeMap mazeMap, UniqueTile[] tilesToPlace) {
            List<Vector2> possibleCoords = new List<Vector2>();

            for (int yCoord = 0; yCoord < mazeMap.mazeLength; yCoord++) {
                for (int xCoord = 0; xCoord < mazeMap.mazeWidth; xCoord++) {
                    possibleCoords.Add(new Vector2(xCoord, yCoord));
                }
            }

            for (int index = 0; index < possibleCoords.Count; index++) {
                Vector2 temp = possibleCoords[index];
                int randomIndex = Random.Range(0, possibleCoords.Count);
                possibleCoords[index] = possibleCoords[randomIndex];
                possibleCoords[randomIndex] = temp;
            }

            foreach (UniqueTile tile in tilesToPlace) {
                bool found = false;
                for (int coordIndex = 0; coordIndex < possibleCoords.Count && !found; coordIndex++) {
                    found = TestUniqueTile(possibleCoords[coordIndex], tile.placement, mazeMap);
                    if (found) {
                        uniqueTileDict[possibleCoords[coordIndex]] = tile; //add the tile (with it's coordinates) to the dictionary so we can instantiate it in the right place later
                        mazeMap.map[((int)possibleCoords[coordIndex].y * mazeMap.mazeWidth) + (int)possibleCoords[coordIndex].x] = MazeMap.TileState.unique; //mark this space as being used for a unique tile
                        possibleCoords.RemoveAt(coordIndex); //this space is already being used by a unique tile, so we can't use it again
                    }
                }
                if (!found) {
                    Debug.LogWarning(string.Format("One of the unique tiles [{0}] could not be placed anywhere on the map. This could be a result of the current seed, the size of the map, or the unique tile settings.", tile.uniqueTileName));
                }
            }
        }

        /// <summary>
        /// Tests the finish to make sure it's valid
        /// </summary>
        private bool TestUniqueTile(Vector2 coord, UniqueTile.Placement placement, MazeMap mapToTest) {
            if (mapToTest.map[((int)coord.y * mapToTest.mazeWidth) + (int)coord.x] == MazeMap.TileState.brokenWall ||
                mapToTest.map[((int)coord.y * mapToTest.mazeWidth) + (int)coord.x] == MazeMap.TileState.visitedOnce ||
                mapToTest.map[((int)coord.y * mapToTest.mazeWidth) + (int)coord.x] == MazeMap.TileState.visitedTwice ||
                mapToTest.map[((int)coord.y * mapToTest.mazeWidth) + (int)coord.x] == MazeMap.TileState.deadEnd ||
                mapToTest.map[((int)coord.y * mapToTest.mazeWidth) + (int)coord.x] == MazeMap.TileState.unexplored) {
                switch (placement) {
                    case UniqueTile.Placement.center:
                        if ((int)coord.x > mapToTest.mazeWidth / 3 && (int)coord.x < (mapToTest.mazeWidth / 3) * 2) {
                            if ((int)coord.y > mapToTest.mazeLength / 3 && (int)coord.y < (mapToTest.mazeLength / 3) * 2) {
                                return true;
                            }
                        }
                        break;
                    case UniqueTile.Placement.outside:
                        if ((int)coord.x < mapToTest.mazeWidth / 3 || (int)coord.x > (mapToTest.mazeWidth / 3) * 2) {
                            if ((int)coord.y < mapToTest.mazeLength / 3 || (int)coord.y > (mapToTest.mazeLength / 3) * 2) {
                                return true;
                            }
                        }
                        break;
                    default:
                        return true;
                }
                return false;
            } else {
                return false;
            }
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

        #region Save/Load Functionality
        /// <summary>
        /// Saves the current generator settings for future use via the LoadGeneratorSettings(string loadPath) method.
        /// </summary>
        /// <param name="savePath">The path with which to save the settings.</param>
        public void SaveGeneratorSettings(string savePath) {
            MazeSettings settings = new MazeSettings(mazeName, mazeDimension, seedValue, mazeWidth, mazeLength, mazePosition, mazeTileWidthAndLength,
                                                     defaultWallHeight, defaultFloorThickness, defaultWallZPlane, floorZPlane, makeBraidMaze, braidFrequency, makeFloorExit, exitType,
                                                     deleteOutsideWalls, outsideWallPiecesToDelete, outsideWallDeleteMode, makeRooms, numberOfRooms,
                                                     roomPlacementAttempts, differentCorners, differentEnds);
            try {
                Stream stream = File.Open(savePath, FileMode.Create);
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(stream, settings);
                stream.Close();
            } catch (UnityException uex) {
                Debug.LogException(uex);
            } catch (System.Exception ex) {
                Debug.LogException(ex);
            }
        }

        /// <summary>
        /// Loads a saved set of settings for the maze generator and applies them
        /// </summary>
        /// <param name="loadPath">The filepath where the settings were saved</param>
        public void LoadGeneratorSettings(string loadPath) {
            try {
                Stream stream = File.Open(loadPath, FileMode.Open);
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                MazeSettings settings = (MazeSettings)binaryFormatter.Deserialize(stream);
                stream.Close();

                mazeName = settings.MazeName;
                mazeDimension = settings.MazeDimension;
                seedValue = settings.SeedValue;
                mazeWidth = settings.MazeWidth;
                mazeLength = settings.MazeLength;
                mazePosition = settings.MazePosition;
                mazeTileWidthAndLength = settings.MazeTileWidthAndLength;
                defaultWallHeight = settings.DefaultWallHeight;
                defaultFloorThickness = settings.DefaultFloorThickness;
                makeBraidMaze = settings.MakeBraidMaze;
                braidFrequency = settings.braidFrequency;
                makeFloorExit = settings.MakeFloorExit;
                exitType = settings.ExitType;
                makeRooms = settings.MakeRooms;
                numberOfRooms = settings.NumberOfRooms;
                roomPlacementAttempts = settings.RoomPlacementAttempts;
                differentCorners = settings.DifferentCorners;
                differentEnds = settings.DifferentEnds;
            } catch (UnityException uex) {
                Debug.LogException(uex);
            } catch (System.Exception ex) {
                Debug.LogException(ex);
            }
        }
        #endregion
    }
}
