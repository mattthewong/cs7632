using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

using GameAICourse;

namespace Tests
{
    public class GridTest
    {
        // You can run the tests in this class in the Unity Editor if you open
        // the Test Runner Window and choose the EditMode tab


        // Annotate methods with [Test] or [TestCase(...)] to create tests like this one!
        [Test]
        public void TestName()
        {
            // Tests are performed through assertions. You can Google NUnit Assertion
            // documentation to learn about them. Several examples follow.
            Assert.That(CreateGrid.StudentAuthorName, Is.Not.Contains("George P. Burdell"),
                "You forgot to change to your name!");
        }


        // You can write helper methods that are called by multiple tests!
        // This method is not itself a test because it is not annotated with [Test].
        // But look below for examples of calling it.
        void BasicGridCheck(bool [,] grid)
        {
            Assert.That(grid, Is.Not.Null);
            Assert.That(grid.Rank, Is.EqualTo(2), "grid is not a 2D array!");
        }


        // You can write parameterized tests for more efficient test coverage!
        // This single method can reflect an arbitrary number of test configurations
        // via the TestCase(...) syntax.
        // TODO You probably want some more test cases here
        [TestCase(0f, 0f, 1f, 1f, 1f)]
        [TestCase(0f, 0f, 1f, 1f, 0.25f)]
        public void TestEmptyGrid(float originx, float originy, float width, float height, float cellSize)
        {
            
            var origin = new Vector2(originx, originy);

            bool[,] grid;
            List<Vector2> pathNodes;
            List<List<int>> pathEdges;
            List<Polygon> obstPolys = new List<Polygon>();


            // Here is an example of testing code you are working on by calling it!
            CreateGrid.Create(origin, width, height, cellSize, obstPolys, out grid);

            // You could test this method in isolation by providing a hard-coded grid
            CreateGrid.CreatePathGraphFromGrid(origin, width, height, cellSize, GridConnectivity.FourWay, grid,
                    out pathNodes, out pathEdges);

            // There is that helper method in action
            BasicGridCheck(grid);


            // TODO Maybe these path tests should be in a helper method of their own...

            // TODO You should probably test for the correct grid size...

            Assert.That(pathNodes, Is.Not.Null);

            Assert.That(pathEdges, Is.Not.Null);
            Assert.That(pathEdges, Has.All.Not.Null);

            Assert.That(pathNodes.Count, Is.EqualTo(pathEdges.Count),
                "Every pathNode must have a pathEdge list!");

            Assert.That(grid, Has.All.True,
                "There aren't any obstacles to block the grid cells!");

            // TODO This method can be extended with more rigorous testing...

        }


        [TestCase(0f, 0f, 1f, 1f, 1f)]
        [TestCase(0f, 0f, 1f, 1f, 0.25f)]
        public void TestObstacleThatNearlyFillsCanvas(float originx, float originy,
            float width, float height, float cellSize)
        {

            var origin = new Vector2(originx, originy);

            bool[,] grid;
            List<Vector2> pathNodes;
            List<List<int>> pathEdges;
            List<Polygon> obstPolys = new List<Polygon>();

            // Let's make an obstacle in this test...

            Polygon poly = new Polygon();

            float offset = 0.1f;

            // Needs to be counterclockwise!
            Vector2[] pts =
                {
                    origin + Vector2.one * offset,
                    origin + new Vector2(width - offset, offset),
                    origin + new Vector2(width - offset, height - offset),
                    origin + new Vector2(offset, height-offset)
                };

            // There are different ways to approach test setup for tests.
            // I generally just assert things that I believe might be problematic.
            // I then add text like "SETUP FAILURE" so I know the problem is separate
            // from what I'm actually testing.

            Assert.That(CG.Ccw(pts), Is.True, "SETUP FAILURE: polygon verts not listed CCW");

            poly.SetPoints(pts);

            obstPolys.Add(poly);


            // Here is an example of testing code you are working on!
            CreateGrid.Create(origin, width, height, cellSize, obstPolys, out grid);

            // You could test this method in isolation by providing a hard-coded grid
            CreateGrid.CreatePathGraphFromGrid(origin, width, height, cellSize, GridConnectivity.FourWay, grid,
                    out pathNodes, out pathEdges);

            BasicGridCheck(grid);


            // TODO Maybe these path tests should be in a helper method...
            Assert.That(pathNodes, Is.Not.Null);

            Assert.That(pathEdges, Is.Not.Null);
            Assert.That(pathEdges, Has.All.Not.Null);

            Assert.That(pathNodes.Count, Is.EqualTo(pathEdges.Count),
                "Every pathNode must have a pathEdge list!");

            Assert.That(grid, Has.All.False,               
                "There is a big obstacle that should have blocked the entire grid!");

            // TODO This method can be extended with more rigorous testing...

        }


        // TODO I bet there is a lot more you want to write tests for!


    }
}
