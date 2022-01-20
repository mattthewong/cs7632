using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameAICourse
{

    public class CreatePathNetwork
    {

        public const string StudentAuthorName = "Matthew Wong";




        // Helper method provided to help you implement this file. Leave as is.
        // Returns Vector2 converted to Vector2Int according to default scaling factor (1000)
        public static Vector2Int Convert(Vector2 v)
        {
            return CG.Convert(v);
        }

        // Helper method provided to help you implement this file. Leave as is.
        // Returns float converted to int according to default scaling factor (1000)
        public static int Convert(float v)
        {
            return CG.Convert(v);
        }

        // Helper method provided to help you implement this file. Leave as is.
        // Returns true is segment AB intersects CD properly or improperly
        static public bool Intersects(Vector2Int a, Vector2Int b, Vector2Int c, Vector2Int d)
        {
            return CG.Intersect(a, b, c, d);
        }


        //Get the shortest distance from a point to a line
        //Line is defined by the lineStart and lineEnd points
        public static float DistanceToLineSegment(Vector2Int point, Vector2Int lineStart, Vector2Int lineEnd)
        {
            return CG.DistanceToLineSegment(point, lineStart, lineEnd);
        }

        // Helper method provided to help you implement this file. Leave as is.
        //Get the shortest distance from a point to a line
        //Line is defined by the lineStart and lineEnd points
        public static float DistanceToLineSegment(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
        {
            return CG.DistanceToLineSegment(point, lineStart, lineEnd);
        }


        // Helper method provided to help you implement this file. Leave as is.
        // Determines if a point is inside/on a CCW polygon and if so returns true. False otherwise.
        public static bool IsPointInPolygon(Vector2Int[] polyPts, Vector2Int point)
        {
            return CG.PointPolygonIntersectionType.Outside != CG.InPoly1(polyPts, point);
        }




        //Student code to build the path network from the given pathNodes and Obstacles
        //Obstacles - List of obstacles on the plane
        //agentRadius - the radius of the traversing agent
        //pathEdges - out parameter that will contain the edges you build.
        //  Edges cannot intersect with obstacles or boundaries. Edges must be at least agentRadius distance
        //  from all obstacle/boundary line segments

        public static void Create(Vector2 canvasOrigin, float canvasWidth, float canvasHeight,
            List<Polygon> obstacles, float agentRadius, List<Vector2> pathNodes, out List<List<int>> pathEdges)
        {

            //STUDENT CODE HERE
            Vector2Int convertedOrigin = Convert(canvasOrigin);
            int convertedWidth = Convert(canvasWidth);
            int convertedHeight = Convert(canvasHeight);


            pathEdges = new List<List<int>>(pathNodes.Count);
            
            // for each node in pathnodes
            for (int i = 0; i < pathNodes.Count; ++i)
            {
                List<int> pathEdge = new List<int>();
                int radiusOffset = Convert(agentRadius);
                Vector2Int coordsAdjA = Convert(pathNodes[i]) - convertedOrigin;
                Vector2Int rightBoundA = coordsAdjA + new Vector2Int(radiusOffset, 0);
                Vector2Int leftBoundA = coordsAdjA + new Vector2Int(-radiusOffset, 0);
                Vector2Int upperBoundA = coordsAdjA + new Vector2Int(0, radiusOffset);
                Vector2Int lowerBoundA = coordsAdjA + new Vector2Int(0, -radiusOffset);
                // skip node check if the node is outside of any boundaries
                if (rightBoundA.x >= convertedWidth || leftBoundA.x <= 0 || upperBoundA.y >= convertedHeight || lowerBoundA.y <= 0)
                {
                    pathEdges.Add(pathEdge);
                    continue;
                }
                // compare node at i to every other node aside from itself
                for (int j = 0; j < pathNodes.Count; ++j)
                {
                    if (pathNodes[i] == pathNodes[j])
                    {
                        continue;
                    }

                    // for each obstacle, check if obstacle is blocking the path between
                    // either the left or right sides of the nodes
                    Vector2Int upperA = coordsAdjA + new Vector2Int(0, radiusOffset);
                    Vector2Int lowerA = coordsAdjA + new Vector2Int(0, -radiusOffset);

                    Vector2Int nodeAUpperRight = coordsAdjA + new Vector2Int(radiusOffset, radiusOffset);
                    Vector2Int nodeAUpperLeft = coordsAdjA + new Vector2Int(-radiusOffset, radiusOffset);
                    Vector2Int nodeALowerRight = coordsAdjA + new Vector2Int(-radiusOffset, radiusOffset);
                    Vector2Int nodeALowerLeft = coordsAdjA + new Vector2Int(-radiusOffset, -radiusOffset);

                    Vector2Int coordsAdjB = Convert(pathNodes[j]) - convertedOrigin;
                    Vector2Int upperB = coordsAdjB + new Vector2Int(0, radiusOffset);
                    Vector2Int lowerB = coordsAdjB + new Vector2Int(0, -radiusOffset);
                    Vector2Int nodeBUpperRight = coordsAdjB + new Vector2Int(radiusOffset, radiusOffset);
                    Vector2Int nodeBUpperLeft = coordsAdjB + new Vector2Int(-radiusOffset, radiusOffset);
                    Vector2Int nodeBLowerRight = coordsAdjB + new Vector2Int(-radiusOffset, radiusOffset);
                    Vector2Int nodeBLowerLeft = coordsAdjB + new Vector2Int(-radiusOffset, -radiusOffset);

                    Vector2Int rightBoundB = coordsAdjB + new Vector2Int(radiusOffset, 0);
                    Vector2Int leftBoundB = coordsAdjB + new Vector2Int(-radiusOffset, 0);
                    Vector2Int upperBoundB = coordsAdjB + new Vector2Int(0, radiusOffset);
                    Vector2Int lowerBoundB = coordsAdjB + new Vector2Int(0, -radiusOffset);

                    if (rightBoundB.x >= convertedWidth || leftBoundB.x <= 0 || upperBoundB.y >= convertedHeight || lowerBoundB.y <= 0)
                    {
                        continue;
                    }

                    bool invalidPath = false;
                    // also check if any part of the node is inside of the obstacle
                    for (int k = 0; k < obstacles.Count; k++)
                    {
                        // otherwise, a path between node i and j should exist
                        Vector2Int[] points = obstacles[k].getIntegerPoints();
                        Vector2Int[] pointsAdj = new Vector2Int[points.Length];

                        // for each point in the polygon, generate adjusted points with offset added 
                        for (int l = 0; l < points.Length; l++)
                        {
                            Vector2Int adjustedL = points[l] - convertedOrigin;
                            pointsAdj.SetValue(adjustedL, l);

                            // check if any point in obstacle has a distance between two lines is less than diameter
                            // of the agent
                            float d1 = DistanceToLineSegment(adjustedL, upperA, upperB);
                            float d2 = DistanceToLineSegment(adjustedL, lowerA, lowerB);               

                            if (d1 + d2 <= Convert(agentRadius) * 2)
                            {                              
                                invalidPath = true;
                                break;
                            }

                            for (int m = 0; m < points.Length; m++)
                            {
                                Vector2Int adjustedM = points[m] - convertedOrigin;
                                if (adjustedL == adjustedM)
                                {
                                    continue;
                                }
                                // check intersection of line created by points L M
                                if (Intersects(adjustedL, adjustedM, coordsAdjA, coordsAdjB))
                                {
                                    invalidPath = true;
                                    break;
                                }
                            }                        
                        }

                        // if obstacle is blocking, break out of obstacle loop
                        if (invalidPath)
                        {
                            break;
                        }
                        // check if points of node A are in obstacle
                        if (IsPointInPolygon(pointsAdj, nodeAUpperRight) ||
                            IsPointInPolygon(pointsAdj, nodeAUpperLeft) ||
                            IsPointInPolygon(pointsAdj, nodeALowerRight) ||
                            IsPointInPolygon(pointsAdj, nodeALowerLeft))
                        {
                            // no point in checking B or other obstacles; break.
                            invalidPath = true;
                            break;
                        }
                        // check if points of node B are in obstacle
                        if (IsPointInPolygon(pointsAdj, nodeBUpperRight) ||
                            IsPointInPolygon(pointsAdj, nodeBUpperLeft) ||
                            IsPointInPolygon(pointsAdj, nodeBLowerRight) ||
                            IsPointInPolygon(pointsAdj, nodeBLowerLeft))
                        {
                            // no point in checking other obstacles; break.
                            invalidPath = true;
                            break;
                        }

                        if (invalidPath)
                        {
                            break;
                        }
                    }

                    if (invalidPath)
                    {
                        continue;
                    }
                    else
                    {
                        // by this point if invalidPath is still false, then path is valid.
                        pathEdge.Add(j);
                    }
                }

                // add the pathEdge set to pathEdges
                pathEdges.Add(pathEdge);
            }

            // END STUDENT CODE           

        }


    }

}