using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// A <c>GameLevel</c> object describes a single map of the Theseus and the Minotaur game.
// It contains the map dimensions, the initial positions of the two actors, the position of the exit, and the walls present inside the map.
/// </summary>
[Serializable]
public class GameLevel
{
    /// <summary>
    /// Name of the level. Shows up on the level selection screen and functions as a title for the comment.
    /// </summary>
    public string Name;
    /// <summary>
    /// Personal comment to be shown on the side panel.
    /// </summary>
    public string Comment;
    public int Width;
    public int Height;
    public int TheseusX;
    public int TheseusY;
    public int MinotaurX;
    public int MinotaurY;
    public int ExitX;
    public int ExitY;
    public Wall[] Walls;
}

[Serializable]
public struct Wall
{
    public int X;
    public int Y;
    /// <summary>
    /// The side of the parent block that this wall exists on.
    /// 0 = North, 1 = East. Other values are not allowed.
    /// To create a south wall, create a north wall one block down. Similarly, to create a west wall, create an east wall one block left.
    /// </summary>
    public int Side;
}
