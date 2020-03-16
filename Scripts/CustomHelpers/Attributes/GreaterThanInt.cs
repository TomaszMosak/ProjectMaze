﻿using UnityEngine;
using System.Collections;

namespace ProjectMaze
{
    /// <summary>
    /// Accesses the GreaterThanInclusive property which limits a float or integer in the inspector to any value greater or equal to the provided value.
    /// </summary>
    public class GreaterThanIntAttribute : PropertyAttribute
    {
        public int greaterThanInteger;
        public bool inclusive;

        /// <summary>
        /// Uses an int for the greater than comparison
        /// </summary>
        /// <param name="greaterThanInt">The integer that will be used to limit the value of this variable</param>
        /// <param name="inclusive">If true, a less than or equal comparison will be used. If false, a non-inclusive less than comparison will be used.</param>
        public GreaterThanIntAttribute(int greaterThanInteger, bool inclusive)
        {
            this.greaterThanInteger = greaterThanInteger;
            this.inclusive = inclusive;
        }
    }
}
