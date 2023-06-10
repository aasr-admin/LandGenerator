using System;
using System.Collections.Generic;
using System.Text;

namespace DelaunayTriangulator
{
  public class VoronoiPolygon
  {
    private List<Vertex> m_vertices = new List<Vertex>();
    private List<Edge> m_edges = new List<Edge>();
    private Vertex m_delaunayVertex;
    private List<VoronoiPolygon> m_neighbors = new List<VoronoiPolygon>();

    public List<Vertex> Vertices { get { return m_vertices; } set { m_vertices = value; } }
    public List<Edge> Edges { get { return m_edges; } set { m_edges = value; } }
    public Vertex DelaunayVertex { get { return m_delaunayVertex; } set { m_delaunayVertex = value; } }
    public object Attachment { get; set; }

    public List<VoronoiPolygon> Neighbors { get { return m_neighbors; } set { m_neighbors = value; } }
  }
}
