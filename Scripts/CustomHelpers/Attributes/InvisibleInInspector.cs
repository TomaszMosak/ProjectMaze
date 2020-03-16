using UnityEngine;
using System.Collections;

namespace ProjectMaze
{
    /// <summary>
    /// Accesses a property drawer that allows a public, serializable property to be invisible in the inspector. Usefull when you want a property to be serialized, but do not want people to be able to mess around with it or see it.
    /// </summary>
    public class InvisibleInInspectorAttribute : PropertyAttribute
    {
        //this class has no special properties, it is just used to access a property drawer
    }
}
