using System;
using System.Collections.Generic;
using System.Text;

namespace DelaunayTriangulator
{
  public class DelaunayTriangle
  {
    public object Attachment { get; set; }

    private Vertex[] m_vertices = new Vertex[3];
    public DelaunayTriangle(Vertex v0, Vertex v1, Vertex v2)
    {
      m_vertices[0] = v0;
      m_vertices[1] = v1;
      m_vertices[2] = v2;
    }
    public Vertex[] Vertices { get { return m_vertices; } set { m_vertices = value; } }
    public Edge[] Edges { get { return m_edges; } set { m_edges = value; } }
    public Vertex VoronoiVertex
    {
      get
      {
        if (m_voronoiVertex == null)
        {
          m_voronoiVertex = GetCircumcenter();
        }
        return m_voronoiVertex;
      }

      set { m_voronoiVertex = value; }
    }

    public List<DelaunayTriangle> Neighbors { get { return m_neighbors; } set { m_neighbors = value; } }

    private List<DelaunayTriangle> m_neighbors = new List<DelaunayTriangle>();

    private Edge[] m_edges = new Edge[3];
    private Vertex m_voronoiVertex = null;
    private double LengthSquared(double[] v)
    {
      double norm = 0;
      for (int i = 0; i < v.Length; i++)
      {
        var t = v[i];
        norm += t * t;
      }
      return norm;
    }
    private double Det(double[,] m)
    {
      return m[0, 0] * ((m[1, 1] * m[2, 2]) - (m[2, 1] * m[1, 2])) - m[0, 1] * (m[1, 0] * m[2, 2] - m[2, 0] * m[1, 2]) + m[0, 2] * (m[1, 0] * m[2, 1] - m[2, 0] * m[1, 1]);
    }
    private Vertex GetCircumcenter()
    {
      // From MathWorld: http://mathworld.wolfram.com/Circumcircle.html

      double[,] m = new double[3, 3];

      // x, y, 1
      for (int i = 0; i < 3; i++)
      {
        m[i, 0] = m_vertices[i].x;
        m[i, 1] = m_vertices[i].y;
        m[i, 2] = 1;
      }
      var a = Det(m);

      // size, y, 1
      for (int i = 0; i < 3; i++)
      {
        double norm = m_vertices[i].x * m_vertices[i].x;
        norm += m_vertices[i].y * m_vertices[i].y;
        m[i, 0] = norm;
      }

      var dx = -Det(m);

      // size, x, 1
      for (int i = 0; i < 3; i++)
      {
        m[i, 1] = m_vertices[i].x;
      }
      var dy = Det(m);

      // size, x, y
      for (int i = 0; i < 3; i++)
      {
        m[i, 2] = m_vertices[i].y;
      }
      var c = -Det(m);

      var s = -1.0 / (2.0 * a);
      var r = System.Math.Abs(s) * System.Math.Sqrt(dx * dx + dy * dy - 4 * a * c);
      return new Vertex(s * dx, s * dy);
    }
  }
}
