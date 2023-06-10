using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using DelaunayTriangulator;
using System.Diagnostics;
using RandomExtensions;

namespace MapGenerator.MapModel
{
  public class Map
  {
    public const double MINIMUM_PERCENTAGE_OF_RIVERS = 0.03;
    public const double MAXIMUM_PERCENTAGE_OF_RIVERS = 0.08;
    public const double MOUNTAIN_ELEVATION = 0.40;
    public const int NUMBER_OF_CELLULAR_AUTOMATA_ITERATIONS = 4;
    public const double OCEAN_ELEVATION = 0.0;
    public const double POLYGON_ELEVATION_INCREASE = 0.05;
    public const int CELLULAR_AUTOMATA_INNER_HEIGHT_DIMENSION = 94;
    public const int CELLULAR_AUTOMATA_INNER_WIDTH_DIMENSION = 94;
    public const int CELLULAR_AUTOMATA_OUTER_HEIGHT_DIMENSION = 100;
    public const int CELLULAR_AUTOMATA_OUTER_WIDTH_DIMENSION = 100;
    public const double MOISTURE_DEGRADATION_SCALAR = 0.90;
    public const int NUM_POINTS_IN_FAULT_LINE_MESH = 15;
    public const double MOUNTAIN_START_PERCENTAGE = 0.005;
    public const double MOUNTAIN_ELEVATION_STEP = 0.12;
    private int m_width;
    private int m_height;
    private double m_clipPercentX = 0.0;
    public Random m_mapRandom = new Random((int)DateTime.Now.Ticks);
    private List<Vertex> m_points = new List<Vertex>();
    private List<Edge> m_edges = new List<Edge>();
    private double m_clipPercentY = 0.0;
    private int m_seed;
    private int m_numberOfPoints = 8000;
    private Random m_random;
    private Mesh m_mesh;
    private Mesh m_faultLineMesh;
    private List<List<VoronoiPolygon>> m_continents = new List<List<VoronoiPolygon>>();

    public Mesh FaultLineMesh { get { return m_faultLineMesh; } set { m_faultLineMesh = value; } }
    public List<VoronoiPolygon> BorderPolygons = new List<VoronoiPolygon>();
    public delegate void LogStatus(string message);
    public delegate void UpdatePercentage(double percent);
    public event LogStatus LogStatusMessage;
    public event UpdatePercentage UpdatePercentageComplete;

    private void LogMessage(string message)
    {
      if (LogStatusMessage != null)
      {
        LogStatusMessage(message);
      }
    }

    private void UpdatePercent(double percent)
    {
      if (UpdatePercentageComplete != null)
      {
        UpdatePercentageComplete(percent);
      }
    }


    public Mesh Mesh { get { return m_mesh; } set { m_mesh = value; } }
    public string cellularAutomata { get; set; }
    public int Width { get { return m_width; } }
    public int Height { get { return m_height; } }
    public double ClipPercentX { get { return m_clipPercentX; } set { m_clipPercentX = value; } }
    public double ClipPercentY { get { return m_clipPercentY; } set { m_clipPercentY = value; } }

    public Map(int width, int height, int seed, int points)
    {
      m_width = width;
      m_height = height;
      m_seed = seed;
      m_random = new Random(m_seed);
      m_numberOfPoints = points;
    }

    public void generate()
    {
      Stopwatch watch = new Stopwatch();

      LogMessage("Generating Random points");
      watch.Start();
      List<Vertex> vertices = new List<Vertex>();
      generateRandomPoints(vertices, m_numberOfPoints, m_random, m_height, m_width);
      watch.Stop();
      LogMessage(string.Format("{0} milliseconds elapsed.", watch.ElapsedMilliseconds));
      watch.Reset();

      LogMessage("Processing Points into Mesh");
      watch.Start();
      m_mesh = new Mesh(vertices);
      m_mesh.ProcessPointsIntoMesh();
      watch.Stop();
      LogMessage(string.Format("{0} milliseconds elapsed.", watch.ElapsedMilliseconds));
      watch.Reset();

      LogMessage("Averaging vertices");
      watch.Start();
      List<Vertex> dVertices = averageVertices(m_mesh, m_width, m_height);
      watch.Stop();
      LogMessage(string.Format("{0} milliseconds elapsed.", watch.ElapsedMilliseconds));

      LogMessage("Processing Averaged Vertices into Mesh");
      watch.Start();
      m_mesh = new Mesh(dVertices);
      m_mesh.ProcessPointsIntoMesh();
      watch.Stop();
      LogMessage(string.Format("{0} milliseconds elapsed.", watch.ElapsedMilliseconds));

      watch.Reset();
      LogMessage("Assigning Properties to each Vertex, Edge and Polygon");
      watch.Start();
      assignPropertiesToEdgesVerticesAndPolygons();
      watch.Stop();
      LogMessage(string.Format("{0} milliseconds elapsed.", watch.ElapsedMilliseconds));
      watch.Reset();

      LogMessage("Generating Land Shapes");
      watch.Start();
      generateLand();
      watch.Stop();
      LogMessage(string.Format("{0} milliseconds elapsed.", watch.ElapsedMilliseconds));
      watch.Reset();

      LogMessage("Marking Border Edges");
      watch.Start();
      markBorderEdges();
      watch.Stop();
      LogMessage(string.Format("{0} milliseconds elapsed.", watch.ElapsedMilliseconds));
      watch.Reset();

      LogMessage("Marking Border polygons");
      watch.Start();
      markBorderPolygons();
      watch.Stop();
      LogMessage(string.Format("{0} milliseconds elapsed.", watch.ElapsedMilliseconds));
      watch.Reset();

      LogMessage("Marking Border polygons");
      watch.Start();
      parseContinents();
      watch.Stop();
      LogMessage(string.Format("{0} milliseconds elapsed.", watch.ElapsedMilliseconds));
      watch.Reset();

      LogMessage("Marking Oceans");
      watch.Start();
      markOceans();
      watch.Stop();
      LogMessage(string.Format("{0} milliseconds elapsed.", watch.ElapsedMilliseconds));
      watch.Reset();


      LogMessage("Marking Elevations");
      watch.Start();
      assignElevations();
      watch.Stop();
      LogMessage(string.Format("{0} milliseconds elapsed.", watch.ElapsedMilliseconds));
      watch.Reset();


      LogMessage("Marking downslopes");
      watch.Start();
      markDownslopes();
      watch.Stop();
      LogMessage(string.Format("{0} milliseconds elapsed.", watch.ElapsedMilliseconds));
      watch.Reset();

      LogMessage("Marking Mountains");
      watch.Start();
      markMountains();
      watch.Stop();
      LogMessage(string.Format("{0} milliseconds elapsed.", watch.ElapsedMilliseconds));
      watch.Reset();


      LogMessage("Unmarking mountains around lakes");
      watch.Start();
      foreach (VoronoiPolygon vp in m_mesh.VoronoiPolygons)
      {
        PolygonLandProperties landProps = (PolygonLandProperties)vp.Attachment;
        if (landProps.LandType == VertexLandProperties.TerrainType.Lake)
        {
          foreach (VoronoiPolygon neighbor in vp.Neighbors)
          {
            PolygonLandProperties neighborProps = (PolygonLandProperties)neighbor.Attachment;
            {
              if (neighborProps.LandSubtype == PolygonLandProperties.TerrainSubtype.Mountain)
              {
                neighborProps.LandSubtype = PolygonLandProperties.TerrainSubtype.Grassland;
              }
            }
          }
        }
      }
      watch.Stop();
      LogMessage(string.Format("{0} milliseconds elapsed.", watch.ElapsedMilliseconds));
      watch.Reset();


      LogMessage("Marking Rivers");
      watch.Start();
      markRivers();
      watch.Stop();
      LogMessage(string.Format("{0} milliseconds elapsed.", watch.ElapsedMilliseconds));
      watch.Reset();

      LogMessage("Marking Watersheds");
      watch.Start();
      markWaterSheds();
      watch.Stop();
      LogMessage(string.Format("{0} milliseconds elapsed.", watch.ElapsedMilliseconds));
      watch.Reset();

      LogMessage("Assigning Vertex Moisture");
      watch.Start();
      assignVertexMoisture();
      watch.Stop();
      LogMessage(string.Format("{0} milliseconds elapsed.", watch.ElapsedMilliseconds));
      watch.Reset();

      LogMessage("Assigning Polygon Moisture");
      watch.Start();
      assignPolygonMoisture();
      watch.Stop();
      LogMessage(string.Format("{0} milliseconds elapsed.", watch.ElapsedMilliseconds));
      watch.Reset();

      LogMessage("Assigning Temperatures");
      watch.Start();
      assignTemperatures();
      watch.Stop();
      LogMessage(string.Format("{0} milliseconds elapsed.", watch.ElapsedMilliseconds));
      watch.Reset();

      LogMessage("Assignign Biomes");
      watch.Start();
      assignBiomes();
      watch.Stop();
      LogMessage(string.Format("{0} milliseconds elapsed.", watch.ElapsedMilliseconds));
      watch.Reset();
    }

    private void parseContinents()
    {
      Dictionary<Vertex, VoronoiPolygon> processedPolygons = new Dictionary<Vertex, VoronoiPolygon>();

      foreach (VoronoiPolygon vp in m_mesh.VoronoiPolygons)
      {
        if (processedPolygons.ContainsKey(vp.DelaunayVertex) || vp.GetProperties().LandType != VertexLandProperties.TerrainType.Land)
        {
          continue;
        }

        List<VoronoiPolygon> continent = new List<VoronoiPolygon>();

        //flood fill to find continents
        Queue<VoronoiPolygon> polygonsToProcess = new Queue<VoronoiPolygon>();
        polygonsToProcess.Enqueue(vp);
        processedPolygons.Add(vp.DelaunayVertex, vp);

        while (polygonsToProcess.Count > 0)
        {
          VoronoiPolygon currentPolygon = polygonsToProcess.Dequeue();
          continent.Add(currentPolygon);

          foreach (VoronoiPolygon neighbor in currentPolygon.Neighbors)
          {
            if (processedPolygons.ContainsKey(neighbor.DelaunayVertex) || neighbor.GetProperties().LandType != VertexLandProperties.TerrainType.Land)
            {
              continue;
            }
            polygonsToProcess.Enqueue(neighbor);
            processedPolygons.Add(neighbor.DelaunayVertex, neighbor);
          }
        }

        if (continent.Count > 0)
        {
          m_continents.Add(continent);
        }
      }
    }


    private void assignPropertiesToEdgesVerticesAndPolygons()
    {
      foreach (VoronoiPolygon vp in m_mesh.VoronoiPolygons)
      {
        vp.Attachment = new PolygonLandProperties();
      }

      //assign voronoi vertex properties to each voronoi vertex
      foreach (Vertex v in m_mesh.VoronoiVertices)
      {
        v.Attachment = new VertexLandProperties();
      }

      //assign voronoi vertex properties to each edge
      foreach (Edge e in m_mesh.Edges)
      {
        e.Attachment = new EdgeProperties();
      }
    }

    private static List<Vertex> averageVertices(Mesh mesh, int maxWidth, int maxHeight)
    {
      List<Vertex> dVertices = new List<Vertex>();

      foreach (VoronoiPolygon vf in mesh.VoronoiPolygons)
      {
        double x = 0;
        double y = 0;

        foreach (Vertex v in vf.Vertices)
        {
          if (v != null)
          {
            x += v.x;
            y += v.y;
          }
        }

        x /= (double)vf.Vertices.Count;
        y /= (double)vf.Vertices.Count;

        Vertex aveVertex = new Vertex(x, y);
        if (aveVertex.x > 0 && aveVertex.x < maxWidth && aveVertex.y > 0 && aveVertex.y < maxHeight)
        {
          dVertices.Add(aveVertex);
        }
      }

      return dVertices;
    }

    //Temperatures are a combination of Latitude and Elevation
    //For this generator, the range of temperatures can vary between 0 to 100 degrees.
    //
    //
    //                _.-,=_"""--,_               +-------+0    0
    //             .-" =/7"   _  .3#"=.                   |1        Polar Zone
    //           ,#7  " "  ,//)#d#######=.        +-------+2   50      
    //         ,/ "      # ,i-/###########=               |3        
    //        /         _)#sm###=#=# #######\             |4        Temperate Zone
    //       /         (#/"_`;\//#=#\-#######\            |5        
    //      /         ,d####-_.._.)##P########\   +-------+6   60     
    //     ,        ,"############\\##bi- `\| Y.          |7        
    //     |       .d##############b\##P'   V  |          |8        
    //     |\      '#################!",       |          |9  100   Tropical Zone
    //     |C.       \###=############7        |          |10        
    //     '###.           )#########/         '          |11       
    //      \#(             \#######|         /   +-------+12   60     
    //       \B             /#######7 /      /            |13        
    //        \             \######" /"     /             |14        Temperate Zone
    //         `.            \###7'       ,'              |15
    //           "-_          `"'      ,-'        +-------+16   50   Polar Zone
    //              "-._           _.-"                   |17
    //                  """"---""""               +-------+18    0
    //     
    //                                             Elev    Temp
    //                             __         +---+ 80 = -40
    //                            /  \            |
    //                           /    \       +---+ 60 = -30
    //                          /      \          | 
    //                         /        \     +---+ 40 = -20
    //                     .'\/          \        |
    //                   _/   \           \   +---+ 20 = -10
    //                  / .-'  `-.         \      |
    //                 / .   .'   \         \ +---+  0 = 0

    private double getTemperatureFromLatitude(int y)
    {
      //Standard Equation of a circle
      // (x-h)^2 + (y-k)^2 = r^2
      // x^2 + y^2 = r^2 
      // x^2 = r^2 - y^2
      // x = +/- sqrt(r^2 - y^2)
      if (y < 0)
      {
        y = 0;
      }

      if (y > Height - 1)
      {
        y = Height - 1;
      }

      double radius = (double)Height / 2.0;
      double latitude = y - radius;
      double temperature = Math.Sqrt((radius * radius) - (latitude * latitude));

      //normalize it
      temperature = temperature / radius;

      //square it
      temperature = temperature * temperature;

      return temperature;
    }

    private double getTemperatureOffsetByElevation(double elevation)
    {
      return elevation / -200.0; ;
    }

    private void assignTemperatures()
    {
      //set each individual vertex temperature
      foreach (Vertex v in m_mesh.VoronoiVertices)
      {
        double initialTemp = getTemperatureFromLatitude((int)v.y);
        double elevation = v.GetProperties().Elevation;
        double elevationTempDrop = getTemperatureOffsetByElevation(elevation);
        initialTemp += elevationTempDrop;

        if (initialTemp < 0.0)
        {
          initialTemp = 0.0;
        }

        if (initialTemp > 1.0)
        {
          initialTemp = 1.0;
        }

        v.GetProperties().Temperature = initialTemp;
      }

      //set polygon temperature
      foreach (VoronoiPolygon vp in m_mesh.VoronoiPolygons)
      {
        double totalTemperature = 0.0;

        foreach (Vertex polygonVertex in vp.Vertices)
        {
          totalTemperature += polygonVertex.GetProperties().Temperature;
        }

        vp.GetProperties().Temperature = totalTemperature / vp.Vertices.Count;
      }
    }

    private void assignPolygonMoisture()
    {
      foreach (VoronoiPolygon poly in m_mesh.VoronoiPolygons)
      {
        if (poly.GetProperties().LandType == VertexLandProperties.TerrainType.Ocean)
        {
          poly.GetProperties().Moisture = 1.0;
        }
        else
        {
          double total = 0.0;
          int count = 0;
          foreach (Vertex v in poly.Vertices)
          {
            if (v.GetProperties().LandType != VertexLandProperties.TerrainType.Ocean)
            {
              total += v.GetProperties().Moisture;
              count++;
            }
          }
          if (count > 0)
          {
            poly.GetProperties().Moisture = total / count;
          }
        }
      }
    }

    private void markOceans()
    {
      Queue<VoronoiPolygon> polygonsToProcess = new Queue<VoronoiPolygon>();

      foreach (VoronoiPolygon vp in BorderPolygons)
      {
        polygonsToProcess.Enqueue(vp);
      }

      //polygons already processed
      Dictionary<Vertex, VoronoiPolygon> processedPolygons = new Dictionary<Vertex, VoronoiPolygon>();

      int polygonsProcessed = 0;

      while (polygonsToProcess.Count > 0)
      {
        polygonsProcessed++;
        VoronoiPolygon currentPolygonBeingProcessed = polygonsToProcess.Dequeue();
        PolygonLandProperties currentPolyProps = (PolygonLandProperties)currentPolygonBeingProcessed.Attachment;

        if (currentPolyProps.LandType == VertexLandProperties.TerrainType.Land || processedPolygons.ContainsKey(currentPolygonBeingProcessed.DelaunayVertex))
        {
          continue;
        }

        processedPolygons.Add(currentPolygonBeingProcessed.DelaunayVertex, currentPolygonBeingProcessed);

        currentPolyProps.LandType = VertexLandProperties.TerrainType.Ocean;
        currentPolyProps.Elevation = 0.0;

        foreach (Vertex v in currentPolygonBeingProcessed.Vertices)
        {
          VertexLandProperties vertexProps = (VertexLandProperties)v.Attachment;
          vertexProps.LandType = VertexLandProperties.TerrainType.Ocean;
          vertexProps.Elevation = 0.0;
        }

        foreach (VoronoiPolygon neighbor in currentPolygonBeingProcessed.Neighbors)
        {
          PolygonLandProperties currentNeighborProps = (PolygonLandProperties)neighbor.Attachment;
          if (currentNeighborProps.LandType == VertexLandProperties.TerrainType.Lake && !processedPolygons.ContainsKey(neighbor.DelaunayVertex))
          {
            polygonsToProcess.Enqueue(neighbor);
          }
        }
      }
    }

    private void assignVertexMoisture()
    {
      Dictionary<Vertex, bool> processedVertices = new Dictionary<Vertex, bool>();
      Queue<Vertex> verticesToProcess = new Queue<Vertex>();

      foreach (Edge edge in m_mesh.Edges)
      {
        if (edge.VoronoiV1 != null && !processedVertices.ContainsKey(edge.VoronoiV1))
        {
          processedVertices.Add(edge.VoronoiV1, true);
          if (edge.VoronoiV1.GetProperties().LandType == VertexLandProperties.TerrainType.Lake)
          {
            edge.VoronoiV1.GetProperties().Moisture = 1.0;
          }

          if (edge.GetProperties().RiverVolume > 0)
          {
            edge.VoronoiV1.GetProperties().Moisture = 1.0;
          }

          if (edge.VoronoiV1.GetProperties().LandType == VertexLandProperties.TerrainType.Lake || edge.GetProperties().RiverVolume > 0)
          {
            verticesToProcess.Enqueue(edge.VoronoiV1);
          }
        }

        if (edge.VoronoiV2 != null && !processedVertices.ContainsKey(edge.VoronoiV2))
        {
          processedVertices.Add(edge.VoronoiV2, true);
          if (edge.VoronoiV2.GetProperties().LandType == VertexLandProperties.TerrainType.Lake)
          {
            edge.VoronoiV2.GetProperties().Moisture = 1.0;
          }

          if (edge.GetProperties().RiverVolume > 0)
          {
            edge.VoronoiV2.GetProperties().Moisture = 1.0;
          }

          if (edge.VoronoiV2.GetProperties().LandType == VertexLandProperties.TerrainType.Lake || edge.GetProperties().RiverVolume > 0)
          {
            verticesToProcess.Enqueue(edge.VoronoiV2);
          }
        }
      }

      while (verticesToProcess.Count > 0)
      {
        Vertex v = verticesToProcess.Dequeue();

        foreach (Vertex neighbor in v.GetNeighbors(m_mesh))
        {
          double newMoisture = v.GetProperties().Moisture * MOISTURE_DEGRADATION_SCALAR;

          if (newMoisture > neighbor.GetProperties().Moisture)
          {
            neighbor.GetProperties().Moisture = newMoisture;

            if (neighbor.GetProperties().LandType != VertexLandProperties.TerrainType.Ocean)
            {
              verticesToProcess.Enqueue(neighbor);
            }
          }
        }
      }

      //// Salt water
      foreach (Vertex v in m_mesh.VoronoiVertices)
      {
        if (v.GetProperties().LandType == VertexLandProperties.TerrainType.Ocean)
        {
          v.GetProperties().Moisture = 1.0;
        }
      }
    }

    private void markRivers()
    {
      //gather a list of possible starting vertices in the mountains
      List<Vertex> candidateVertices = new List<Vertex>();
      foreach (VoronoiPolygon polygon in m_mesh.VoronoiPolygons)
      {
        if (((PolygonLandProperties)polygon.Attachment).LandSubtype == PolygonLandProperties.TerrainSubtype.Mountain)
        {
          foreach (Vertex v in polygon.Vertices)
          {
            if (!candidateVertices.Contains(v))
            {
              candidateVertices.Add(v);
            }
          }
        }
      }

      Random random = new Random((int)DateTime.Now.Ticks);

      int numRiversToMake = (int)(random.NextDoubleRange(MINIMUM_PERCENTAGE_OF_RIVERS, MAXIMUM_PERCENTAGE_OF_RIVERS) * candidateVertices.Count);
      Dictionary<Vertex, List<Edge>> indexedEdges = m_mesh.getIndexedEdgesByVoronoiVertex();

      while (numRiversToMake > 0)
      {
        Vertex currentVertex = candidateVertices[random.Next(candidateVertices.Count - 1)];
        candidateVertices.Remove(currentVertex);

        VertexLandProperties vertexProps = (VertexLandProperties)currentVertex.Attachment;
        Edge currentEdge = vertexProps.DownslopeEdge;

        //some edges are null, likely because of the elevation issues
        if (currentEdge == null)
        {
          continue;
        }

        EdgeProperties currentEdgeProps = currentEdge.GetProperties();

        double currentElevation = vertexProps.Elevation;

        while (currentElevation > 0 && currentEdge != null)
        {
          currentEdgeProps.RiverVolume++;

          if (currentEdge.VoronoiV1 == currentVertex)
          {
            currentVertex = currentEdge.VoronoiV2;
          }
          else
          {
            currentVertex = currentEdge.VoronoiV1;
          }

          candidateVertices.Remove(currentVertex);
          vertexProps = (VertexLandProperties)currentVertex.Attachment;
          currentEdge = vertexProps.DownslopeEdge;
          currentElevation = vertexProps.Elevation;

          if (currentEdge != null)
          {
            currentEdgeProps = (EdgeProperties)currentEdge.Attachment;
          }

        }

        --numRiversToMake;
      }

    }

    /*
    private void markMountains()
    {
      Dictionary<int, int> distribution = new Dictionary<int, int>();
      List<VoronoiPolygon> landPolygons = new List<VoronoiPolygon>();

      foreach (VoronoiPolygon vp in m_mesh.VoronoiPolygons)
      {
        if (vp.GetProperties().LandType == VertexLandProperties.TerrainType.Land)
        {
          landPolygons.Add(vp);
        }
      }

      landPolygons.Sort
      (
        delegate(VoronoiPolygon vp1, VoronoiPolygon vp2)
        {
          return vp2.Neighbors.Count.CompareTo(vp1.Neighbors.Count);
        }
      );

      //get the starting polygons based on the greatest number of edges / neighbors
      int numberOfStartingMountainsToMake = Math.Max(5, (int)(landPolygons.Count * MOUNTAIN_START_PERCENTAGE));
      List<VoronoiPolygon> startingPolygons = new List<VoronoiPolygon>();
      for (int i = 0; i < numberOfStartingMountainsToMake; ++i)
      {
        startingPolygons.Add(landPolygons[i]);
      }

      Queue<VoronoiPolygon> mountainsToThicken = new Queue<VoronoiPolygon>();

      //add neighbor polygons based on distance from starting polygon
      foreach (VoronoiPolygon startOfMountainPolygon in startingPolygons)
      {
        PolygonLandProperties startOfMountainPolygonProps = startOfMountainPolygon.GetProperties();
        startOfMountainPolygonProps.LandSubtype = PolygonLandProperties.TerrainSubtype.Mountain;

        if (startOfMountainPolygonProps.Elevation < 0.95)
        {
          startOfMountainPolygonProps.Elevation = 0.95;
        }

        double distanceFromMountainStart = double.NegativeInfinity;

        Queue<VoronoiPolygon> openFronts = new Queue<VoronoiPolygon>();
        openFronts.Enqueue(startOfMountainPolygon);
        mountainsToThicken.Enqueue(startOfMountainPolygon);
        while (openFronts.Count > 0)
        {
          VoronoiPolygon currentPoly = openFronts.Dequeue();
          PolygonLandProperties currentPolyProps = currentPoly.GetProperties();

          currentPolyProps.LandSubtype = PolygonLandProperties.TerrainSubtype.Mountain;

          if (currentPolyProps.Elevation < 0.80)
          {
            currentPoly.GetProperties().Elevation = 0.80;
          }
          distanceFromMountainStart = currentPoly.DelaunayVertex.distanceTo(startOfMountainPolygon.DelaunayVertex);

          //find the furthest polygon
          VoronoiPolygon furthestPolygon = null;
          double longestDistance = double.NegativeInfinity;

          foreach (VoronoiPolygon neighbor in currentPoly.Neighbors)
          {
            if (neighbor.GetProperties().LandType == VertexLandProperties.TerrainType.Land)
            {
              double distance = neighbor.DelaunayVertex.distanceTo(startOfMountainPolygon.DelaunayVertex);

              if (distance > longestDistance)
              {
                longestDistance = distance;
                furthestPolygon = neighbor;
              }
            }
          }

          //add it to the open front
          if (furthestPolygon != null && furthestPolygon.GetProperties().LandSubtype != PolygonLandProperties.TerrainSubtype.Mountain && longestDistance > distanceFromMountainStart)
          {
            openFronts.Enqueue(furthestPolygon);
          }
        }
      }

      //Thicken Mountains
      //while (mountainsToThicken.Count > 0)
      //{
      //  VoronoiPolygon currentPoly = mountainsToThicken.Dequeue();
      //  foreach (VoronoiPolygon neighbor in currentPoly.Neighbors)
      //  {
      //    PolygonLandProperties props = neighbor.GetProperties();
      //    if (props.LandType == VertexLandProperties.TerrainType.Land)
      //    {
      //      props.LandSubtype = PolygonLandProperties.TerrainSubtype.Mountain;
      //      props.Elevation = 0.80;
      //    }
      //  }
      //}

      foreach (VoronoiPolygon vp in landPolygons)
      {
        if (vp.GetProperties().LandSubtype == PolygonLandProperties.TerrainSubtype.Mountain)
        {
          //thickenup all the lines
          //foreach (VoronoiPolygon neighbor in vp.Neighbors)
          //{
          //  PolygonLandProperties props = neighbor.GetProperties();
          //  if (props.LandType == VertexLandProperties.TerrainType.Land)
          //  {
          //    props.LandSubtype = PolygonLandProperties.TerrainSubtype.Mountain;
          //    props.Elevation = 0.80;
          //  }
          //}


          //try to add a third neighbor when there are too few
          //int loop = 0;
          //while (countAdjacentMountains(vp) < 4 && loop < 2)
          //{
          //  loop++;
          //  foreach (VoronoiPolygon neighbor in vp.Neighbors)
          //  {
          //    PolygonLandProperties neighborProps = neighbor.GetProperties();
          //
          //    if (neighborProps.LandType == VertexLandProperties.TerrainType.Land && neighborProps.LandSubtype != PolygonLandProperties.TerrainSubtype.Mountain)
          //    {
          //      neighborProps.LandSubtype = PolygonLandProperties.TerrainSubtype.Mountain;
          //      break;
          //    }
          //  }
          //}
        }
      }
    }

    private int countAdjacentMountains(VoronoiPolygon vp)
    {
      int mountains = 0;

      foreach (VoronoiPolygon neighbor in vp.Neighbors)
      {
        if (neighbor.GetProperties().LandSubtype == PolygonLandProperties.TerrainSubtype.Mountain)
        {
          mountains++;
        }
      }

      return mountains;
    }
    /**/

    private void assignElevations()
    {
      Queue<Vertex> verticesToProcess = new Queue<Vertex>();
      Dictionary<Vertex, List<Edge>> edgeTable = m_mesh.getIndexedEdgesByVoronoiVertex();

      foreach (Vertex v in m_mesh.VoronoiVertices)
      {
        if (((VertexLandProperties)(v.Attachment)).LandType == VertexLandProperties.TerrainType.Ocean)
        {
          ((VertexLandProperties)(v.Attachment)).Elevation = 0.0;
          verticesToProcess.Enqueue(v);
        }
      }

      while (verticesToProcess.Count > 0)
      {
        Vertex currentVertex = verticesToProcess.Dequeue();
        VertexLandProperties currentVertexProps = (VertexLandProperties)currentVertex.Attachment;

        if (edgeTable.ContainsKey(currentVertex))
        {
          //get Neighboring Vertices
          List<Edge> neighboringEdges = edgeTable[currentVertex];
          List<Vertex> neighborVertices = new List<Vertex>();
          foreach (Edge e in neighboringEdges)
          {
            if (!e.VoronoiV1.Equals(currentVertex))
            {
              neighborVertices.Add(e.VoronoiV1);
            }

            if (!e.VoronoiV2.Equals(currentVertex))
            {
              neighborVertices.Add(e.VoronoiV2);
            }
          }

          foreach (Vertex neighbor in neighborVertices)
          {
            VertexLandProperties neighborProps = (VertexLandProperties)neighbor.Attachment;
            double newElevation = currentVertexProps.Elevation;

            if (neighborProps.LandType != VertexLandProperties.TerrainType.Lake)
            {
              newElevation += POLYGON_ELEVATION_INCREASE;
            }

            if (newElevation < neighborProps.Elevation)
            {
              neighborProps.Elevation = newElevation;
              verticesToProcess.Enqueue(neighbor);
            }
          }
        }
      }

      foreach (VoronoiPolygon vp in m_mesh.VoronoiPolygons)
      {
        PolygonLandProperties lp = (PolygonLandProperties)vp.Attachment;
        lp.Elevation = PolygonLandProperties.CalculatePolygonAltitudeFromVertices(vp);
      }
    }

    //*
    private void markMountains()
    {
      foreach (VoronoiPolygon currentPolygonBeingProcessed in m_mesh.VoronoiPolygons)
      {
        PolygonLandProperties currentPolyProps = (PolygonLandProperties)currentPolygonBeingProcessed.Attachment;

        if (PolygonLandProperties.CalculatePolygonAltitudeFromVertices(currentPolygonBeingProcessed) > MOUNTAIN_ELEVATION && currentPolyProps.LandType != VertexLandProperties.TerrainType.Lake)
        {
          PolygonLandProperties lp = (PolygonLandProperties)currentPolygonBeingProcessed.Attachment;
          lp.LandSubtype = PolygonLandProperties.TerrainSubtype.Mountain;
        }
      }
    }/**/

    /* Assign Elevation by distance from starting peak
    private void assignElevations()
    {
      Dictionary<Vertex, List<Edge>> indexedEdges = m_mesh.getIndexedEdgesByVoronoiVertex();

      foreach (VoronoiPolygon vp in m_mesh.VoronoiPolygons)
      {
        Dictionary<Edge, bool> processedEdges = new Dictionary<Edge,bool>();

        PolygonLandProperties vpProps = vp.GetProperties();
        if (vpProps.LandSubtype == PolygonLandProperties.TerrainSubtype.Mountain)
        {
          double elevation = vpProps.Elevation;

          //process all edges coming out of the polygon
          Queue<KeyValuePair<double, Edge>> edgesToProcess = new Queue<KeyValuePair<double, Edge>>();
          foreach (Edge e in vp.Edges)
          {
            edgesToProcess.Enqueue(new KeyValuePair<double, Edge>(vp.GetProperties().Elevation, e));
            processedEdges.Add(e, true);
          }

          //process all edges and continue adding edges
          while (edgesToProcess.Count > 0)
          {
            KeyValuePair<double, Edge> currentPair = edgesToProcess.Dequeue();
            Edge currentEdge = currentPair.Value;
            double currentElevation = currentPair.Key;

            if (currentEdge.VoronoiV1.GetProperties().Elevation < currentElevation)
            {
              currentEdge.VoronoiV1.GetProperties().Elevation = currentElevation;
            }

            if (currentEdge.VoronoiV2.GetProperties().Elevation < currentElevation)
            {
              currentEdge.VoronoiV2.GetProperties().Elevation = currentElevation;
            }

            if (currentElevation - MOUNTAIN_ELEVATION_STEP > 0 && indexedEdges.ContainsKey(currentEdge.VoronoiV1))
            {
              foreach (Edge neighborEdge in indexedEdges[currentEdge.VoronoiV1])
              {
                if (!processedEdges.ContainsKey(neighborEdge))
                {
                  edgesToProcess.Enqueue(new KeyValuePair<double, Edge>(currentElevation - MOUNTAIN_ELEVATION_STEP, neighborEdge));
                  processedEdges.Add(neighborEdge, true);
                }
              }
            }

            if (currentElevation - MOUNTAIN_ELEVATION_STEP > 0 && indexedEdges.ContainsKey(currentEdge.VoronoiV2))
            {
              foreach (Edge neighborEdge in indexedEdges[currentEdge.VoronoiV2])
              {
                if (!processedEdges.ContainsKey(neighborEdge))
                {
                  edgesToProcess.Enqueue(new KeyValuePair<double, Edge>(currentElevation - MOUNTAIN_ELEVATION_STEP, neighborEdge));
                  processedEdges.Add(neighborEdge, true);
                }
              }
            }
          }
        }
      }

      foreach (VoronoiPolygon vp in m_mesh.VoronoiPolygons)
      {
        PolygonLandProperties vpProps = vp.GetProperties();
        vpProps.Elevation = PolygonLandProperties.CalculatePolygonAltitudeFromVertices(vp);
      }
    }
    /**/

    private void markBorderPolygons()
    {
      foreach (VoronoiPolygon vp in m_mesh.VoronoiPolygons)
      {
        foreach (Edge e in vp.Edges)
        {
          if (((EdgeProperties)(e.Attachment)).Border == true)
          {
            ((PolygonLandProperties)(vp.Attachment)).Border = true;
            ((PolygonLandProperties)(vp.Attachment)).LandType = PolygonLandProperties.TerrainType.Ocean;
            BorderPolygons.Add(vp);

            foreach (Vertex v in vp.Vertices)
            {
              v.GetProperties().LandType = VertexLandProperties.TerrainType.Ocean;
            }
          }
        }
      }
    }

    private List<Edge> m_borderEdges = new List<Edge>();
    private void markBorderEdges()
    {
      Dictionary<Vertex, List<DelaunayTriangulator.Edge>> edgeTable = m_mesh.getIndexedEdgesByVoronoiVertex();

      foreach (KeyValuePair<Vertex, List<DelaunayTriangulator.Edge>> itr in edgeTable)
      {
        if (itr.Key.x < 0 || itr.Key.x > Width || itr.Key.y < 0 || itr.Key.y > Height)
        {
          foreach (DelaunayTriangulator.Edge e in itr.Value)
          {
            ((EdgeProperties)(e.Attachment)).Border = true;
            if (!m_borderEdges.Contains(e))
            {
              m_borderEdges.Add(e);
            }
          }
        }
      }
    }

    private void markDownslopes()
    {
      Dictionary<Vertex, List<Edge>> indexedEdges = m_mesh.getIndexedEdgesByVoronoiVertex();

      // Go through every vertex in the mesh, and look at its connected edges.  Find the edge with a paired voronoi vertex that is lower in elevation
      // and mark it as the downslope.
      foreach (Vertex currentVertexBeingProcessed in m_mesh.VoronoiVertices)
      {
        if (indexedEdges.ContainsKey(currentVertexBeingProcessed))
        {
          List<Edge> connectedEdges = indexedEdges[currentVertexBeingProcessed];
          Edge lowestEdge = null;
          double lowestElevation = ((VertexLandProperties)currentVertexBeingProcessed.Attachment).Elevation;

          foreach (Edge edge in connectedEdges)
          {
            if (edge.VoronoiV1 != currentVertexBeingProcessed && ((VertexLandProperties)edge.VoronoiV1.Attachment).Elevation < lowestElevation)
            {
              lowestElevation = ((VertexLandProperties)edge.VoronoiV1.Attachment).Elevation;
              lowestEdge = edge;
            }
            else if (edge.VoronoiV2 != currentVertexBeingProcessed && ((VertexLandProperties)edge.VoronoiV2.Attachment).Elevation < lowestElevation)
            {
              lowestElevation = ((VertexLandProperties)edge.VoronoiV2.Attachment).Elevation;
              lowestEdge = edge;
            }
          }
          ((VertexLandProperties)currentVertexBeingProcessed.Attachment).DownslopeEdge = lowestEdge;
        }
      }
    }

    private void markWaterSheds()
    {
      // Find the lowest corner of the polygon, and set that as the
      // exit point for rain falling on this polygon
      foreach (VoronoiPolygon vp in m_mesh.VoronoiPolygons)
      {
        Vertex lowestVertex = null;

        foreach (Vertex v in vp.Vertices)
        {
          VertexLandProperties vlp = (VertexLandProperties)vp.Attachment;

          if (vlp != null && (lowestVertex == null || vlp.Elevation < ((VertexLandProperties)lowestVertex.Attachment).Elevation))
          {
            lowestVertex = v;
          }
        }

        PolygonLandProperties polygonProperties = (PolygonLandProperties)vp.Attachment;
        polygonProperties.WatershedVertex = lowestVertex;
      }
    }

    //public static TerrainSubtype[][] BiomeTable = new TerrainSubtype[10][]
    //{
    //  /* Moisture                Temperature   0.0 to 0.24               0.25 to 0.49                    0.50 to 0.74                             0.75 to 1.00 */
    //  /* 0.0  to 0.09 */ new TerrainSubtype[] {TerrainSubtype.Scorched,  TerrainSubtype.TemperateDesert, TerrainSubtype.TemperateDesert,          TerrainSubtype.SubtropicalDesert      },
    //  /* 0.10 to 0.19 */ new TerrainSubtype[] {TerrainSubtype.Scorched,  TerrainSubtype.TemperateDesert, TerrainSubtype.TemperateDesert,          TerrainSubtype.SubtropicalDesert      },
    //  /* 0.20 to 0.29 */ new TerrainSubtype[] {TerrainSubtype.Scorched,  TerrainSubtype.TemperateDesert, TerrainSubtype.Grassland,                TerrainSubtype.SubtropicalDesert      },
    //  /* 0.30 to 0.39 */ new TerrainSubtype[] {TerrainSubtype.Bare,      TerrainSubtype.TemperateDesert, TerrainSubtype.Grassland,                TerrainSubtype.TropicalSeasonalForest },
    //  /* 0.40 to 0.49 */ new TerrainSubtype[] {TerrainSubtype.Bare,      TerrainSubtype.Shrubland,       TerrainSubtype.Grassland,                TerrainSubtype.TropicalSeasonalForest },
    //  /* 0.50 to 0.59 */ new TerrainSubtype[] {TerrainSubtype.Tundra,    TerrainSubtype.Shrubland,       TerrainSubtype.Grassland,                TerrainSubtype.TropicalSeasonalForest },
    //  /* 0.60 to 0.69 */ new TerrainSubtype[] {TerrainSubtype.Tundra,    TerrainSubtype.Shrubland,       TerrainSubtype.TemperateDeciduousForest, TerrainSubtype.TropicalRainForest     },
    //  /* 0.70 to 0.79 */ new TerrainSubtype[] {TerrainSubtype.Tundra,    TerrainSubtype.Taiga,           TerrainSubtype.TemperateDeciduousForest, TerrainSubtype.TropicalRainForest     },
    //  /* 0.80 to 0.89 */ new TerrainSubtype[] {TerrainSubtype.Snow,      TerrainSubtype.Taiga,           TerrainSubtype.TemperateRainForest,      TerrainSubtype.TropicalRainForest     },
    //  /* 0.90 to 1.00 */ new TerrainSubtype[] {TerrainSubtype.Snow,      TerrainSubtype.Taiga,           TerrainSubtype.TemperateRainForest,      TerrainSubtype.TropicalRainForest     },
    //};

    private void assignBiomes()
    {
      foreach (VoronoiPolygon vp in m_mesh.VoronoiPolygons)
      {
        if (vp.GetProperties().LandType == VertexLandProperties.TerrainType.Ocean || vp.GetProperties().LandSubtype == PolygonLandProperties.TerrainSubtype.Mountain || vp.GetProperties().LandType == VertexLandProperties.TerrainType.Lake)
        {
          continue;
        }

        int moistureIndex = (int)(vp.GetProperties().Moisture * 100);

        moistureIndex /= 10;

        if (moistureIndex > 9)
        {
          moistureIndex = 9;
        }

        int temperatureIndex = (int)(vp.GetProperties().Temperature * 100);
        temperatureIndex /= 25;
        if (temperatureIndex > 3)
        {
          temperatureIndex = 3;
        }

        vp.GetProperties().LandSubtype = PolygonLandProperties.BiomeTable[moistureIndex][temperatureIndex];
      }
    }

    private void generateLand()
    {
      //Cellular automata map needs to be the same ratio as the target map

      double ratio = m_width / m_height;
      int cellMapHeight = CELLULAR_AUTOMATA_INNER_HEIGHT_DIMENSION;
      int cellMapWidth = CELLULAR_AUTOMATA_INNER_WIDTH_DIMENSION;

      CellularAutomataMap cellMap = new CellularAutomataMap(cellMapWidth, cellMapHeight, NUMBER_OF_CELLULAR_AUTOMATA_ITERATIONS);
      cellMap.generate();

      int refittedMapHeight = CELLULAR_AUTOMATA_OUTER_HEIGHT_DIMENSION;
      int refittedMapWidth = CELLULAR_AUTOMATA_OUTER_WIDTH_DIMENSION;

      int[][] refittedMap = new int[refittedMapWidth][];
      for (int i = 0; i < refittedMapWidth; i++)
      {
        refittedMap[i] = new int[refittedMapHeight];
      }

      for (int w = 0; w < cellMapWidth; ++w)
      {
        for (int h = 0; h < cellMapHeight; ++h)
        {
          int x = w + (int)((refittedMapWidth - cellMapWidth) / 2);
          int y = h + (int)((refittedMapHeight - cellMapHeight) / 2);
          int value = cellMap.Map[w][h];
          refittedMap[x][y] = value;
        }
      }

      cellularAutomata = "";

      #region Build String Representation
      for (int j = 0; j < refittedMapHeight; ++j)
      {
        for (int i = 0; i < refittedMapWidth; ++i)
        {
          if (refittedMap[i][j] == 1)
          {
            cellularAutomata += "#";
          }
          else
          {
            cellularAutomata += ".";
          }
        }
        cellularAutomata += "\r\n";
      }
      #endregion

      double xScalar = m_width / refittedMapWidth;
      double yScalar = m_height / refittedMapHeight;

      foreach (VoronoiPolygon vp in m_mesh.VoronoiPolygons)
      {
        int x = (int)(vp.DelaunayVertex.x / xScalar);
        int y = (int)(vp.DelaunayVertex.y / yScalar);

        if (x > refittedMapWidth - 1)
        {
          x = refittedMapWidth - 1;
        }

        if (y > refittedMapHeight - 1)
        {
          y = refittedMapHeight - 1;
        }


        if (refittedMap[x][y] == 1)
        {
          ((PolygonLandProperties)vp.Attachment).LandType = VertexLandProperties.TerrainType.Land;
          foreach (Vertex v in vp.Vertices)
          {
            ((VertexLandProperties)(v.Attachment)).LandType = VertexLandProperties.TerrainType.Land;
          }
        }
      }
    }

    private static void generateRandomPoints(List<Vertex> points, int numPointsToGenerate, Random random, int maxHeight, int maxWidth)
    {
      for (int i = 0; i < numPointsToGenerate; ++i)
      {
        Vertex v = new Vertex(0, 0);
        v.x = random.NextDouble();
        v.x += random.Next(0, maxWidth - 1);

        v.y = random.NextDouble();
        v.y += random.Next(0, maxHeight - 1);

        points.Add(v);
      }
    }

  }
}
