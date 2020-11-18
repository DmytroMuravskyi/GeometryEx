﻿using System;
using System.Linq;
using System.Collections.Generic;
using Elements.Geometry;

namespace GeometryEx
{
    /// <summary>
    /// Extends Elements.Geometry.Mesh with utility methods.
    /// </summary>
    public static class MeshEx
    {
        /// <summary>
        /// Returns the Mesh Triangle(s) that border the supplied Line edge.
        /// </summary>
        /// <param name="edge">A Line representing a Mesh edge.</param>
        /// <returns>
        /// A List of Triangles.
        /// </returns>
        public static List<Elements.Geometry.Triangle> AdjacentTriangles(this Mesh mesh, Line edge)
        {
            var triangles = new List<Elements.Geometry.Triangle>();
            foreach (var triangle in mesh.Triangles)
            {
                var points = new List<Vector3>();
                triangle.Vertices.ToList().ForEach(v => points.Add(v.Position));
                if (points.Contains(edge.Start) && points.Contains(edge.End))
                {
                    triangles.Add(triangle);
                }
            }
            return triangles;
        }

        /// <summary>
        /// Returns the Mesh Triangle(s) that border the supplied Triangle.
        /// </summary>
        /// <param name="triangle">A Mesh triangle.</param>
        /// <returns>
        /// A List of Triangles.
        /// </returns>
        public static List<Elements.Geometry.Triangle> AdjacentTriangles(this Mesh mesh, Elements.Geometry.Triangle triangle)
        {
            var triangles = new List<Elements.Geometry.Triangle>();
            var triPoints = new List<Vector3>();
            triangle.Vertices.ToList().ForEach(v => triPoints.Add(v.Position));
            foreach (var triang in mesh.Triangles)
            {
                var common = 0;
                var points = new List<Vector3>();
                triang.Vertices.ToList().ForEach(v => points.Add(v.Position));
                points.ForEach(p => common += p.Occurs(triPoints));
                if (common == 2)
                { 
                    triangles.Add(triang);
                }
            }
            return triangles;
        }

        /// <summary>
        /// Returns the aggregate area of all Mesh triangles.
        /// </summary>
        /// <param name="mesh"></param>
        /// <returns>
        /// A double.
        /// </returns>
        public static double Area(this Mesh mesh)
        {
            var area = 0.0;
            mesh.Triangles.ToList().ForEach(t => area += t.Area());
            return area;
        }

        /// <summary>
        /// Averages the vectors terminating at a Mesh Vertex..
        /// </summary>
        /// <param name="point">A Vector3 at the same position as a Vertex on the Mesh.</param>
        /// <returns>
        /// A Vector3 average.
        /// </returns>
        public static Vector3 AverageAt(this Mesh mesh, Vector3 point)
        {
            var average = new Vector3(double.MaxValue, double.MaxValue, double.MaxValue);
            if (point.Occurs(mesh.Points()) == 0) //Supplied point is not a mesh point.
            {
                return average;
            }
            var edges = EdgesAt(mesh, point);
            var vectors = new List<Vector3>();
            edges.ForEach(e => vectors.Add(new Vector3(e.End.X - e.Start.X, e.End.Y - e.Start.Y, e.End.Z - e.Start.Z).Unitized()));
            average = vectors[0].Average(vectors[1]);
            for (int i = 2; i < vectors.Count; i++)
            {
                average = average.Average(vectors[i]);
            }
            var avg = average.Unitized();
            return avg;
        }

        /// <summary>
        /// Calculates whether a supplied edge of the Mesh is a valley relative to its adjacent triangles and a supplied comparison vector.
        /// </summary>
        /// <param name="edge">A Line representing an edge of this Mesh.</param>
        /// <param name="compareTo">A vector to compare adjacent triangle normals.</param>
        /// <returns>
        /// A List of Lines.
        /// </returns>

        public static List<Line> Concavities(this Mesh mesh, Vector3 compareTo)
        {            
            var iEdges = mesh.EdgesInterior();
            var lowLines = new List<Line>();
            foreach (var edge in iEdges)
            {
                var triPoints = mesh.ThirdPoints(edge);
                var plane1 = new Plane(triPoints.First(), compareTo);
                var plane2 = new Plane(triPoints.Last(), compareTo);
                if (plane1.SignedDistanceTo(edge.Start) <= 0.0 &&
                    plane1.SignedDistanceTo(edge.End) <= 0.0 &&
                    plane2.SignedDistanceTo(edge.Start) <= 0.0 &&
                    plane2.SignedDistanceTo(edge.End) <= 0.0)
                {
                    lowLines.Add(edge);
                }
            }
            var pEdges = mesh.EdgesPerimeters();
            var lowPerims = new List<Line>();
            foreach (var lines in pEdges)
            {
                foreach (var edge in lines)
                {
                    var triPoint = mesh.ThirdPoints(edge).First();
                    var plane = new Plane(triPoint, compareTo);
                    if (plane.SignedDistanceTo(edge.Start) <= 0.0 &&
                        plane.SignedDistanceTo(edge.End) <= 0.0)
                    {
                        lowPerims.Add(edge);
                    }
                }
            }
            foreach (var edge in lowPerims)
            {
                if (edge.SharesEndpointWith(lowLines).Count == 0)
                {
                    lowLines.Add(edge);
                }
            }
            return lowLines;
        }


        /// <summary>
        /// Returns the unique edges of a Mesh as a List of Lines.
        /// </summary>
        /// <returns>A List of Lines.</returns>
        public static List<Line> Edges(this Mesh mesh)
        {
            var edges = new List<Line>();
            mesh.Triangles.ToList().ForEach(t => edges.AddRange(t.Edges().Where(e => e.Occurs(edges) == 0)));
            return edges;
        }

        /// <summary>
        /// Returns the unique edges intersecting a supplied point of the Mesh, or an empty list if the supplied point is not at the same position as a Mesh Vertex.
        /// </summary>
        /// <param name="point">A Vector3 at the same position as a Vertex on the Mesh.</param>
        /// <param name="orient">If true, returns lines whose end point is at the supplied point.</param>
        /// <returns>
        /// A List of Lines.
        /// </returns>
        public static List<Line> EdgesAt(this Mesh mesh, Vector3 point, bool orient = true)
        {
            var lines = new List<Line>();
            var points = mesh.Points();
            if (!points.Any(p => p.IsAlmostEqualTo(point)))
            {
                return lines;
            }
            mesh.Triangles.ToList().ForEach(t => lines.AddRange(t.Edges().Where(e => !e.IsListed(lines))));
            lines = lines.Where(e => e.Start.IsAlmostEqualTo(point) || e.End.IsAlmostEqualTo(point)).ToList();
            if (!orient)
            {
                return lines;
            }
            var edges = new List<Line>();
            foreach (var line in lines)
            {
                if (line.End.IsAlmostEqualTo(point))
                {
                    edges.Add(line);
                    continue;
                }
                edges.Add(line.Reverse());
            }
            return edges;
        }

        /// <summary>
        /// Returns a list of interior edges from a Mesh, treating all half-edges as full edges.
        /// </summary>
        /// <returns>
        /// A List of Lines.
        /// </returns>
        public static List<Line> EdgesInterior(this Mesh mesh)
        {
            var edges = new List<Line>();
            mesh.Triangles.ToList().ForEach(t => edges.AddRange(t.Edges()));
            var iEdges = edges.Where(e => e.Occurs(edges) == 2).ToList();
            edges.Clear();
            foreach (var edge in iEdges)
            {
                if (edge.Occurs(edges) == 0)
                {
                    edges.Add(edge);
                }
            }
            return edges;
        }

        /// <summary>
        /// Returns ordered lists of perimeter edges from a Mesh. There is no guarantee of directionality, only contiguity of returned lines.
        /// </summary>
        /// <returns>
        /// A List of Lines.
        /// </returns>
        public static List<List<Line>> EdgesPerimeters(this Mesh mesh)
        {
            var edges = new List<Line>();
            mesh.Triangles.ToList().ForEach(t => edges.AddRange(t.Edges()));
            var pEdges = edges.Where(e => e.Occurs(edges) == 1).ToList();
            var perimeters = new List<List<Line>>();
            while (pEdges.Count() > 0)
            {
                var edge = pEdges.First();
                pEdges = pEdges.Skip(1).ToList();
                var perimeter = new List<Line> { edge };
                var connected = pEdges.Where(e => e.Start.IsAlmostEqualTo(edge.End) || e.End.IsAlmostEqualTo(edge.End)).ToList();
                while (connected.Count() > 0)
                {
                    pEdges.Remove(connected.First());
                    edge = new Line(edge.End, edge.End.FarthestFrom(connected.First().Points()));
                    perimeter.Add(edge);
                    connected = pEdges.Where(e => e.Start.IsAlmostEqualTo(edge.End) || e.End.IsAlmostEqualTo(edge.End)).ToList();
                }
                perimeters.Add(perimeter);
            }
            if (perimeters.Count == 1)
            {
                return perimeters;
            }
            return perimeters.OrderByDescending(p => Shaper.TotalLength(p)).ToList();
        }

        /// <summary>
        /// Calculates whether a supplied edge of the Mesh is a valley relative to its adjacent triangles and a supplied comparison vector.
        /// </summary>
        /// <param name="edge">A Line representing an edge of this Mesh.</param>
        /// <param name="compareTo">A vector to compare adjacent triangle normals.</param>
        /// <returns></returns>
        public static bool IsConcavity(this Mesh mesh, Line edge, Vector3 compareTo)
        {
            if (edge.Occurs(mesh.Edges()) == 0) //Line is not a Mesh edge.
            {
                return false;
            }
            var points = mesh.ThirdPoints(edge);
            var plane = new Plane(points.First(), compareTo);
            var ends = edge.Points();
            var valley = 0;
            if (plane.SignedDistanceTo(ends.First()) <= 0.0 &&
                plane.SignedDistanceTo(ends.Last()) <= 0.0)
            {
                valley++;
            }
            if (points.Count == valley)
            {
                return true;
            }
            plane = new Plane(points.Last(), compareTo);
            if (plane.SignedDistanceTo(ends.First()) <= 0.0 &&
                plane.SignedDistanceTo(ends.Last()) <= 0.0)
            {
                valley++;
            }
            if (points.Count == valley)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns whether all normals of this Mesh's triangles are equivalent.
        /// </summary>
        /// <param name="mesh"></param>
        /// <returns>
        /// True if the absolute value of all normals is equivalent.
        /// </returns>
        public static bool IsFlat(this Mesh mesh)
        {
            var triangles = mesh.Triangles;
            var idx = triangles.First().Normal;
            var index = new Vector3(Math.Abs(idx.X), Math.Abs(idx.Y), Math.Abs(idx.Z));
            foreach (var triangle in triangles)
            {
                var vtx = triangle.Normal;
                var compare = new Vector3(Math.Abs(vtx.X), Math.Abs(vtx.Y), Math.Abs(vtx.Z));
                if (!index.IsAlmostEqualTo(compare))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Returns a list of distict points in the Mesh.
        /// </summary>
        /// <param name="mesh"></param>
        /// <returns>
        /// A Vector3 List.
        /// </returns>
        public static List<Vector3> Points(this Mesh mesh)
        {
            var triangles = mesh.Triangles;
            var points = new List<Vector3>();
            triangles.ToList().ForEach(t => points.AddRange(t.Points()));
            return points.Distinct().ToList();
        }

        /// <summary>
        /// Returns the perimeter Vector3 points of the Mesh in CCW order.
        /// </summary>
        /// <param name="mesh"></param>
        /// <returns>
        /// A List of Vector3 points.
        /// </returns>
        public static List<Vector3> PointsBoundary(this Mesh mesh)
        {
            var pPoints = new List<Vector3>();
            var edges = EdgesPerimeters(mesh);
            if (edges.Count == 0)
            {
                return pPoints;
            }
            edges.First().ForEach(e => pPoints.Add(e.Start));
            return pPoints;
        }

        /// <summary>
        /// Returns the list of Mesh interior Vector3 points.
        /// </summary>
        /// <returns>
        /// A List of Vector3 points.
        /// </returns>
        public static List<Vector3> PointsInterior(this Mesh mesh)
        {
            var pPoints = mesh.PointsBoundary();
            return mesh.Points().Where(p => p.Occurs(pPoints) == 0).ToList();
        }

        public enum Normal { X, Y, Z }
        /// <summary>
        /// Returns a Plane for every distinct specified coordinate in this Mesh.
        /// </summary>
        /// <returns>
        /// A List of Planes in ascending axis order.
        /// </returns>
        public static List<Plane> Planes(this Mesh mesh, Normal axis)
        {
            var slices = new List<double>();
            var planes = new List<Plane>();
            var points = new List<Vector3>();
            mesh.Triangles.ToList().ForEach(t => t.Vertices.ToList().ForEach(v => points.Add(v.Position)));
            switch (axis)
            {
                case Normal.X:
                    points.ForEach(v => slices.Add(v.X));
                    slices.Distinct().ToList().ForEach(s => planes.Add(new Plane(new Vector3(s, 0.0, 0.0), Vector3.XAxis)));
                    planes = planes.OrderBy(p => p.Origin.X).ToList();
                    break;
                case Normal.Y:
                    points.ForEach(v => slices.Add(v.Y));
                    slices.Distinct().ToList().ForEach(s => planes.Add(new Plane(new Vector3(0.0, s, 0.0), Vector3.YAxis)));
                    planes = planes.OrderBy(p => p.Origin.Y).ToList();
                    break;
                case Normal.Z:
                    points.ForEach(v => slices.Add(v.Z));
                    slices.Distinct().ToList().ForEach(s => planes.Add(new Plane(new Vector3(0.0, 0.0, s), Vector3.ZAxis)));
                    planes = planes.OrderBy(p => p.Origin.Z).ToList();
                    break;
            }
            return planes; ;
        }

        //public static Slope GetSlopeAtEdge(this Mesh mesh, Line edge, Vector3 compareTo)
        //{
        //    var points = ThirdPoints(mesh, edge);
        //}

        /// <summary>
        /// Returns the third points of triangles from one delivered edge.
        /// </summary>
        /// <param name="edge">A Line representing a Mesh edge.</param>
        /// <returns>
        /// A List of Vector3 points.
        /// </returns>
        public static List<Vector3> ThirdPoints(this Mesh mesh, Line edge)
        {
            var thirdPoints = new List<Vector3>();
            foreach (var triangle in mesh.Triangles)
            {
                var points = new List<Vector3>();
                triangle.Vertices.ToList().ForEach(v => points.Add(v.Position));
                if (points.Contains(edge.Start) && points.Contains(edge.End))
                {
                    thirdPoints.AddRange(points.Where(p => !edge.Start.IsAlmostEqualTo(p) && !edge.End.IsAlmostEqualTo(p)));
                }
            }
            return thirdPoints;
        }

        public struct Vertex
        {
            public int index;
            public bool isBoundary;
            public Vector3 position;
        }

        public struct IndexedVertices
        {
            public List<List<int>> triangles;
            public List<Vertex> vertices;
        }
        /// <summary>
        /// 
        /// NOTE: ELIMINATES HOLES IN THE MESH.
        /// TODO: PRESERVE HOLES.
        /// </summary>
        /// <param name="mesh"></param>
        /// <returns>
        /// A List of IndexedVertices.
        /// </returns>
        public static IndexedVertices ToIndexedVertices(this Mesh mesh)
        {
            IndexedVertices indexedVertices;
            indexedVertices.triangles = new List<List<int>>();
            indexedVertices.vertices = new List<Vertex>();
            var i = 0;
            foreach(var point in mesh.PointsBoundary())
            {
                indexedVertices.vertices.Add(
                    new Vertex
                    {
                        index = i,
                        isBoundary = true,
                        position = point
                    });
                i++;
            }
            foreach (var point in mesh.PointsInterior())
            {
                indexedVertices.vertices.Add(
                    new Vertex
                    {
                        index = i,
                        isBoundary = false,
                        position = point
                    });
                i++;
            }
            foreach(var triangle in mesh.Triangles)
            {
                var indices = new List<int>();
                triangle.Vertices.ToList().ForEach(
                    p => indices.Add(indexedVertices.vertices.First(v => v.position.IsAlmostEqualTo(p.Position)).index));
                indexedVertices.triangles.Add(indices);
            }
            return indexedVertices;
        }
    }
}
