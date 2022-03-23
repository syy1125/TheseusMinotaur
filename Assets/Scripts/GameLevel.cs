using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class GameLevel
{
    public string Name;
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
