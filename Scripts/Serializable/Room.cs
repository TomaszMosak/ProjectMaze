﻿using UnityEngine;

namespace ProjectMaze
{
    public class RoomPrefab : MonoBehaviour
    {
        [Tooltip("The width, in units, that you want the plaza to be.")]
        //[GreaterThanInt(0, false)]
        public int width;
        [Tooltip("The length, in units, that you want the plaza to be.")]
        //[GreaterThanInt(0, false)]
        public int length;
        [Tooltip("How tall the plaza is (used to lift the prefab off the ground so that all of the objects are above ground)")]
        public float height;
        [Tooltip("If true, will allow you to manually enter the position in which the plaza will appear.")]
        public bool fixedPosition;
        [Tooltip("How many units you want the plaza to be moved from the leftmost side of the maze.")]
        //[GreaterThanInt(0, false)]
        public int xPosition;
        [Tooltip("How many units you want the plaza to be moved from the bottom of the maze.")]
        //[GreaterThanInt(0, false)]
        public int zPosition;

        internal bool placedOnce;
    }
}