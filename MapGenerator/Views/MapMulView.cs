using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;
using DelaunayTriangulator;
using MapGenerator.MapModel;

namespace MapGenerator.View
{
  public struct MapCell
  {
    public UInt16 LandId;
    public sbyte Altitude;
  }

  public struct MapBlock
  {
    public Int32 Header;
    public MapCell[] Cells;
  }

  public class MapMul
  {
    public const int NUM_CELLS_PER_MAP_BLOCK = 64;

    public int Height { get; set; }
    public int Width { get; set; }
    public int NumHorizontalBlocks { get { return Width / 8; } }
    public int NumVerticalBlocks { get { return Height / 8; } }

    public MapBlock[] Blocks { get; set; }

    public void SetTileId(int x, int y, UInt16 tileId)
    {
      MapBlock block = Blocks[((x / 8) * NumVerticalBlocks) + (y / 8)];
      int cellIndex = ((y & 0x7) << 3) + (x & 0x7);
      block.Cells[cellIndex].LandId = tileId;
    }

    public void SetTile(int x, int y, sbyte altitude, UInt16 tileId)
    {
      MapBlock block = Blocks[((x / 8) * NumVerticalBlocks) + (y / 8)];
      int cellIndex = ((y & 0x7) << 3) + (x & 0x7);
      block.Cells[cellIndex].LandId = tileId;
      block.Cells[cellIndex].Altitude = altitude;
    }

    private static UInt16[] WaterTiles = new UInt16[] { 0xA8, 0xA9, 0xAA, 0xAB, 0x136, 0x137 };

    public MapMul(int width, int height)
    {
      Random rand = new Random((int)DateTime.Now.Ticks);
      Height = height;
      Width = width;

      if (Width % 8 != 0)
      {
        Width += 8 - (Width % 8);
      }

      if (Height % 8 != 0)
      {
        Height += 8 - (Height % 8);
      }

      Blocks = new MapBlock[NumHorizontalBlocks * NumVerticalBlocks];

      //build Map structure, setting all tiles to water at elevation 0
      for (int i = 0; i < Blocks.Length; ++i)
      {
        Blocks[i].Cells = new MapCell[NUM_CELLS_PER_MAP_BLOCK];

        for (int cellNum = 0; cellNum < NUM_CELLS_PER_MAP_BLOCK; cellNum++)
        {
          Blocks[i].Cells[cellNum].Altitude = 0;
          Blocks[i].Cells[cellNum].LandId = WaterTiles[rand.Next() % WaterTiles.Length];
        }
      }
    }

    public void SaveMapToDisk(string filename)
    {
      using (FileStream fs = File.Create(filename, 2048, FileOptions.None))
      {
        using (BinaryWriter writer = new BinaryWriter(fs))
        {
          for (int blockNum = 0; blockNum < Blocks.Length; ++blockNum)
          {
            writer.Write((UInt32)0x00000000);
            for (int cellNum = 0; cellNum < NUM_CELLS_PER_MAP_BLOCK; ++cellNum)
            {
              writer.Write(Blocks[blockNum].Cells[cellNum].LandId);
              writer.Write(Blocks[blockNum].Cells[cellNum].Altitude);
            }
          }
        }
      }
    }


    private MapMul()
    {
    }
  }


  public class MapMulView
  {
    public static void ConvertMap(Map map, string filename)
    {
      MapMul mapMul = new MapMul(map.Width, map.Height);

      WriteLandToMapMul(map, mapMul);

      mapMul.SaveMapToDisk(filename);
    }

    private static void WriteLandToMapMul(Map map, MapMul mapMul)
    {
      foreach (VoronoiPolygon vp in map.Mesh.VoronoiPolygons)
      {
        if (((PolygonLandProperties)(vp.Attachment)).LandType == PolygonLandProperties.TerrainType.Land)
        {
          FillPoly(vp, mapMul, 0x0003);
        }
      }
    }

    private static void DrawHorizontalLine(MapMul mapMul, int x1, int x2, int y, UInt16 tileId, sbyte altitude)
    {
      if (x1 < 0)
      {
        x1 = 0;
      }

      if (x1 >= mapMul.Width)
      {
        x1 = mapMul.Width - 1;
      }

      if (x2 < 0)
      {
        x2 = 0;
      }

      if (x2 >= mapMul.Width)
      {
        x2 = mapMul.Width - 1;
      }

      if (y < 0)
      {
        y = 0;
      }

      if (y >= mapMul.Height)
      {
        y = mapMul.Height - 1;
      }
      for (int x = x1; x < x2; ++x)
      {
        mapMul.SetTile(x, y, altitude, tileId);
      }
    }

    private static void FillPoly(VoronoiPolygon vp, MapMul mapMul, UInt16 tileId)
    {
      List<Vertex> vertices = new List<Vertex>();

      foreach (Edge edge in vp.Edges)
      {
        EdgeProperties edgeProps = (EdgeProperties)edge.Attachment;
        if (edgeProps.NoisyPath0 != null && edgeProps.NoisyPath1 != null)
        {
          foreach (Vertex vertexItr in edgeProps.NoisyPath0)
          {
            if (!vertices.Contains(vertexItr))
            {
              vertices.Add(vertexItr);
            }
          }

          foreach (Vertex vertexItr in edgeProps.NoisyPath1)
          {
            if (!vertices.Contains(vertexItr))
            {
              vertices.Add(vertexItr);
            }
          }

        }
        else
        {
          if (!vertices.Contains(edge.VoronoiV1))
          {
            vertices.Add(edge.VoronoiV1);
          }

          if (!vertices.Contains(edge.VoronoiV2))
          {
            vertices.Add(edge.VoronoiV2);
          }
        }
      }

      #region Sort Points
      vertices.Sort
      (
        delegate(Vertex p1, Vertex p2)
        {
          double angleP1 = Math.Atan2(vp.DelaunayVertex.x - p1.x, vp.DelaunayVertex.y - p1.y);
          double angleP2 = Math.Atan2(vp.DelaunayVertex.x - p2.x, vp.DelaunayVertex.y - p2.y);
          return angleP1.CompareTo(angleP2);
        }
      );
      #endregion


      FillPolygon(vertices, mapMul, tileId, PolygonLandProperties.CalculatePolygonAltitudeFromVertices(vp));
    }
    

    private static void FillPolygon(List<Vertex> vertices, MapMul mapMul, UInt16 tileId, double altitude)
    {

      int numPointsMinusOne = vertices.Count - 1;
      //all edges table
      //[Edge Number] [y-min, y-max, x-val, 1/m] 
      List<double[]> allEdgesTable = new List<double[]>();

      for (int i = 0; i < numPointsMinusOne; i++)
      {
        double[] edge = new double[4];
        edge[0] = (int)vertices[i].y < (int)vertices[i + 1].y ? (int)vertices[i].y : (int)vertices[i + 1].y; //The minimum y value of the two vertices. 
        edge[1] = (int)vertices[i].y > (int)vertices[i + 1].y ? (int)vertices[i].y : (int)vertices[i + 1].y; //The maximum y value of the two vertices. 
        edge[2] = (int)vertices[i].y < (int)vertices[i + 1].y ? (int)vertices[i].x : (int)vertices[i + 1].x; //x value associated with the minimum y value
        double rise = (int)vertices[i].y - (int)vertices[i + 1].y;
        double run = ((int)vertices[i].x - (int)vertices[i + 1].x);

        if (run == 0)
        {
          edge[3] = double.PositiveInfinity;
        }
        else
        {
          edge[3] = rise / run;
        }

        allEdgesTable.Add(edge);
      }

      //last and first
      double[] finalEdge = new double[4];
      finalEdge[0] = (int)vertices[numPointsMinusOne].y < (int)vertices[0].y ? (int)vertices[numPointsMinusOne].y : (int)vertices[0].y; //The minimum y value of the two vertices. 
      finalEdge[1] = (int)vertices[numPointsMinusOne].y > (int)vertices[0].y ? (int)vertices[numPointsMinusOne].y : (int)vertices[0].y; //The maximum y value of the two vertices. 
      finalEdge[2] = (int)vertices[numPointsMinusOne].y < (int)vertices[0].y ? (int)vertices[numPointsMinusOne].x : (int)vertices[0].x; //x value associated with the minimum y value
      double finalRise = (int)vertices[numPointsMinusOne].y - (int)vertices[0].y;
      double finalRun = ((int)vertices[numPointsMinusOne].x - (int)vertices[0].x);

      if (finalRun == 0)
      {
        finalEdge[3] = double.PositiveInfinity;
      }
      else
      {
        finalEdge[3] = finalRise / finalRun;
      }

      allEdgesTable.Add(finalEdge);

      int currentY = (int)allEdgesTable[0][0];
      int endY = (int)allEdgesTable[0][1];
      //find the lowest starting position
      foreach (double[] edge in allEdgesTable)
      {
        if (edge[0] < currentY)
        {
          currentY = (int)edge[0];
        }

        if (edge[1] > endY)
        {
          endY = (int)edge[1];
        }
      }

      int numberOfScanlines = endY - currentY + 1;

      Dictionary<int, List<double[]>> scanlineEdgeTable = new Dictionary<int, List<double[]>>();

      foreach (double[] edge in allEdgesTable)
      {
        if (edge[3] != 0)
        {
          if (!scanlineEdgeTable.ContainsKey((int)edge[0]))
          {
            scanlineEdgeTable.Add((int)edge[0], new List<double[]>());
          }
          scanlineEdgeTable[(int)edge[0]].Add(edge);
        }
      }

      foreach (KeyValuePair<int, List<double[]>> kvp in scanlineEdgeTable)
      {
        kvp.Value.Sort
          (
            delegate(double[] d1, double[] d2)
            {
              return d1[2].CompareTo(d2[2]);
            }
          );
      }

      List<double[]> scanLineBucket = new List<double[]>();

      for (int y = currentY; y <= endY; y++)
      {
        //merge y bucket from ET into SLB, sort on xmin
        if (scanlineEdgeTable.ContainsKey(y))
        {
          foreach (double[] edge in scanlineEdgeTable[y])
          {
            scanLineBucket.Add(edge);
          }

          scanLineBucket.Sort
          (
            delegate(double[] d1, double[] d2)
            {
              return d1[2].CompareTo(d2[2]);
            }
          );
        }

        //remove edges from SLB whose y-max == current y
        foreach (double[] edge in scanLineBucket.ToArray())
        {
          if (edge[1] == y)
          {
            scanLineBucket.Remove(edge);
          }
        }

        //fill in pixels between rounded pairs of x values in SLB
        if (scanLineBucket.Count > 1)
        {
          if (scanLineBucket.Count % 2 != 0)
          {
            continue;
          }
          for (int i = 0; i < scanLineBucket.Count - 1; i += 2)
          {

            //graphics.DrawLine(blackPen, new Point((int)scanLineBucket[i][2], y), new Point((int)scanLineBucket[i + 1][2], y));
            DrawHorizontalLine(mapMul, (int)scanLineBucket[i][2], (int)scanLineBucket[i + 1][2], y, tileId, (sbyte)altitude);
          }
        }

        //increment xmin by 1/m for edges in slb
        foreach (double[] edge in scanLineBucket)
        {
          if (edge[3] != double.PositiveInfinity)
          {
            edge[2] += (1.0 / edge[3]);
          }
        }
      }
    }
  }
}
