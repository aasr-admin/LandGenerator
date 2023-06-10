using System;
using System.Collections.Generic;
using System.Text;

namespace DelaunayTriangulator
{

  /* Each edge is checked to see if it is unique by only the delaunay vertices.  Vertices are added
   * such that the left most (smallest x value) vertex is always delaunayV1.  If both vertices have the same X value, 
   * then the vertex with the top most (smallest y value) y value is delaunayV1.
   */
  public class Edge
  {
    public override int GetHashCode()
    {
      unchecked // Overflow is fine, just wrap
      {
        int hash = 17;
        // Suitable nullity checks etc, of course :)
        hash = hash * 23 + m_delaunayV1.x.GetHashCode();
        hash = hash * 23 + m_delaunayV1.y.GetHashCode();
        return hash;
      }
    }


    private Vertex m_voronoiV1;
    private Vertex m_voronoiV2;
    private Vertex m_delaunayV1;
    private Vertex m_delaunayV2;
    private Vertex m_voronoiMidpoint;

    public object Attachment { get; set; }

    public override string ToString()
    {
      return string.Format("({0},{1}) - ({2},{3})", m_delaunayV1.x, m_delaunayV1.y, m_delaunayV2.x, m_delaunayV2.y);
    }

    public Edge(Vertex v1, Vertex v2)
    {
      if (v1.x < v2.x || (v1.x == v2.x && v1.y < v2.y))
      {
        m_delaunayV1 = v1;
        m_delaunayV2 = v2;
      }
      else
      {
        m_delaunayV1 = v2;
        m_delaunayV2 = v1;
      }
    }

    public Vertex VoronoiV1 { get { return m_voronoiV1; } set { m_voronoiV1 = value; } }
    public Vertex VoronoiV2 { get { return m_voronoiV2; } set { m_voronoiV2 = value; } }
    public Vertex DelaunayV1 { get { return m_delaunayV1; } set { m_delaunayV1 = value; } }
    public Vertex DelaunayV2 { get { return m_delaunayV2; } set { m_delaunayV2 = value; } }

    public Vertex VoronoiMidPoint
    {
      get
      {
        if (m_voronoiMidpoint == null)
        {
          m_voronoiMidpoint = new Vertex((m_voronoiV1.x + m_voronoiV2.x) / 2.0, (m_voronoiV1.y + m_voronoiV2.y) / 2.0);
        }

        return m_voronoiMidpoint;
      }
    }

    #region IEquatable<dEdge> Members

    //This only works because the vertices are sorted by Delaunay Vertex
    public bool Equals(Edge other)
    {
      return (this.m_delaunayV1 == other.m_delaunayV1) && (this.m_delaunayV2 == other.m_delaunayV2);
    }
    #endregion


    public Vertex FindVoronoiIntersection(Edge e2)
    {
      Vertex result = null;

      if (this.VoronoiV1 != null && this.VoronoiV2 != null && e2.VoronoiV1 != null && e2.VoronoiV2 != null)
      {
        double s1_x;
        double s1_y;
        double s2_x;
        double s2_y;

        s1_x = this.VoronoiV2.x - this.VoronoiV1.x;
        s1_y = this.VoronoiV2.y - this.VoronoiV1.y;
        s2_x = e2.VoronoiV2.x - e2.VoronoiV1.x;
        s2_y = e2.VoronoiV2.y - e2.VoronoiV1.y;

        double s, t;
        s = (-s1_y * (this.VoronoiV1.x - e2.VoronoiV1.x) + s1_x * (this.VoronoiV1.y - e2.VoronoiV1.y)) / (-s2_x * s1_y + s1_x * s2_y);
        t = (s2_x * (this.VoronoiV1.y - e2.VoronoiV1.y) - s2_y * (this.VoronoiV1.x - e2.VoronoiV1.x)) / (-s2_x * s1_y + s1_x * s2_y);

        if (s >= 0 && s <= 1 && t >= 0 && t <= 1)
        {
          result = new Vertex(this.VoronoiV1.x + (t * s1_x), this.VoronoiV1.y + (t * s1_y));
        }
      }
      return result; // No collision
    }

  }
}
