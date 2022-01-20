//#define SAVE_CASES

using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

using GameAICourse;

/*
 * Class that holds all information about the path network
 *
 */
public class PathNetwork : DiscretizedSpaceMonoBehavior
{

    private string PathNodeMarkersGroupName = "PathNodeMarkersGroup";

    //public GameObject PathNodeMarkersGroup;
    public MoveBall moveBall;
    //material for drawing the path network line
    //public Material LineMaterial;
    public Color LineColor = Color.green;


    public override void Awake()
    {
        base.Awake();

        Obstacles = this.GetComponent<Obstacles>();
    }


    public override void Start()
    {
        base.Start();

        Obstacles.Init();

        Bake();

        Utils.DisplayName("CreatePathNetwork", CreatePathNetwork.StudentAuthorName);
    }


    //private void Update()
    //{
    //    Utils.DisplayName("CreatePathNetwork", CreatePathNetwork.StudentAuthorName);
    //}

    //Get all child node cubes and create their corresponding path node vectors
    void Init()
    {

        var PathNodeMarkersGroup = GameObject.Find(PathNodeMarkersGroupName);

        if (PathNodeMarkersGroup != null)
        {

            PathNodeMarkers = new List<GameObject>(PathNodeMarkersGroup.transform.childCount);

            for (int i = 0; i < PathNodeMarkersGroup.transform.childCount; ++i)
            {
                PathNodeMarkers.Add(PathNodeMarkersGroup.transform.GetChild(i).gameObject);
            }

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



    //void CreateNetworkLines()
    //{
    //    PurgeOutdatedLineViz();

    //    var parent = Utils.FindOrCreateGameObjectByName(this.gameObject, Utils.LineGroupName);


    //    HashSet<System.Tuple<int, int>> hs = new HashSet<System.Tuple<int, int>>();


    //    if (PathEdges != null)
    //    {
    //        for (int i = 0; i < PathEdges.Count; ++i)
    //        {
    //            var pts = PathEdges[i];
    //            if (pts != null)
    //            {
    //                for (int j = 0; j < pts.Count; ++j)
    //                {
    //                    var smaller = i;
    //                    var bigger = pts[j];

    //                    if (bigger < smaller)
    //                    {
    //                        var tmp = bigger;
    //                        bigger = smaller;
    //                        smaller = tmp;
    //                    }

    //                    var tup = new System.Tuple<int, int>(smaller, bigger);
    //                    if (!hs.Contains(tup))
    //                    {
    //                        hs.Add(tup);
    //                        Utils.DrawLine(PathNodes[i], PathNodes[pts[j]], Utils.ZOffset, parent, Color.green, LineMaterial);
    //                    }

    //                }
    //            }
    //        }
    //    }
    //}



#if SAVE_CASES
    int caseCount = 0;
#endif

    public bool UseHardCodedCases = false;


    public override void Bake()
    {
        base.Bake();

        Init();

        PathNodes = new List<Vector2>(PathNodeMarkers.Count);

        for (int i = 0; i < PathNodeMarkers.Count; i++)
        {
            PathNodes.Add(new Vector2(PathNodeMarkers[i].transform.position.x, PathNodeMarkers[i].transform.position.z));
        }

        var obst = Obstacles.getObstacles();

        var polys = new List<Polygon>(obst.Count);

        for (int i = 0; i < obst.Count; ++i)
        {
            polys.Add(obst[i].GetPolygon());
        }



        List<List<int>> pathEdges;



        PathNetworkCase overrideCase = null;

        if (UseHardCodedCases)
        {
            var pnt = new PathNetworkCase(0, BottomLeftCornerWCS, Boundary.size.x, Boundary.size.z, polys, moveBall.Radius, PathNodes);

            overrideCase = HardCodedPathNetworkCases.FindCase(pnt);
        }

        if (overrideCase != null)
        {
            PathEdges = overrideCase.pathEdges;
        }
        else
        {

            //pathEdges = new List<List<int>>(PathNodes.Count);

            //for (int i = 0; i < PathNodes.Count; ++i)
            //{
            //    pathEdges.Add(new List<int>());
            //}
            //PathEdges = pathEdges;


            CreatePathNetwork.Create(BottomLeftCornerWCS, Boundary.size.x, Boundary.size.z,
                                        polys, moveBall.Radius, PathNodes, out pathEdges);

            PathEdges = pathEdges;

        }


#if SAVE_CASES

        PathNetworkCase gc = new PathNetworkCase(caseCount++, BottomLeftCornerWCS, Boundary.size.x, Boundary.size.z, polys, moveBall.Radius, PathNodes, PathEdges);

        using (var OutFile = new StreamWriter("cases.txt", true))
        {

            OutFile.WriteLine(gc);
        }

#endif



        PurgeOutdatedLineViz();
        CreateNetworkLines(Utils.ZOffset);

    }

}
