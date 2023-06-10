using System;
using System.Collections.Generic;
using DelaunayTriangulator;
using System.Text;

namespace MapGenerator.MapModel
{
  public static class VoronoiPolygonExtensions
  {
    public static PolygonLandProperties GetProperties(this VoronoiPolygon poly)
    {
      if (poly.Attachment == null)
      {
        poly.Attachment = new PolygonLandProperties();
      }

      return (PolygonLandProperties)poly.Attachment;
    }
  }

  public class PolygonLandProperties : VertexLandProperties
  {
    public enum TerrainSubtype
    {
      Water,
      Snow,
      Tundra, 
      Bare,
      Scorched,
      Taiga,
      Shrubland,
      TemperateDesert,
      TemperateRainForest,
      TemperateDeciduousForest,
      Grassland, 
      TropicalRainForest,
      TropicalSeasonalForest,
      SubtropicalDesert,
      Mountain,
      Swamp
    }

    public static TerrainSubtype[][] BiomeTable = new TerrainSubtype[10][]
    {
      /* Moisture                Temperature   0.0 to 0.24               0.25 to 0.49                    0.50 to 0.74                             0.75 to 1.00 */
      /* 0.0  to 0.09 */ new TerrainSubtype[] {TerrainSubtype.Scorched,  TerrainSubtype.TemperateDesert, TerrainSubtype.TemperateDesert,          TerrainSubtype.SubtropicalDesert      },
      /* 0.10 to 0.19 */ new TerrainSubtype[] {TerrainSubtype.Scorched,  TerrainSubtype.TemperateDesert, TerrainSubtype.TemperateDesert,          TerrainSubtype.SubtropicalDesert      },
      /* 0.20 to 0.29 */ new TerrainSubtype[] {TerrainSubtype.Scorched,  TerrainSubtype.TemperateDesert, TerrainSubtype.Grassland,                TerrainSubtype.SubtropicalDesert      },
      /* 0.30 to 0.39 */ new TerrainSubtype[] {TerrainSubtype.Bare,      TerrainSubtype.TemperateDesert, TerrainSubtype.Grassland,                TerrainSubtype.TropicalSeasonalForest },
      /* 0.40 to 0.49 */ new TerrainSubtype[] {TerrainSubtype.Bare,      TerrainSubtype.Shrubland,       TerrainSubtype.Grassland,                TerrainSubtype.TropicalSeasonalForest },
      /* 0.50 to 0.59 */ new TerrainSubtype[] {TerrainSubtype.Tundra,    TerrainSubtype.Shrubland,       TerrainSubtype.Grassland,                TerrainSubtype.TropicalSeasonalForest },
      /* 0.60 to 0.69 */ new TerrainSubtype[] {TerrainSubtype.Tundra,    TerrainSubtype.Shrubland,       TerrainSubtype.TemperateDeciduousForest, TerrainSubtype.TropicalRainForest     },
      /* 0.70 to 0.79 */ new TerrainSubtype[] {TerrainSubtype.Tundra,    TerrainSubtype.Taiga,           TerrainSubtype.TemperateDeciduousForest, TerrainSubtype.TropicalRainForest     },
      /* 0.80 to 0.89 */ new TerrainSubtype[] {TerrainSubtype.Snow,      TerrainSubtype.Taiga,           TerrainSubtype.TemperateRainForest,      TerrainSubtype.TropicalRainForest     },
      /* 0.90 to 1.00 */ new TerrainSubtype[] {TerrainSubtype.Snow,      TerrainSubtype.Taiga,           TerrainSubtype.TemperateRainForest,      TerrainSubtype.TropicalRainForest     },
    };

    private TerrainSubtype m_landSubtype = TerrainSubtype.Water;
    public TerrainSubtype LandSubtype { get { return m_landSubtype; } set { m_landSubtype = value; } }

    private Vertex m_watershedVertex;
    public Vertex WatershedVertex { get { return m_watershedVertex; } set { m_watershedVertex = value; } }

    public static double CalculatePolygonAltitudeFromVertices(VoronoiPolygon vp)
    {
      double altitude = 0.0;

      foreach (Vertex vv in vp.Vertices)
      {
        altitude += ((VertexLandProperties)(vv.Attachment)).Elevation;
      }

      altitude /= vp.Vertices.Count;

      return altitude;
    }
  }
}
