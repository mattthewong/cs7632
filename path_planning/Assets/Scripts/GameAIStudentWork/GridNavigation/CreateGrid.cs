using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace GameAICourse
{

    public class CreateGrid
    {

        // Please change this string to your name
        public const string StudentAuthorName = "Matthew Wong";


        // Helper method provided to help you implement this file. Leave as is.
        // Returns true if point p is inside (or on edge) the polygon defined by pts (CCW winding). False, otherwise
        public static bool IsPointInsidePolygon(Vector2Int[] pts, Vector2Int p)
        {
            return CG.InPoly1(pts, p) != CG.PointPolygonIntersectionType.Outside;
        }

        // Helper method provided to help you implement this file. Leave as is.
        // Returns float converted to int according to default scaling factor (1000)
        public static int Convert(float v)
        {
            return CG.Convert(v);
        }


        // Helper method provided to help you implement this file. Leave as is.
        // Returns Vector2 converted to Vector2Int according to default scaling factor (1000)
        public static Vector2Int Convert(Vector2 v)
        {
            return CG.Convert(v);
        }

        // Helper method provided to help you implement this file. Leave as is.
        // Returns true if segment AB intersects CD properly or improperly
        static public bool Intersects(Vector2Int a, Vector2Int b, Vector2Int c, Vector2Int d)
        {
            return CG.Intersect(a, b, c, d);
        }


        // PointInsideBoundingBox(): Determines whether a point (Vector2Int:p) is On/Inside a bounding box (such as a grid cell) defined by
        // minCellBounds and maxCellBounds (both Vector2Int's).
        // Returns true if the point is ON/INSIDE the cell and false otherwise
        // This method should return true if the point p is on one of the edges of the cell.
        // This is more efficient than PointInsidePolygon() for an equivalent dimension poly
        public static bool PointInsideBoundingBox(Vector2Int minCellBounds, Vector2Int maxCellBounds, Vector2Int p)
        {
            if (p.x >= minCellBounds.x && p.y <= minCellBounds.y && p.x <= maxCellBounds.x && p.y >= maxCellBounds.y)
            {
                // Point is in bounding box
                return true;
            }

            return false;
        }

        private static int[] DetermineNumericOffset(TraverseDirection dir)
        {
            switch (dir)
            {
                case TraverseDirection.Up:
                    return new int[2] { 0, 1 };
                case TraverseDirection.Down:
                    return new int[2] { 0, -1 };
                case TraverseDirection.Left:
                    return new int[2] { -1, 0 };
                case TraverseDirection.Right:
                    return new int[2] { 1, 0 };
                case TraverseDirection.DownLeft:
                    return new int[2] { -1, -1 };
                case TraverseDirection.DownRight:
                    return new int[2] { 1, -1 };
                case TraverseDirection.UpLeft:
                    return new int[2] { -1, 1 };
                case TraverseDirection.UpRight:
                    return new int[2] { 1, 1 };
                default:
                    return new int[2] { 0, 0 };
            }
        }


        // Istraversable(): returns true if the grid is traversable from grid[x,y] in the direction dir, false otherwise.
        // The grid boundaries are not traversable. If the grid position x,y is itself not traversable but the grid cell in direction
        // dir is traversable, the function will return false.
        // returns false if the grid is null, grid rank is not 2 dimensional, or any dimension of grid is zero length
        // returns false if x,y is out of range
        public static bool Istraversable(bool[,] grid, int x, int y, TraverseDirection dir, GridConnectivity conn)
        {

            if (grid == null || grid.Rank != 2 || grid.GetLength(0) == 0 || grid.GetLength(1) == 0 ||
                x > grid.GetLength(0) || y > grid.GetLength(1))
            {
                return false;
            }

            int[] numericOffset = DetermineNumericOffset(dir);
            //// goes outside of max x bounds
            if (x + numericOffset[0] >= grid.GetLength(0) || x + numericOffset[0] < 0)
            {
                return false;
            }
            // goes outside of max y bounds
            if (y + numericOffset[1] >= grid.GetLength(1) || y + numericOffset[1] < 0)
            {
                return false;
            }
            // base case grid[x,y] not traversable but dir is traversable
            if (!grid[x, y] && grid[x + numericOffset[0], y + numericOffset[1]])
            {
                return false;
            }

            switch (conn)
            {
                case GridConnectivity.FourWay:
                    return grid[x + numericOffset[0], y + numericOffset[1]];
                case GridConnectivity.EightWay:
                    return grid[x + numericOffset[0], y + numericOffset[1]];
                default:
                    return false;
            }
        }

        private static void checkTraversabilityDirections(bool[,] grid, List<Vector2> pathNodes,
            Vector2Int pathNode, int i, float cellWidth, int cols, int rows, GridConnectivity conn, List<int> pathEdge)
        {
            int currIndex = pathNodes.IndexOf(pathNodes[i]);
            if (Istraversable(grid, pathNode.x, pathNode.y, TraverseDirection.Up, GridConnectivity.FourWay))
            {
                pathEdge.Add(currIndex + 1);
            }
            if (Istraversable(grid, pathNode.x, pathNode.y, TraverseDirection.Down, GridConnectivity.FourWay))
            {
                pathEdge.Add(currIndex - 1);
            }
            if (Istraversable(grid, pathNode.x, pathNode.y, TraverseDirection.Left, GridConnectivity.FourWay))
            {
                Vector2 left = (pathNodes[i]) + new Vector2(-cellWidth, 0);
                int index = pathNodes.IndexOf(left);
                if (index != -1)
                {
                    pathEdge.Add(index);
                }
            }
            if (Istraversable(grid, pathNode.x, pathNode.y, TraverseDirection.Right, GridConnectivity.FourWay))
            {
                Vector2 right = (pathNodes[i]) + new Vector2(cellWidth, 0);
                int index = pathNodes.IndexOf(right);
                if (index != -1)
                {
                    pathEdge.Add(index);
                }
            }
            if (conn == GridConnectivity.EightWay)
            {
                if (Istraversable(grid, pathNode.x, pathNode.y, TraverseDirection.DownLeft, GridConnectivity.EightWay))
                {
                    Vector2 right = pathNodes[i] + new Vector2(-cellWidth, -cellWidth);
                    int index = pathNodes.IndexOf(right);
                    if (index != -1)
                    {
                        pathEdge.Add(index);
                    }
                    else
                    {
                        int adj = currIndex - rows - 1;
                        if (adj > 0)
                        {
                            pathEdge.Add(adj);

                        }
                    }
                }
                if (Istraversable(grid, pathNode.x, pathNode.y, TraverseDirection.DownRight, GridConnectivity.EightWay))
                {
                    Vector2 right = pathNodes[i] + new Vector2(cellWidth, -cellWidth);
                    int index = pathNodes.IndexOf(right);
                    if (index != -1)
                    {
                        pathEdge.Add(index);
                    }
                    else
                    {
                        int adj = currIndex + rows - 1;
                        if (adj > 0)
                        {
                            pathEdge.Add(adj);

                        }
                    }
                }
                if (Istraversable(grid, pathNode.x, pathNode.y, TraverseDirection.UpLeft, GridConnectivity.EightWay))
                {
                    Vector2 right = pathNodes[i] + new Vector2(-cellWidth, cellWidth);
                    int index = pathNodes.IndexOf(right);
                    if (index != -1)
                    {
                        pathEdge.Add(index);
                    }
                    else
                    {
                        int adj = currIndex - rows + 1;
                        if (adj > 0)
                        {
                            pathEdge.Add(adj);

                        }
                    }
                }
                if (Istraversable(grid, pathNode.x, pathNode.y, TraverseDirection.UpRight, GridConnectivity.EightWay))
                {
                    Vector2 right = pathNodes[i] + new Vector2(cellWidth, cellWidth);
                    int index = pathNodes.IndexOf(right);
                    if (index != -1)
                    {
                        pathEdge.Add(index);
                    }
                    else
                    {
                        int adj = currIndex + rows + 1;
                        if (adj > 0)
                        {
                            pathEdge.Add(adj);

                        }
                    }
                }
            }
        }


        // CreatePathNetworkFromGrid(): Creates a path network from a grid according to traversability
        // from one node to an adjacent node. Each node should be centered in the cell.
        // Edges from A to B should always have a matching B to A edge
        // pathNodes: a list of graph nodes, centered on each cell
        // pathEdges: graph adjacency list for each graph node. cooresponding index of pathNodes to match
        //      node with its edge list. All nodes must have an edge list (no null list)
        //      entries in each edge list are indices into pathNodes
        public static void CreatePathGraphFromGrid(
            Vector2 canvasOrigin, float canvasWidth, float canvasHeight, float cellWidth,
            GridConnectivity conn,
            bool[,] grid, out List<Vector2> pathNodes, out List<List<int>> pathEdges
            )
        {

            if (grid == null || grid.Rank != 2)
            {
                pathNodes = new List<Vector2>();
                pathEdges = new List<List<int>>();
                return;
            }

            // a list of graph nodes, centered on each cell
            pathNodes = new List<Vector2>();
            //initalization of a path edge that corresponds to same index pathNode
            pathEdges = new List<List<int>>();

            int rows = Mathf.FloorToInt(canvasHeight / cellWidth);
            int cols = Mathf.FloorToInt(canvasWidth / cellWidth);

            int nodeCount = 0;
            // for each column
            float offset = cellWidth / 2;

            for (float i = 0; i < cols; i++)
            {
                // for each row
                for (float j = 0; j < rows; j++)
                {
                    float adjustedI = i * cellWidth;
                    float adjustedJ = j * cellWidth;
                    // add a graph node to the pathNodes list (a node centered in a cell, offset by canvas)                   
                    Vector2 node = canvasOrigin + new Vector2(adjustedI + offset, adjustedJ + offset);
                    pathNodes.Add(node);
                    nodeCount++;
                }
            }

            //example of node placed in center of cell
            //pathNodes.Add(canvasOrigin + new Vector2(cellWidth / 2f, cellWidth / 2f));          

            //only one node, so can't be connected to anything, but we still initialize
            //to an empty list. Null not allowed!

            // for each path node in pathNodes
            for (int i = 0; i < pathNodes.Count; i += 1)
            {
                // determine target pathnode for checking traversability
                Vector2Int pathNode = new Vector2Int((int)((pathNodes[i].x - canvasOrigin.x) * (1 / cellWidth)), (int)((pathNodes[i].y - canvasOrigin.y) * (1 / cellWidth)));
                // initialize a new pathEdge for the pathNode
                List<int> pathEdge = new List<int>();
                checkTraversabilityDirections(grid, pathNodes, pathNode, i, cellWidth, cols, rows, conn, pathEdge);
                pathEdges.Add(pathEdge);

            }
        }

        // Create(): Creates a grid lattice discretized space for navigation.
        // canvasOrigin: bottom left corner of navigable region in world coordinates
        // canvasWidth: width of navigable region in world dimensions
        // canvasHeight: height of navigable region in world dimensions
        // cellWidth: target cell width (of a grid cell) in world dimensions
        // obstacles: a list of collider obstacles
        // grid: an array of bools. row major. a cell is true if navigable, false otherwise

        public static void Create(Vector2 canvasOrigin, float canvasWidth, float canvasHeight, float cellWidth,
            List<Polygon> obstacles,
            out bool[,] grid
            )
        {
            // Carefully consider all possible geometry interactions


            int rows = Mathf.FloorToInt(canvasHeight / cellWidth);
            int cols = Mathf.FloorToInt(canvasWidth / cellWidth);

            grid = new bool[cols, rows];

            Vector2Int convertedOrigin = Convert(canvasOrigin);

            // initially set the cell states
            for (int i = 0; i < cols; i++)
            {
                for (int j = 0; j < rows; j++)
                {
                    bool obstaclesExist = obstacles.Count > 0;
                    // if there is only one row and one column and obstacles exist, then false
                    if (rows == 1 && cols == 1 && obstaclesExist)
                    {
                        grid[i, j] = false;
                    } else
                    {
                        grid[i, j] = true;
                    }                    
                }
            }

            for (int i = 0; i < cols; i++)
            {
                for (int j = 0; j < rows; j++)
                {
                    // for each obstacle
                    for (int k = 0; k < obstacles.Count; k++)
                    {
                        // convert iteration based on cell width
                        float adjustedI = i * cellWidth;
                        float adjustedJ = j * cellWidth;
                        // convert points to an actual point based on origin
                        Vector2Int[] points = obstacles[k].getIntegerPoints();
                        Vector2Int[] pointsAdj = new Vector2Int[points.Length];

                        // for each point in the polygon, generate adjusted points with offset added 
                        for (int l = 0; l < points.Length; l++)
                        {
                            Vector2Int adjusted = points[l] - convertedOrigin;
                            pointsAdj.SetValue(adjusted, l);
                        }

                        Vector2Int ur = new Vector2Int(Convert(adjustedI) + 1, Convert(adjustedJ) + 1);

                        //check other neighboring cells
                        int offset = Convert(cellWidth) / 2;
                        Vector2Int ul = new Vector2Int(Convert(adjustedI) - 1, Convert(adjustedJ) + 1);
                        Vector2Int bl = new Vector2Int(Convert(adjustedI) - 1, Convert(adjustedJ) - 1);
                        Vector2Int br = new Vector2Int(Convert(adjustedI) + 1, Convert(adjustedJ) - 1);
                        Vector2Int cc = new Vector2Int(Convert(adjustedI) - offset, Convert(adjustedJ) + offset);

                        // cell
                        Vector2Int cell = new Vector2Int(Convert(adjustedI), Convert(adjustedJ));
                        // to save on computation, check if cell might even be in polygon
                        int polyVertexCount = points.Length;
                        //Debug.Log("POLY VERT COUNT: " + polyVertexCount);
                        //Debug.Log("CELL: " + cell);
                        //Debug.Log("POINTS: " + pointsAdj[1] + " " + pointsAdj[3]);
                        //Debug.Log("UL: " + (points.Length - (points.Length - 1)));
                        //Debug.Log("BR: " + (points.Length - 1));
                        //if (PointInsideBoundingBox(pointsAdj[points.Length-(points.Length-1)], pointsAdj[points.Length-1], cell))
                        //{
                            // check if point is in/on edge of polygon
                            if (IsPointInsidePolygon(pointsAdj, ur))
                            {
                                grid[(ur.x + offset) / Convert(cellWidth), (ur.y + offset) / Convert(cellWidth)] = false;
                            }
                            if (IsPointInsidePolygon(pointsAdj, ul))
                            {
                                grid[ul.x / Convert(cellWidth), (ul.y + offset) / Convert(cellWidth)] = false;
                            }
                            if (IsPointInsidePolygon(pointsAdj, bl))
                            {
                                grid[(bl.x - offset) / Convert(cellWidth), (bl.y - offset) / Convert(cellWidth)] = false;
                            }
                            if (IsPointInsidePolygon(pointsAdj, br))
                            {
                                grid[(br.x + offset) / Convert(cellWidth), (br.y - offset) / Convert(cellWidth)] = false;
                            }
                            if (IsPointInsidePolygon(pointsAdj, cc))
                            {
                                grid[(cc.x - offset) / Convert(cellWidth), cc.y / Convert(cellWidth)] = false;
                            }
                        //}
                    }

                }
            }
        }

    }

}