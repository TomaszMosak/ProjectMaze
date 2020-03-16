using UnityEngine;
using System.Collections;

namespace ProjectMaze
{
    /// <summary>
    /// Accesses the LessThanInclusive property which limits a float or integer in the inspector to any value less or equal to the provided value.
    /// </summary>
    public class LessThanIntAttribute : PropertyAttribute
    {
        public int lessThanInteger;
        public bool inclusive;

        /// <summary>
        /// Uses an int for the less than comparison
        /// </summary>
        /// <param name="lessThanInt">The integer that will be used to limit the value of this variable</param>
        /// <param name="inclusive">If true, a less than or equal comparison will be used. If false, a non-inclusive less than comparison will be used.</param>
        public LessThanIntAttribute(int lessThanInteger, bool inclusive)
        {
            this.lessThanInteger = lessThanInteger;
            this.inclusive = inclusive;
        }
    }
}
