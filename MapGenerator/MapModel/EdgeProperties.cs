using System;
using System.Collections.Generic;
using DelaunayTriangulator;
using System.Text;

namespace MapGenerator.MapModel
{
  using RandomExtensions;

  public static class EdgeExtensions
  {
    public static EdgeProperties GetProperties(this Edge edge)
    {
      if (edge.Attachment == null)
      {
        edge.Attachment = new EdgeProperties();
      }

      return (EdgeProperties)edge.Attachment;
    }
  }

  public class EdgeProperties
  {
    private int m_riverVolume = 0;
    private bool m_border = false;

    public bool Border { get { return m_border; } set { m_border = value; } }

    public int RiverVolume { get { return m_riverVolume; } set { m_riverVolume = value; } }

    public List<Vertex> NoisyPath0 = null;
    public List<Vertex> NoisyPath1 = null;

    public const double NOISY_LINE_TRADEOFF = 0.5;

    public static void CalculateNoisyEdges(Mesh mesh, int minLength)
    {
      Random rand = new Random((int)DateTime.Now.Ticks);
      foreach (VoronoiPolygon vp in mesh.VoronoiPolygons) 
      {
          foreach (Edge edge in vp.Edges) 
          {
            EdgeProperties edgeProps = (EdgeProperties)edge.Attachment;

            if (edge.DelaunayV1 != null && edge.DelaunayV2 != null && edge.VoronoiV1 != null && edge.VoronoiV2 != null && edgeProps.NoisyPath0 == null && edgeProps.NoisyPath1 == null) 
              {
                Vertex t = Vertex.Interpolate(edge.VoronoiV1, edge.DelaunayV1, NOISY_LINE_TRADEOFF);
                Vertex q = Vertex.Interpolate(edge.VoronoiV1, edge.DelaunayV2, NOISY_LINE_TRADEOFF);
                Vertex r = Vertex.Interpolate(edge.VoronoiV2, edge.DelaunayV1, NOISY_LINE_TRADEOFF);
                Vertex s = Vertex.Interpolate(edge.VoronoiV2, edge.DelaunayV1, NOISY_LINE_TRADEOFF);
    
                //todo come back to this when we add biomes
                //if (edge.d0.biome != edge.d1.biome) minLength = 3;
                //if (edge.d0.ocean && edge.d1.ocean) minLength = 100;
                //if (edge.d0.coast || edge.d1.coast) minLength = 1;
                //if (edge.river || lava.lava[edge.index]) minLength = 1;

                edgeProps.NoisyPath0 = BuildNoisyLineSegments(edge.VoronoiV1, t, edge.VoronoiMidPoint, q, minLength, rand);
                edgeProps.NoisyPath1 = BuildNoisyLineSegments(edge.VoronoiV2, s, edge.VoronoiMidPoint, r, minLength, rand);
              }
            }
        }
    }

    private static void subdivide(Vertex a, Vertex b, Vertex c, Vertex d, List<Vertex> points, int minLength, Random random)
    {

        if (a.Subtract(c).Length < minLength || b.Subtract(d).Length < minLength) 
        {
          return;
        }

        // Subdivide the quadrilateral
        double p = random.NextDoubleRange(0.2, 0.8); // vertical (along A-D and B-C)
        double q = random.NextDoubleRange(0.2, 0.8); // horizontal (along A-B and D-C)

        // Midpoints
        Vertex e = Vertex.Interpolate(a, d, p);
        Vertex f = Vertex.Interpolate(b, c, p);
        Vertex g = Vertex.Interpolate(a, b, q);
        Vertex i = Vertex.Interpolate(d, c, q);
        
        // Central point
        Vertex h = Vertex.Interpolate(e, f, q);
        
        // Divide the quad into subquads, but meet at H
        //double s = 1.0 - (random.NextDoubleRange(-0.4, +0.4);
        //double t = 1.0 - (random.NextDoubleRange(-0.4, +0.4);
        double s = 1.0 - random.NextDoubleRange(-0.4, +0.4);
        double t = 1.0 - random.NextDoubleRange(-0.4, +0.4);
        subdivide(a, Vertex.Interpolate(g, b, s), h, Vertex.Interpolate(e, d, t), points, minLength, random);
        points.Add(h);
        subdivide(h, Vertex.Interpolate(f, c, s), c, Vertex.Interpolate(i, d, t), points, minLength, random);
      }

    private static List<Vertex> BuildNoisyLineSegments(Vertex a, Vertex b, Vertex c, Vertex d, int minLength, Random rand)
    {
      List<Vertex> points = new List<Vertex>();

      points.Add(a);
      subdivide(a, b, c, d, points, minLength, rand);
      points.Add(c);
      return points;
    }
  }


}
