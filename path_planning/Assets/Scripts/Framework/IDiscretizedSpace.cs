using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public interface IDiscretizedSpace
{
    Obstacles Obstacles { get; }

    List<Vector2> PathNodes { get; }

    List<List<int>> PathEdges { get; }

    List<GameObject> PathNodeMarkers { get; }

    Bounds Boundary { get; }
    void Bake();
}
