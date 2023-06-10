using System;
using System.Collections.Generic;
using System.Text;

/*
  copyright s-hull.org 2011
  released under the contributors beerware license

  contributors: Phil Atkin, Dr Sinclair.
*/
namespace DelaunayTriangulator
{
  public class Vertex : IEquatable<Vertex>
  {
    public override int GetHashCode()
    {
      unchecked // Overflow is fine, just wrap
      {
        int hash = 17;
        // Suitable nullity checks etc, of course :)
        hash = hash * 23 + m_x.GetHashCode();
        hash = hash * 23 + m_y.GetHashCode();
        return hash;
      }
    }


    protected Vertex() { }

    public object Attachment { get; set; }

    double m_x;
    double m_y;
    public Vertex(double x, double y)
    {
      m_x = x;
      m_y = y;
    }

    public override string ToString()
    {
      return string.Format("{0},{1}", m_x, m_y);
    }

    public double x { get { return m_x; } set { m_x = value; } }
    public double y { get { return m_y; } set { m_y = value; } }

    public bool Equals(Vertex other)
    {
      return this.x == other.x && this.y == other.y;
    }

    public static Vertex Interpolate(Vertex pt1, Vertex pt2, double f)
    {
      return new Vertex(f * pt1.x + (1 - f) * pt2.x, f * pt1.y + (1 - f) * pt2.y);
    }

    // Returns the distance between v1 and v2.
    public static double Distance(Vertex v1, Vertex v2)
    {
      return Math.Sqrt(((v1.x - v2.x) * (v1.x - v2.x)) + ((v1.y - v2.y) * (v1.y + v2.y)));
    }

    //Subtracts the coordinates of another point from the coordinates of this point to create a new point.
    public Vertex Subtract(Vertex v)
    {
      return new Vertex(this.x - v.x, this.y - v.y);
    }

    //The length of the line segment from (0,0) to this point.
    public double Length
    {
      get
      {
        return Math.Sqrt((this.x * this.x) + (this.y * this.y));
      }
    }
  }
}
