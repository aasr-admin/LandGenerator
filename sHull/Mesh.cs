using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace DelaunayTriangulator
{
  public class Mesh
  {
    private List<Vertex> m_delaunayVertices = null;
    private List<Vertex> m_voronoiVertices = new List<Vertex>();
    private List<Edge> m_edges = new List<Edge>();
    private List<VoronoiPolygon> m_voronoiPolygons = new List<VoronoiPolygon>();
    private List<DelaunayTriangle> m_delaunayTriangles = new List<DelaunayTriangle>();

    public List<Vertex> VoronoiVertices { get { return m_voronoiVertices; } set { m_voronoiVertices = value; } }
    public List<Vertex> DelaunayVertices { get { return m_delaunayVertices; } set { m_delaunayVertices = value; } }
    public List<Edge> Edges { get { return m_edges; } set { m_edges = value; } }
    public List<VoronoiPolygon> VoronoiPolygons { get { return m_voronoiPolygons; } set { m_voronoiPolygons = value; } }
    public List<DelaunayTriangle> DelaunayTriangles { get { return m_delaunayTriangles; } set { m_delaunayTriangles = value; } }


    public Mesh(List<Vertex> startingPoints)
    {
      m_delaunayVertices = startingPoints;
    }

    Stopwatch m_watch = new Stopwatch();


    public void ProcessPointsIntoMesh()
    {
      Triangulator angulator = new Triangulator();

      m_watch.Start();
      //100 milliseconds to triangulate 8000 vertices
      List<Triad> triangles = angulator.Triangulation(m_delaunayVertices);
      m_watch.Stop();

      m_watch.Reset();
      m_watch.Start();
      //6 milliseconds to build up the triangle objects
      foreach (Triad t in triangles)
      {
        m_delaunayTriangles.Add(new DelaunayTriangle(m_delaunayVertices[t.a], m_delaunayVertices[t.b], m_delaunayVertices[t.c]));
      }
      m_watch.Stop();
      m_watch.Reset();

      //populate delaunay triangles and edges
      m_watch.Start();
      //32 milliseconds to get all unique edges
      m_edges = Triangulator.GetUniqueEdges(m_delaunayTriangles);
      m_watch.Stop();
      m_watch.Reset();

      //finish populating the mesh
      m_watch.Start();
      //47 milliseconds to calculate all voronoi vertices
      GetVoronoiVertices();
      m_watch.Stop();
      m_watch.Reset();

      parseVoronoiFaces();
    }

    private void GetVoronoiVertices()
    {
      foreach (DelaunayTriangle triangle in m_delaunayTriangles)
      {
        m_voronoiVertices.Add(triangle.VoronoiVertex);
      }
    }

    private Dictionary<Vertex, List<Edge>> m_cachedIndexedEdgesByVoronoiVertex = null;
    public Dictionary<Vertex, List<Edge>> getIndexedEdgesByVoronoiVertex()
    {
      if (m_cachedIndexedEdgesByVoronoiVertex != null)
      {
        return m_cachedIndexedEdgesByVoronoiVertex;
      }

      Dictionary<Vertex, List<Edge>> edgeTable = new Dictionary<Vertex, List<Edge>>();
      foreach (DelaunayTriangle triangle in this.DelaunayTriangles)
      {
        foreach (Edge e in triangle.Edges)
        {
          if (e.VoronoiV1 == null || e.VoronoiV2 == null)
          {
            continue;
          }

          if (!edgeTable.ContainsKey(e.VoronoiV1))
          {
            List<Edge> edgeList = new List<Edge>();
            edgeList.Add(e);
            edgeTable.Add(e.VoronoiV1, edgeList);
          }
          else
          {
            bool found = false;
            foreach (Edge edgeItr in edgeTable[e.VoronoiV1])
            {
              if (edgeItr.DelaunayV1.x == e.DelaunayV1.x && edgeItr.DelaunayV1.y == e.DelaunayV1.y &&
                  edgeItr.DelaunayV2.x == e.DelaunayV2.x && edgeItr.DelaunayV2.y == e.DelaunayV2.y)
              {
                found = true;
                break;
              }
            }

            if (!found)
            {
              edgeTable[e.VoronoiV1].Add(e);
            }
          }

          if (!edgeTable.ContainsKey(e.VoronoiV2))
          {
            List<Edge> edgeList = new List<Edge>();
            edgeList.Add(e);
            edgeTable.Add(e.VoronoiV2, edgeList);
          }
          else
          {
            bool found = false;
            foreach (Edge edgeItr in edgeTable[e.VoronoiV2])
            {
              if (edgeItr.DelaunayV1.x == e.DelaunayV1.x && edgeItr.DelaunayV1.y == e.DelaunayV1.y &&
                  edgeItr.DelaunayV2.x == e.DelaunayV2.x && edgeItr.DelaunayV2.y == e.DelaunayV2.y)
              {
                found = true;
                break;
              }
            }

            if (!found)
            {
              edgeTable[e.VoronoiV2].Add(e);
            }
          }
        }
      }

      m_cachedIndexedEdgesByVoronoiVertex = edgeTable;

      return edgeTable;
    }

    private Dictionary<Vertex, List<Edge>> m_cachedIndexedEdgesByDelaunayVertex = null;
    public Dictionary<Vertex, List<Edge>> getIndexedEdgesByDelaunayVertex()
    {
      if (m_cachedIndexedEdgesByDelaunayVertex != null)
      {
        return m_cachedIndexedEdgesByDelaunayVertex;
      }

      Dictionary<Vertex, List<Edge>> edgeTable = new Dictionary<Vertex, List<Edge>>();
      foreach (DelaunayTriangle triangle in this.DelaunayTriangles)
      {
        foreach (Edge e in triangle.Edges)
        {
          if (e.DelaunayV1 == null || e.DelaunayV2 == null)
          {
            continue;
          }


          if (!edgeTable.ContainsKey(e.DelaunayV1))
          {
            List<Edge> edgeList = new List<Edge>();
            edgeList.Add(e);
            edgeTable.Add(e.DelaunayV1, edgeList);
          }
          else
          {
            bool found = false;
            foreach (Edge edgeItr in edgeTable[e.DelaunayV1])
            {
              if (edgeItr.DelaunayV1.x == e.DelaunayV1.x && edgeItr.DelaunayV1.y == e.DelaunayV1.y &&
                  edgeItr.DelaunayV2.x == e.DelaunayV2.x && edgeItr.DelaunayV2.y == e.DelaunayV2.y)
              {
                found = true;
                break;
              }
            }

            if (!found)
            {
              edgeTable[e.DelaunayV1].Add(e);
            }
          }

          if (!edgeTable.ContainsKey(e.DelaunayV2))
          {
            List<Edge> edgeList = new List<Edge>();
            edgeList.Add(e);
            edgeTable.Add(e.DelaunayV2, edgeList);
          }
          else
          {
            bool found = false;
            foreach (Edge edgeItr in edgeTable[e.DelaunayV2])
            {
              if (edgeItr.DelaunayV1.x == e.DelaunayV1.x && edgeItr.DelaunayV1.y == e.DelaunayV1.y &&
                  edgeItr.DelaunayV2.x == e.DelaunayV2.x && edgeItr.DelaunayV2.y == e.DelaunayV2.y)
              {
                found = true;
                break;
              }
            }

            if (!found)
            {
              edgeTable[e.DelaunayV2].Add(e);
            }
          }
        }
      }

      m_cachedIndexedEdgesByDelaunayVertex = edgeTable;

      return edgeTable;
    }

    public void parseVoronoiFaces()
    {
      m_voronoiPolygons.Clear();

      m_watch.Reset();
      m_watch.Start();

      //index all the delaunay triangles by vertex
      Dictionary<Vertex, List<DelaunayTriangle>> triangles = new Dictionary<Vertex, List<DelaunayTriangle>>();
      foreach (DelaunayTriangle triangle in m_delaunayTriangles)
      {
        foreach (Vertex dv in triangle.Vertices)
        {
          if (!triangles.ContainsKey(dv))
          {
            List<DelaunayTriangle> triangleList = new List<DelaunayTriangle>();
            triangleList.Add(triangle);
            triangles.Add(dv, triangleList);
          }
          else
          {
            triangles[dv].Add(triangle);
          }
        }
      }

      m_watch.Stop();
      m_watch.Reset();
      m_watch.Start();
      //connect edges and triangle neighbors
      foreach (Vertex dv in m_delaunayVertices)
      {
        if (triangles.ContainsKey(dv))
        {
          //sort triangles
          #region Sort Triangles
          //sort the spokes clockwise
          triangles[dv].Sort
          (
            delegate(DelaunayTriangle t1, DelaunayTriangle t2)
            {
              double angleP1 = Math.Atan2(dv.x - t1.VoronoiVertex.x, dv.y - t1.VoronoiVertex.y);
              double angleP2 = Math.Atan2(dv.x - t2.VoronoiVertex.x, dv.y - t2.VoronoiVertex.y);
              return angleP1.CompareTo(angleP2);
            }
          );
          #endregion

          VoronoiPolygon vp = new VoronoiPolygon();
          vp.DelaunayVertex = dv;
          Dictionary<Vertex, List<Edge>> edges = new Dictionary<Vertex, List<Edge>>();

          //match edges
          foreach (DelaunayTriangle tr1 in triangles[dv])
          {
            foreach (Edge e in tr1.Edges)
            {
              if (e.DelaunayV1 == dv || e.DelaunayV2 == dv)
              {
                foreach (DelaunayTriangle tr2 in triangles[dv])
                {
                  if (tr1 == tr2)
                  {
                    continue;
                  }

                  if (tr2.Edges[0] == e || tr2.Edges[1] == e || tr2.Edges[2] == e)
                  {
                    e.VoronoiV1 = tr1.VoronoiVertex;
                    e.VoronoiV2 = tr2.VoronoiVertex;

                    //check to see if the Voronoi Polygon already has this edge

                    if (!edges.ContainsKey(e.DelaunayV1))
                    {
                      List<Edge> tempEdges = new List<Edge>();
                      tempEdges.Add(e);
                      edges.Add(e.DelaunayV1, tempEdges);
                    }
                    else
                    {
                      bool found = false;
                      foreach (Edge edg in edges[e.DelaunayV1])
                      {
                        if (e.DelaunayV2.x == edg.DelaunayV2.x && e.DelaunayV2.y == edg.DelaunayV2.y)
                        {
                          found = true;
                          break;
                        }
                      }

                      if (!found)
                      {
                        edges[e.DelaunayV1].Add(e);
                      }
                    }

                    break;
                  }
                }
              }
            }

            vp.Vertices.Add(tr1.VoronoiVertex);
          }

          foreach (KeyValuePair<Vertex, List<Edge>> kvp in edges)
          {
            foreach (Edge edg in kvp.Value)
            {
              vp.Edges.Add(edg);
            }
          }

          if (vp.Edges.Count > 2)
          {
            m_voronoiPolygons.Add(vp);
          }
        }
      }
      parseVoronoiNeighbors();

      m_watch.Stop();
      m_watch.Reset();
    }

    private void parseVoronoiNeighbors()
    {
      Dictionary<Vertex, VoronoiPolygon> indexedPolygons = new Dictionary<Vertex, VoronoiPolygon>();

      //index all voronoi polygons by their delaunay vertex
      foreach (VoronoiPolygon vp in VoronoiPolygons)
      {
        if (!indexedPolygons.ContainsKey(vp.DelaunayVertex))
        {
          indexedPolygons.Add(vp.DelaunayVertex, vp);
        }
      }

      //get edges indexed by delaunay vertex
      Dictionary<Vertex, List<Edge>> indexedEdges = getIndexedEdgesByDelaunayVertex();

      foreach (Vertex delaunayVertex in DelaunayVertices)
      {
        if (indexedEdges.ContainsKey(delaunayVertex))
        {
          List<Edge> polyEdges = indexedEdges[delaunayVertex];
          foreach (Edge connector in polyEdges)
          {
            VoronoiPolygon vp1 = null;
            VoronoiPolygon vp2 = null;

            if (indexedPolygons.ContainsKey(connector.DelaunayV1))
            {
              vp1 = indexedPolygons[connector.DelaunayV1];
            }

            if (indexedPolygons.ContainsKey(connector.DelaunayV2))
            {
              vp2 = indexedPolygons[connector.DelaunayV2];
            }

            if (vp1 != null && vp2 != null)
            {
              if (!vp1.Neighbors.Contains(vp2))
              {
                vp1.Neighbors.Add(vp2);
              }

              if (!vp2.Neighbors.Contains(vp1))
              {
                vp2.Neighbors.Add(vp1);
              }
            }
          }
        }
      }
    }
  }
}
