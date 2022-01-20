using System.Collections;
using System.Collections.Generic;
using Priority_Queue;
using UnityEngine;
using System;


namespace GameAICourse
{


    public class AStarPathSearchImpl
    {

        // Please change this string to your name
        public const string StudentAuthorName = "Matthew Wong";


        // Null Heuristic for Dijkstra
        public static float HeuristicNull(Vector2 nodeA, Vector2 nodeB)
        {
            return 0f;
        }

        // Null Cost for Greedy Best First
        public static float CostNull(Vector2 nodeA, Vector2 nodeB)
        {
            return 0f;
        }



        // Heuristic distance fuction implemented with manhattan distance
        public static float HeuristicManhattan(Vector2 nodeA, Vector2 nodeB)
        {
            //STUDENT CODE HERE

            // The following code is just a placeholder so that the method has a valid return
            // You will replace it with the correct implementation
            return Math.Abs(nodeA.x - nodeB.x) + Math.Abs(nodeA.y - nodeB.y);

            //END CODE 
        }

        // Heuristic distance function implemented with Euclidean distance
        public static float HeuristicEuclidean(Vector2 nodeA, Vector2 nodeB)
        {
            //STUDENT CODE HERE

            // The following code is just a placeholder so that the method has a valid return
            // You will replace it with the correct implementation
            return (float)Math.Sqrt(Math.Pow(nodeB.x - nodeA.x, 2) + Math.Pow(nodeB.y - nodeA.y, 2));           
            //END CODE 
        }


        // Cost is only ever called on adjacent nodes. So we will always use Euclidean distance.
        // We could use Manhattan dist for 4-way connected grids and avoid sqrroot and mults.
        // But we will avoid that for simplicity.
        public static float Cost(Vector2 nodeA, Vector2 nodeB)
        {
            //STUDENT CODE HERE

            // The following code is just a placeholder so that the method has a valid return
            // You will replace it with the correct implementation

            // assume just Euclidean
            return HeuristicEuclidean(nodeA, nodeB);

            //END STUDENT CODE
        }



        public static PathSearchResultType FindPathIncremental(List<Vector2> nodes, List<List<int>> edges,
            CostCallback G,
            CostCallback H,
            int startNodeIndex, int goalNodeIndex,
            int maxNumNodesToExplore, bool doInitialization,
            ref int currentNodeIndex,
            ref Dictionary<int, PathSearchNodeRecord> searchNodeRecords,
            ref SimplePriorityQueue<int, float> openNodes, ref HashSet<int> closedNodes, ref List<int> returnPath)
        {
            PathSearchResultType pathResult = PathSearchResultType.InProgress;
            if (nodes == null || startNodeIndex >= nodes.Count || goalNodeIndex >= nodes.Count ||
                edges == null || startNodeIndex >= edges.Count || goalNodeIndex >= edges.Count ||
                edges.Count != nodes.Count ||
                startNodeIndex < 0 || goalNodeIndex < 0 ||
                maxNumNodesToExplore <= 0 ||
                (!doInitialization &&
                 (openNodes == null || closedNodes == null || currentNodeIndex < 0 ||
                  currentNodeIndex >= nodes.Count || currentNodeIndex >= edges.Count)))
            {
                searchNodeRecords = new Dictionary<int, PathSearchNodeRecord>();
                openNodes = new SimplePriorityQueue<int, float>();
                closedNodes = new HashSet<int>();

                return PathSearchResultType.InitializationError;
            }

            // STUDENT CODE HERE
            if (doInitialization)
            {
                currentNodeIndex = startNodeIndex;
                searchNodeRecords = new Dictionary<int, PathSearchNodeRecord>();
                openNodes = new SimplePriorityQueue<int, float>();
                closedNodes = new HashSet<int>();
                var firstNodeRecord = new PathSearchNodeRecord(currentNodeIndex);
                // set the estimated total cost.
                //firstNodeRecord.EstimatedTotalCost = H(nodes[currentNodeIndex], nodes[goalNodeIndex]);                
                // add to search records.
                searchNodeRecords.Add(firstNodeRecord.NodeIndex, firstNodeRecord);
                float startingPriority = 0f;
                // enqueue.
                openNodes.Enqueue(firstNodeRecord.NodeIndex, startingPriority);
                // should hold the final path either from start to goal (if goal can be reached), or from start to the node nearest goal. 
                returnPath = new List<int>();
            }

            int nodesProcessed = 0;
            // begin while loop.           
            while (nodesProcessed < maxNumNodesToExplore && openNodes.Count > 0)
            {
                //Find the smallest element in the open list using the estimated total cost
                var currentNodeRecord = searchNodeRecords[openNodes.First];
                currentNodeIndex = currentNodeRecord.NodeIndex;


                ++nodesProcessed;

                // goal check should be in edge expansion for better time performance!
                // However, doing so means goal found sooner. But if we are using DFS
                // we are probably intentionally looking for long paths...
                if (currentNodeIndex == goalNodeIndex)
                {
                    break;
                }

                PathSearchNodeRecord edgeNodeRecord = null;

                var currEdges = edges[currentNodeIndex];
                // loop through each connected edge.
                foreach (var edgeNodeIndex in currEdges)
                {
                    float endNodeHeuristic = 0f;

                    // estimate cost so far.
                    var costToEdgeNode = currentNodeRecord.CostSoFar + G(nodes[currentNodeIndex], nodes[edgeNodeIndex]);

                    var dist = Vector2.Distance(nodes[edgeNodeIndex], nodes[goalNodeIndex]);
     
                    if (closedNodes.Contains(edgeNodeIndex))
                    {
                        edgeNodeRecord = searchNodeRecords[edgeNodeIndex];
                        // if we didn't find a shorter route, skip.
                        if (edgeNodeRecord.CostSoFar <= costToEdgeNode)
                        {                            
                            continue;
                        }
                        // otherwise, remove it from the closed list.
                        closedNodes.Remove(edgeNodeIndex);
                        // calculate the heuristic for the node.
                        endNodeHeuristic = edgeNodeRecord.EstimatedTotalCost - edgeNodeRecord.CostSoFar;

                    }
                    else if (openNodes.Contains(edgeNodeIndex))
                    {
                        edgeNodeRecord = searchNodeRecords[edgeNodeIndex];
                        // if we didn't find a shorter route, skip.
                        if (edgeNodeRecord.CostSoFar <= costToEdgeNode)
                        {
                            continue;
                        }
                        // again, calculate heuristic for the node.
                        endNodeHeuristic = edgeNodeRecord.EstimatedTotalCost - edgeNodeRecord.CostSoFar;
                    }
                    else
                    {
                        // we have an unvisited node.
                        edgeNodeRecord = new PathSearchNodeRecord(edgeNodeIndex);
                        // we need to use the heuristic estimate function, since we don't have an existing record to use.
                        endNodeHeuristic = H(nodes[edgeNodeIndex], nodes[goalNodeIndex]);
                    }

                    // update the from node index.
                    edgeNodeRecord.FromNodeIndex = currentNodeIndex;
                    // update the CostSoFar.
                    edgeNodeRecord.CostSoFar = costToEdgeNode;
                    // update the estimated total cost.
                    edgeNodeRecord.EstimatedTotalCost = costToEdgeNode + endNodeHeuristic;
                    // add the node to searchNodeRecords.
                    searchNodeRecords[edgeNodeIndex] = edgeNodeRecord;

                    /// Add it to the open list.
                    if (!openNodes.Contains(edgeNodeIndex))
                    {
                        openNodes.Enqueue(edgeNodeIndex, edgeNodeRecord.EstimatedTotalCost);
                    } else
                    {
                        openNodes.UpdatePriority(edgeNodeIndex, edgeNodeRecord.EstimatedTotalCost);
                    }                 
                } //foreach() edge processing of current node               

                openNodes.Remove(currentNodeIndex);
                closedNodes.Add(currentNodeIndex);
            } //while            

            //if (openNodes.Count)
            Debug.Log("OPENNODES: " + openNodes.Count);
            Debug.Log("CLOSED NODES: " + closedNodes.Count);

            // if no nodes left in open set and we have not reached the goal:
            if (openNodes.Count <= 0 && currentNodeIndex != goalNodeIndex)
            {
                pathResult = PathSearchResultType.Partial;
                //find the closest node we looked at and use for partial path
                int closest = -1;
                float closestDist = float.MaxValue;
                foreach (var n in closedNodes)
                {
                    var nrec = searchNodeRecords[n];

                    // Hmmm. I bet if we were using this partial path code with an A* implementation 
                    // we could avoid calculating Euclidean distance again using cached metadata.
                    // (But we would need to deduce whether said code was running in Dijkstra
                    // mode with a zero constant Heuristic() func)
                    // Otherwise, we must calculate the distance
                    var d = Vector2.Distance(nodes[nrec.NodeIndex], nodes[goalNodeIndex]);
                    if (d < closestDist)
                    {
                        closest = n;
                        closestDist = d;
                    }
                }
                if (closest >= 0)
                {
                    currentNodeIndex = closest;
                }
            }
            else if (currentNodeIndex == goalNodeIndex)
            {
                pathResult = PathSearchResultType.Complete;
            }


            if (pathResult != PathSearchResultType.InProgress)
            {
                // processing complete, a path (possibly partial) can be generated returned
                returnPath = new List<int>();
                while (currentNodeIndex != startNodeIndex)
                {
                    returnPath.Add(currentNodeIndex);
                    currentNodeIndex = searchNodeRecords[currentNodeIndex].FromNodeIndex;
                }
                returnPath.Add(startNodeIndex);
                returnPath.Reverse();
            }

            return pathResult;

        }

    }

}