using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using DelaunayTriangulator;
using System.Diagnostics;
using MapGenerator.MapModel;

namespace LandGenerator
{
  public class MapImager
  {
    public static BitmapImage GetBitmapImage(Bitmap bmp)
    {
      byte[] imageBytes = null;
      using (MemoryStream stream = new MemoryStream())
      {
        bmp.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
        imageBytes = stream.ToArray();
      }

      var bitmapImage = new BitmapImage();
      bitmapImage.BeginInit();
      bitmapImage.StreamSource = new MemoryStream(imageBytes);
      bitmapImage.EndInit();
      return bitmapImage;
    }

    private static void FillPolygon(Bitmap bmp, VoronoiPolygon vp, System.Drawing.Color targetColor, bool useNoisyLines)
    {
      List<Vertex> vertices = new List<Vertex>();

      foreach (Edge edge in vp.Edges)
      {
        EdgeProperties edgeProps = (EdgeProperties)edge.Attachment;
        if (useNoisyLines && edgeProps.NoisyPath0 != null && edgeProps.NoisyPath1 != null)
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
      //*
      vertices.Sort
      (
        delegate(Vertex p1, Vertex p2)
        {
          double angleP1 = Math.Atan2(vp.DelaunayVertex.x - p1.x, vp.DelaunayVertex.y - p1.y);
          double angleP2 = Math.Atan2(vp.DelaunayVertex.x - p2.x, vp.DelaunayVertex.y - p2.y);
          return angleP1.CompareTo(angleP2);
        }
      );
      /**/
      #endregion


      FillPolygon2(bmp, vertices, targetColor);
    }

    private static void FillPolygon2(Bitmap bmp, List<Vertex> vertices, System.Drawing.Color targetColor)
    {

      List<Point> points = new List<Point>();
      foreach (Vertex v in vertices)
      {
        points.Add(new Point((int)v.x, (int)v.y));
      }

      using (var graphics = Graphics.FromImage(bmp))
      {
        graphics.FillPolygon(new SolidBrush(targetColor), points.ToArray());
      }
    }

    private static void FillPolygon(Bitmap bmp, List<Vertex> vertices, System.Drawing.Color targetColor)
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

      if (currentY < 0)
      {
        currentY = 0;
      }

      if (currentY >= bmp.Height)
      {
        currentY = bmp.Height - 1;
      }

      if (endY < 0)
      {
        endY = 0;
      }

      if (endY >= bmp.Height)
      {
        endY = bmp.Height - 1;
      }

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

      Pen blackPen = new Pen(targetColor);

      using (var graphics = Graphics.FromImage(bmp))
      {


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
            for (int i = 0; i < scanLineBucket.Count - 1; i += 2)
            {
              int x1 = (int)scanLineBucket[i][2];
              int x2 = (int)scanLineBucket[i + 1][2];

              if (x1 < 0)
              {
                x1 = 0;
              }

              if (x1 > bmp.Width)
              {
                x1 = bmp.Width - 1;
              }

              if (x2 < 0)
              {
                x2 = 0;
              }

              if (x2 > bmp.Width)
              {
                x2 = bmp.Width - 1;
              }

              graphics.DrawLine(blackPen, new Point(x1, y), new Point(x2, y));
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

    public static Bitmap GetDelaunayLayer(Map map)
    {
      Bitmap bmp = new Bitmap(map.Width, map.Height);

      bmp.MakeTransparent();
      using (var graphics = Graphics.FromImage(bmp))
      {
        graphics.FillRectangle(new SolidBrush(Color.Transparent), 0, 0, bmp.Width, bmp.Height);

        Pen redPen = new Pen(Color.Red);
        Pen blackPen = new Pen(Color.Red);

        foreach (Vertex v in map.Mesh.DelaunayVertices)
        {
          bmp.SetPixel((int)v.x, (int)v.y, Color.Red);
          graphics.DrawLine(redPen, (int)v.x, (int)v.y, (int)v.x, (int)v.y);
        }

        foreach (DelaunayTriangle dt in map.Mesh.DelaunayTriangles)
        {
          for (int i = 0; i < dt.Vertices.Length; i++)
          {
            Vertex v1 = dt.Vertices[i];
            Vertex v2 = null;

            if (i == dt.Vertices.Length - 1)
            {
              v2 = dt.Vertices[0];
            }
            else
            {
              v2 = dt.Vertices[i + 1];
            }
            if (v1 != null && v2 != null)
            {
              graphics.DrawLine(blackPen, (int)v1.x, (int)v1.y, (int)v2.x, (int)v2.y);
            }
          }
        }
      }

      return bmp;
    }

    public static Bitmap GetLandLayer(Map map)
    {
      Bitmap bmp = new Bitmap(map.Width, map.Height);

      foreach (VoronoiPolygon vp in map.Mesh.VoronoiPolygons)
      {
        if (((PolygonLandProperties)vp.Attachment).LandType == PolygonLandProperties.TerrainType.Land)
        {
          FillPolygon(bmp, vp, Color.Green, false);
        }
      }

      return bmp;
    }

    private class rgb
    {
      public int Red = 0;
      public int Blue = 0;
      public int Green = 0;
      public rgb(int r, int b, int g)
      {
        Red = r;
        Blue = b;
        Green = g;
      }
    }

    private static rgb[] TerrainSubtypeColors = new rgb[]
    {
      new rgb(0,0,0),/* Water,                     */
      new rgb(0xCC,0xFF,0xFF),/* Snow,                     */
      new rgb(0xCC,0xFF,0xFF),/* Tundra,                   */
      new rgb(0xCC,0xFF,0xFF),/* Bare,                     */
      new rgb(0xCC,0xFF,0xFF),/* Scorched,                 */
      new rgb(0x66,0xFF,0xCC),/* Taiga,                    */
      new rgb(0x66,0xFF,0xCC),/* Shrubland,                */
      new rgb(0xFF,0xFF,0x66),/* TemperateDesert,          */
      new rgb(0x47,0xB2,0x24),/* TemperateRainForest,      */
      new rgb(0x00,0x3D,0x00),/* TemperateDeciduousForest, */
      new rgb(0x4D,0xDB,0x4D),/* Grassland,                */
      new rgb(0x00,0xCC,0x00),/* TropicalRainForest,       */
      new rgb(0x29,0x7A,0x29),/* TropicalSeasonalForest,   */
      new rgb(0xFF,0xFF,0x99),/* SubtropicalDesert,        */
      new rgb(0x50,0x50,0x50),/* Mountain,                 */
      new rgb(0x99,0x99,0xFF),/* Swamp                     */
    };



    public static Bitmap GetNoisyElevationLandLayer(Map map)
    {
      Bitmap bmp = new Bitmap(map.Width, map.Height);
      bmp.MakeTransparent();

      foreach (VoronoiPolygon vp in map.Mesh.VoronoiPolygons)
      {
        PolygonLandProperties lp = vp.GetProperties();
        if (lp.LandType == PolygonLandProperties.TerrainType.Land)
        {
          //double elevation = PolygonLandProperties.CalculatePolygonAltitudeFromVertices(vp);
          double elevation = (lp.Elevation * 50.0);
          
          rgb rgbValues = TerrainSubtypeColors[(int)lp.LandSubtype];
          int red = Math.Max(0, Math.Min(((int)elevation + rgbValues.Red), 255));
          int blue = Math.Max(0, Math.Min(((int)elevation + rgbValues.Green), 255));
          int green = Math.Max(0, Math.Min(((int)elevation + rgbValues.Blue), 255));

          //int red = Math.Min(rgbValues.Red, 255);
          //int blue = Math.Min(rgbValues.Green, 255);
          //int green = Math.Min(rgbValues.Blue, 255);


          FillPolygon(bmp, vp, Color.FromArgb(red, green, blue), true);
          //FillPolygon(bmp, vp, Color.FromArgb(rgbValues.Red, rgbValues.Green, rgbValues.Blue), true);
        }
      }

      return bmp;
    }

    public static Bitmap GetDelaunayVerticesLayer(Map map)
    {
      Bitmap bmp = new Bitmap(map.Width, map.Height);
      bmp.MakeTransparent();

      using (var graphics = Graphics.FromImage(bmp))
      {
        graphics.FillRectangle(new SolidBrush(Color.Transparent), 0, 0, bmp.Width, bmp.Height);

        Pen blackPen = new Pen(Color.Black);


        foreach (Vertex v in map.Mesh.DelaunayVertices)
        {
          graphics.DrawEllipse(blackPen, (int)v.x, (int)v.y, 1, 1);
        }
      }

      return bmp;
    }

    public static Bitmap GetLakeVerticesLayer(Map map)
    {
      Bitmap bmp = new Bitmap(map.Width, map.Height);
      bmp.MakeTransparent();

      using (var graphics = Graphics.FromImage(bmp))
      {
        graphics.FillRectangle(new SolidBrush(Color.Transparent), 0, 0, bmp.Width, bmp.Height);
        Pen blackPen = new Pen(Color.Orange);

        foreach (Vertex v in map.Mesh.VoronoiVertices)
        {
          if (v.GetProperties().LandType == VertexLandProperties.TerrainType.Lake)
          {
            graphics.DrawEllipse(blackPen, (int)v.x, (int)v.y, 2, 2);
          }
        }
      }

      return bmp;
    }

    public static Bitmap GetLakes(Map map)
    {
      Bitmap bmp = new Bitmap(map.Width, map.Height);
      bmp.MakeTransparent();
      int lakeCount = 0;
      int oceanCount = 0;

      foreach (VoronoiPolygon vp in map.Mesh.VoronoiPolygons)
      {

        PolygonLandProperties vpProps = (PolygonLandProperties)vp.Attachment;
        if (vpProps.LandType == VertexLandProperties.TerrainType.Ocean && vpProps.Border == false)
        {
          oceanCount++;
        }

        if (vpProps.LandType == VertexLandProperties.TerrainType.Lake)
        {
          lakeCount++;
          int blue = (int)(0xA0 + vpProps.Elevation);

          if (blue > 255)
          {
            blue = 255;
          }

          if (blue < 0)
          {
            blue = 0;
          }


          FillPolygon(bmp, vp, Color.FromArgb(0x00, 0x55, blue), false);
        }
      }

      return bmp;
    }

    public static Bitmap GetBorderFaces(Map map)
    {
      Stopwatch watch = new Stopwatch();

      watch.Start();

      Bitmap bmp = new Bitmap(map.Width, map.Height);
      watch.Stop();
      watch.Reset();

      watch.Start();
      foreach (VoronoiPolygon vf in map.BorderPolygons)
      {
        FillPolygon(bmp, vf, Color.Red, false);
      }

      watch.Stop();
      watch.Reset();

      return bmp;
    }

    public static Bitmap GetPolygonElevationLayer(Map map)
    {
      Bitmap bmp = new Bitmap(map.Width, map.Height);
      bmp.MakeTransparent();

      using (var graphics = Graphics.FromImage(bmp))
      {
        Font font = new Font("Arial", 8);

        System.Drawing.SolidBrush drawBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Black);

        graphics.FillRectangle(new SolidBrush(Color.Transparent), 0, 0, bmp.Width, bmp.Height);

        foreach (VoronoiPolygon vp in map.Mesh.VoronoiPolygons)
        {
          int altitude = (int)(vp.GetProperties().Elevation * 100.0);
          //int altitude = (int)PolygonLandProperties.CalculatePolygonAltitudeFromVertices(vp);

          graphics.DrawString(altitude.ToString("F"), font, drawBrush, new PointF((float)(vp.DelaunayVertex.x - 2), (float)vp.DelaunayVertex.y - 2));
        }
      }

      return bmp;
    }

    public static Bitmap GetPolygonMoistureLayer(Map map)
    {
      Bitmap bmp = new Bitmap(map.Width, map.Height);
      bmp.MakeTransparent();

      using (var graphics = Graphics.FromImage(bmp))
      {
        Font font = new Font("Arial", 8);

        System.Drawing.SolidBrush drawBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Red);

        graphics.FillRectangle(new SolidBrush(Color.Transparent), 0, 0, bmp.Width, bmp.Height);

        foreach (VoronoiPolygon vp in map.Mesh.VoronoiPolygons)
        {
          graphics.DrawString(vp.GetProperties().Moisture.ToString("F"), font, drawBrush, new PointF((float)(vp.DelaunayVertex.x - 2), (float)vp.DelaunayVertex.y - 2));
        }
      }

      return bmp;
    }

    public static Bitmap GetElevationLayer(Map map)
    {
      Bitmap bmp = new Bitmap(map.Width, map.Height);
      bmp.MakeTransparent();

      using (var graphics = Graphics.FromImage(bmp))
      {
        Font font = new Font("Arial", 8);

        System.Drawing.SolidBrush drawBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Black);

        graphics.FillRectangle(new SolidBrush(Color.Transparent), 0, 0, bmp.Width, bmp.Height);

        foreach (Vertex v in map.Mesh.VoronoiVertices)
        {
          graphics.DrawString(((VertexLandProperties)(v.Attachment)).Elevation.ToString("F"), font, drawBrush, new PointF((float)(v.x + 2), (float)v.y));
        }
      }

      return bmp;
    }

    public static Bitmap GetVertexMoistureLayer(Map map)
    {
      Bitmap bmp = new Bitmap(map.Width, map.Height);
      bmp.MakeTransparent();

      using (var graphics = Graphics.FromImage(bmp))
      {
        Font font = new Font("Arial", 8);

        System.Drawing.SolidBrush drawBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Black);

        graphics.FillRectangle(new SolidBrush(Color.Transparent), 0, 0, bmp.Width, bmp.Height);

        foreach (Vertex v in map.Mesh.VoronoiVertices)
        {
          graphics.DrawString(v.GetProperties().Moisture.ToString("F"), font, drawBrush, new PointF((float)(v.x + 2), (float)v.y));
        }
      }

      return bmp;
    }

    public static Bitmap GetBiomeOverlayLayer(Map map)
    {
      Bitmap bmp = new Bitmap(map.Width, map.Height);
      bmp.MakeTransparent();

      using (var graphics = Graphics.FromImage(bmp))
      {
        Font font = new Font("Arial", 8);

        System.Drawing.SolidBrush drawBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Black);

        graphics.FillRectangle(new SolidBrush(Color.Transparent), 0, 0, bmp.Width, bmp.Height);

        foreach (VoronoiPolygon vp in map.Mesh.VoronoiPolygons)
        {
          graphics.DrawString(vp.GetProperties().LandSubtype.ToString(), font, drawBrush, new PointF((float)(vp.DelaunayVertex.x + 2), (float)vp.DelaunayVertex.y));
        }
      }

      return bmp;
    }

    public static Bitmap GetTemperatureValueOverlayLayer(Map map)
    {
      Bitmap bmp = new Bitmap(map.Width, map.Height);
      bmp.MakeTransparent();

      using (var graphics = Graphics.FromImage(bmp))
      {
        Font font = new Font("Arial", 8);

        System.Drawing.SolidBrush drawBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Black);

        graphics.FillRectangle(new SolidBrush(Color.Transparent), 0, 0, bmp.Width, bmp.Height);

        foreach (VoronoiPolygon vp in map.Mesh.VoronoiPolygons)
        {
          graphics.DrawString(vp.GetProperties().Temperature.ToString("F"), font, drawBrush, new PointF((float)(vp.DelaunayVertex.x + 2), (float)vp.DelaunayVertex.y));
        }
      }

      return bmp;
    }

    public static Bitmap GetRiverLayer(Map map)
    {
      Bitmap bmp = new Bitmap(map.Width, map.Height);
      bmp.MakeTransparent();

      using (var graphics = Graphics.FromImage(bmp))
      {
        graphics.FillRectangle(new SolidBrush(Color.Transparent), 0, 0, bmp.Width, bmp.Height);
        Pen bluePen = new Pen(Color.FromArgb(0x00, 0x55, 0xA0));
        bluePen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
        bluePen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
        bluePen.EndCap = System.Drawing.Drawing2D.LineCap.Round;

        foreach(Edge edge in map.Mesh.Edges)
        {
          EdgeProperties ep = (EdgeProperties)edge.Attachment;

          
          if (ep != null && ep.RiverVolume > 0)
          {
            bluePen.Width = 1.0f + ((float)ep.RiverVolume);

            //graphics.DrawLine(bluePen, (int)edge.VoronoiV1.x, (int)edge.VoronoiV1.y, (int)edge.VoronoiV2.x, (int)edge.VoronoiV2.y);

            //*
            for (int i = 0; i < ep.NoisyPath0.Count - 1; i++)
            {
              graphics.DrawLine(bluePen, (int)ep.NoisyPath0[i].x, (int)ep.NoisyPath0[i].y, (int)ep.NoisyPath0[i + 1].x, (int)ep.NoisyPath0[i + 1].y);
            }

            graphics.DrawLine(bluePen, (int)ep.NoisyPath0[ep.NoisyPath0.Count - 1].x, (int)ep.NoisyPath0[ep.NoisyPath0.Count - 1].y, (int)ep.NoisyPath1[ep.NoisyPath1.Count - 2].x, (int)ep.NoisyPath1[ep.NoisyPath1.Count - 2].y);

            for (int i = 0; i < ep.NoisyPath1.Count - 1; i++)
            {
              graphics.DrawLine(bluePen, (int)ep.NoisyPath1[i].x, (int)ep.NoisyPath1[i].y, (int)ep.NoisyPath1[i + 1].x, (int)ep.NoisyPath1[i + 1].y);
            }
            /**/
          }
        }
      }

      return bmp;
    }

    public static Bitmap GetMountainLayer(Map map)
    {
      Bitmap bmp = new Bitmap(map.Width, map.Height);
      bmp.MakeTransparent();

      foreach (VoronoiPolygon vp in map.Mesh.VoronoiPolygons)
      {
        PolygonLandProperties lp = (PolygonLandProperties)vp.Attachment;
        double elevation = lp.Elevation;

        if (lp.LandSubtype == PolygonLandProperties.TerrainSubtype.Mountain)
        {
          int colorValue = (0x50 + (int)elevation);
          if (colorValue > 255)
          {
            colorValue = 255;
          }

          if (colorValue < 0)
          {
            colorValue = 0;
          }

          FillPolygon(bmp, vp, Color.FromArgb(colorValue, colorValue, colorValue), true);
        }
      }

      return bmp;
    }

    public static Bitmap GetVoronoiLayer(Map map)
    {
      Bitmap bmp = new Bitmap(map.Width, map.Height);
      bmp.MakeTransparent();

      using (var graphics = Graphics.FromImage(bmp))
      {
        graphics.FillRectangle(new SolidBrush(Color.Transparent), 0, 0, bmp.Width, bmp.Height);

        Pen blackPen = new Pen(Color.Black);

        foreach (VoronoiPolygon vf in map.Mesh.VoronoiPolygons)
        {
          foreach (DelaunayTriangulator.Edge edge in vf.Edges)
          {
            if (edge.VoronoiV1 != null && edge.VoronoiV2 != null)
            {
              graphics.DrawLine(blackPen, (int)edge.VoronoiV1.x, (int)edge.VoronoiV1.y, (int)edge.VoronoiV2.x, (int)edge.VoronoiV2.y);
            }
          }
        }
      }

      return bmp;
    }

    public static Bitmap GetTemperatureLayer(Map map)
    {
      Bitmap bmp = new Bitmap(map.Width, map.Height);
      bmp.MakeTransparent();

      foreach (VoronoiPolygon vp in map.Mesh.VoronoiPolygons)
      {
        if (vp.GetProperties().LandType == VertexLandProperties.TerrainType.Land)
        {
          double temperature = vp.GetProperties().Temperature;

          int red = (int)((temperature) * 0xFF);
          int blue = (int)((temperature) * 0xFF);

          if (red > 255)
          {
            red = 255;
          }

          if (blue > 255)
          {
            blue = 255;
          }

          blue = 255 - blue;


          FillPolygon(bmp, vp, Color.FromArgb(red, 0xFF, blue), true);
        }
      }

      return bmp;
    }

    public static Bitmap GetLatitudeDividers(Map map)
    {
      Bitmap bmp = new Bitmap(map.Width, map.Height);
      bmp.MakeTransparent();

      using (var graphics = Graphics.FromImage(bmp))
      {
        graphics.FillRectangle(new SolidBrush(Color.Transparent), 0, 0, bmp.Width, bmp.Height);
        Pen blackPen = new Pen(Color.Black);
        blackPen.Width = 3;

        graphics.DrawLine(blackPen, 0, (int)(0.00 * map.Height - 1), map.Width - 1, (int)(0.00 * map.Height - 1));
        graphics.DrawLine(blackPen, 0, (int)(0.11 * map.Height - 1), map.Width - 1, (int)(0.11 * map.Height - 1));
        graphics.DrawLine(blackPen, 0, (int)(0.33 * map.Height - 1), map.Width - 1, (int)(0.33 * map.Height - 1));
        graphics.DrawLine(blackPen, 0, (int)(0.50 * map.Height - 1), map.Width - 1, (int)(0.50 * map.Height - 1));
        graphics.DrawLine(blackPen, 0, (int)(0.67 * map.Height - 1), map.Width - 1, (int)(0.67 * map.Height - 1));
        graphics.DrawLine(blackPen, 0, (int)(0.89 * map.Height - 1), map.Width - 1, (int)(0.89 * map.Height - 1));
        graphics.DrawLine(blackPen, 0, (int)(1.00 * map.Height - 1), map.Width - 1, (int)(1.00 * map.Height - 1));
      }
      return bmp;
    }
  }
}
