using System;
using System.Collections.Generic;
using DelaunayTriangulator;
using System.Text;

namespace MapGenerator.MapModel
{
  public static class VertexExtensions
  {
    public static VertexLandProperties GetProperties(this Vertex vertex)
    {
      if (vertex.Attachment == null)
      {
        vertex.Attachment = new VertexLandProperties();
      }

      return (VertexLandProperties)vertex.Attachment;
    }

    public static List<Vertex> GetNeighbors(this Vertex vertex, Mesh mesh)
    {
      Dictionary<Vertex, List<Edge>> indexedEdges = mesh.getIndexedEdgesByVoronoiVertex();
      List<Vertex> neighbors = new List<Vertex>();
      if (indexedEdges.ContainsKey(vertex))
      {
        foreach (Edge edge in indexedEdges[vertex])
        {
          if (edge.VoronoiV1 != vertex)
          {
            neighbors.Add(edge.VoronoiV1);
            continue;
          }

          if (edge.VoronoiV2 != vertex)
          {
            neighbors.Add(edge.VoronoiV2);
          }
        }
      }

      return neighbors;
    }
  }

  public class VertexLandProperties
  {
    public enum TerrainType
    {
      Ocean,
      Lake,
      Land
    }

    //protected double m_elevation = 0.0;
    protected double m_elevation = double.PositiveInfinity;
    protected TerrainType m_landType = TerrainType.Lake;
    protected bool m_border = false;
    protected double m_moisture = 0.0;
    protected double m_temperature = 0.0;

    public double Temperature { get { return m_temperature; } set { m_temperature = value; } }
    public double Moisture { get { return m_moisture; } set { m_moisture = value; } }
    public bool Border { get { return m_border; } set { m_border = value; } }
    public double Elevation { get { return m_elevation; } set { m_elevation = value; } }

    public Edge DownslopeEdge { get; set; }
    public TerrainType LandType { get { return m_landType; } set { m_landType = value; } }
  }
}
