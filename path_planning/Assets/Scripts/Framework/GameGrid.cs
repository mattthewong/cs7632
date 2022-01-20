//#define SAVE_CASES

using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

using GameAICourse;

public class GameGrid : DiscretizedSpaceMonoBehavior
{

    public float CellSize = 0.2f;

    public GridConnectivity gridConnectivity = GridConnectivity.FourWay;

    public GridConnectivity GridConn
    {
        get
        {
            return gridConnectivity;
        }
    }

    public bool[,] Grid { get; protected set; }

    public Color LineColor = Color.green;
    public Color BlockedLineColor = Color.blue;


    public bool VisualizePathNetwork = false;

    public override void Awake()
    {
        base.Awake();

        Obstacles = this.GetComponent<Obstacles>();
    }

    // Start is called before the first frame update
    public override void Start()
    {
        base.Start();

        Obstacles.Init();

        Bake();

        Utils.DisplayName("CreateGrid", CreateGrid.StudentAuthorName);

    }

#if SAVE_CASES
    int caseCount = 0;
#endif

    public bool UseHardCodedCases = false;

    override public void Bake()
    {
        base.Bake();

        bool[,] grid;
        List<Vector2> pathNodes;
        List<List<int>> pathEdges;

        var obst = Obstacles.getObstacles();

        var polys = new List<Polygon>(obst.Count);

        for(int i=0; i < obst.Count; ++i)
        {
            polys.Add(obst[i].GetPolygon());
        }

        GridCase overrideCase = null;

        if (UseHardCodedCases)
        {
            var gct = new GridCase(0, BottomLeftCornerWCS, Boundary.size.x, Boundary.size.z, CellSize, polys, gridConnectivity);

            overrideCase = HardCodedGridCases.FindCase(gct);
        }

        if (overrideCase != null)
        {
            grid = overrideCase.grid;
            pathNodes = overrideCase.pathNodes;
            pathEdges = overrideCase.pathEdges;
        }
        else
        {

            //pathNodes = new List<Vector2>();
            //pathEdges = new List<List<int>>();
            //grid = new bool[1, 1];
            //grid[0, 0] = true;

            CreateGrid.Create(BottomLeftCornerWCS, Boundary.size.x, Boundary.size.z, CellSize,
                                polys, out grid);

            CreateGrid.CreatePathGraphFromGrid(BottomLeftCornerWCS, Boundary.size.x, Boundary.size.z, CellSize, gridConnectivity,
                            grid, out pathNodes, out pathEdges);
        }



#if SAVE_CASES

        GridCase gc = new GridCase(caseCount++, BottomLeftCornerWCS, Boundary.size.x, Boundary.size.z, CellSize, polys, GridConn, grid, pathNodes, pathEdges);

        using (var OutFile = new StreamWriter("cases.txt", true))
        {

            OutFile.WriteLine(gc);
        }

#endif


        Grid = grid;
        PathNodes = pathNodes;
        PathEdges = pathEdges;

        PurgeOutdatedLineViz();

        if (grid != null)
        {
            CreateGridLines(Grid, CellSize, BottomLeftCornerWCS);
        }

        if (VisualizePathNetwork)
        {
            if (PathNodes != null && PathEdges != null)
                CreateNetworkLines(PathOverlay_OffsetFromFarCP);
        }

        GridOverlayCamera.clearFlags = CameraClearFlags.SolidColor;
        GridOverlayCamera.Render();
        GridOverlayCamera.clearFlags = CameraClearFlags.Nothing;

        PathOverlayCamera.clearFlags = CameraClearFlags.SolidColor;
        PathOverlayCamera.Render();
        PathOverlayCamera.clearFlags = CameraClearFlags.Nothing;

        DisableLineViz();

    }

    void DisableLineViz()
    {

        var linegroup = this.transform.Find(Utils.LineGroupName);

        if (linegroup != null)
        {
            linegroup.gameObject.SetActive(false);
        }
    }


    void PurgeOutdatedLineViz()
    {

        var linegroup = this.transform.Find(Utils.LineGroupName);

        if (linegroup != null)
        {
            linegroup.name = "MARKED_FOR_DELETION";
            linegroup.gameObject.SetActive(false);
            Destroy(linegroup.gameObject);
        }
    }

    //
    // Draws a square for each box of the grid that exists
    // size = This is the number of cells in the grid
    // canvas_pos = This is the where the top left corner of the canvas is
    // lengthX, lengthY = this is the width and height of the plane
    //


    void CreateGridLines(bool[,] grid, float cellSize, Vector2 canvas_pos)
    {
        //PurgeOutdatedLineViz();

        var offset = GridOverlay_OffsetFromFarCP;
        var offset2 = GridOverlay_OffsetFromFarCP - 0.01f;

        var parent = Utils.FindOrCreateGameObjectByName(this.gameObject, Utils.LineGroupName);

        float width = grid.GetLength(0) * cellSize;
        float height = grid.GetLength(1) * cellSize;

        for (int i = 0; i <= grid.GetLength(0); i++)
        {
            Utils.DrawLine(canvas_pos + new Vector2(i * cellSize, 0f), canvas_pos + new Vector2(i * cellSize, height), offset, parent, LineColor, LineMaterial);
        }

        for (int j = 0; j <= grid.GetLength(1); j++)
        {
            Utils.DrawLine(canvas_pos + new Vector2(0f, j * cellSize), canvas_pos + new Vector2(width, j * cellSize), offset, parent, LineColor, LineMaterial);
        }

        for (int i = 0; i < grid.GetLength(0); i++)
        {
            for (int j = 0; j < grid.GetLength(1); j++)
            {

                float lX = i * cellSize;
                float rX = (i + 1) * cellSize;
                float tY = j * cellSize;
                float bY = (j + 1) * cellSize;

                if (!grid[i, j])
                {
                    Utils.DrawLine(canvas_pos + new Vector2(lX, tY), canvas_pos + new Vector2(rX, bY), offset2, parent, BlockedLineColor, LineMaterial);
                    Utils.DrawLine(canvas_pos + new Vector2(rX, tY), canvas_pos + new Vector2(lX, bY), offset2, parent, BlockedLineColor, LineMaterial);
                }

            }
        }


    }




    //
    // Returns cell location corresponding to a particular point
    //
    public Vector2 FindGridCellForPoint(Vector2 point)
    {
        Vector2 diff = point - BottomLeftCornerWCS;
        return new Vector2(diff.x / CellSize, diff.y / CellSize);
    }

    /*
     * Returns the location of the center point of a grid cell
     */
    public Vector2 FindCenterOfGridCell(int i, int j)
    {
        Vector2 local = new Vector2(i * CellSize, j * CellSize);
        local = local + new Vector2(CellSize / 2f, CellSize / 2f);
        return local + BottomLeftCornerWCS;
    }



}
