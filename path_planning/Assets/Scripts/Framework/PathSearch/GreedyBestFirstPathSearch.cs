﻿using System.Collections;
using System.Collections.Generic;
using Priority_Queue;
using UnityEngine;

using GameAICourse;

public class GreedyBestFirstPathSearch : PathSearchProvider
{

    private static PathSearchProvider instance;
    public static PathSearchProvider Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new GreedyBestFirstPathSearch();
            }
            return instance;
        }
    }



    override public PathSearchResultType FindPathIncremental(List<Vector2> nodes, List<List<int>> edges, bool useManhattan,
    int startNodeIndex, int goalNodeIndex, int maxNumNodesToExplore, bool doInitialization, ref int currentNodeIndex, ref Dictionary<int, PathSearchNodeRecord> searchNodeRecords, ref SimplePriorityQueue<int, float> openNodes, ref HashSet<int> closedNodes, ref List<int> returnPath)
    {

        CostCallback h;

        if (useManhattan)
            h = AStarPathSearchImpl.HeuristicManhattan;
        else
            h = AStarPathSearchImpl.HeuristicEuclidean;

        return AStarPathSearchImpl.FindPathIncremental(nodes, edges, AStarPathSearchImpl.CostNull, h, startNodeIndex, goalNodeIndex, maxNumNodesToExplore, doInitialization,
            ref currentNodeIndex, ref searchNodeRecords, ref openNodes, ref closedNodes, ref returnPath);

    }

}
