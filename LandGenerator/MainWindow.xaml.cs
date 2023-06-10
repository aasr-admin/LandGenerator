using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Drawing;
using System.ComponentModel;
using System.Diagnostics;
using MapGenerator.MapModel;

namespace LandGenerator
{
  public class VisualLayer
  {
    public VisualLayer(string name, bool visible, BitmapSource source)
    {
      m_Name = name;
      m_visible = visible;
      m_source = source;
    }

    private string m_Name;
    public string Name
    {
      get { return m_Name; }
      set { m_Name = value; }
    }

    private bool m_visible;
    public bool Visible
    {
      get { return m_visible; }
      set { m_visible = value; }
    }

    private BitmapSource m_source;
    public BitmapSource Source
    {
      get { return m_source; }
      set { m_source = value; }
    }
  }


  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window, INotifyPropertyChanged
  {
    private string m_logOutput = "";
    public string LogOutput
    {
      get { return m_logOutput; }
      set { m_logOutput = value; }
    }

    void LogStatus(string message)
    {
      m_logOutput += string.Format("{0}\r\n", message);
      OnPropertyChanged("LogOutput");
    }
    

    private void Window_KeyUp(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Escape) zoomViewer.Reset();
    }

    public event PropertyChangedEventHandler PropertyChanged;
    // Create the OnPropertyChanged method to raise the event 
    protected void OnPropertyChanged(string name)
    {
      PropertyChangedEventHandler handler = PropertyChanged;
      if (handler != null)
      {
        handler(this, new PropertyChangedEventArgs(name));
      }
    }

    #region Layers

    private BindingList<VisualLayer> m_layerSources = new BindingList<VisualLayer>();
    public BindingList<VisualLayer> LayerSources { get { return m_layerSources; } }
    
    private string m_cellular = "";
    public string cellular { get { return m_cellular; } set { m_cellular = value; } }

    #endregion

    //private int m_mapWidth = 7168;
    //private int m_mapHeight = 4096;
    private int m_mapWidth = 1000;
    private int m_mapHeight = 1000;
    public int MapWidth { get { return m_mapWidth; } set { m_mapWidth = value; } }
    public int MapHeight { get { return m_mapHeight; } set { m_mapHeight = value; } }


    public MainWindow()
    {
      InitializeComponent();
    }

    public BitmapSource source { set; get; }

    private Map m_map = null;

    //private int m_numPoints = 4000;
    private int m_numPoints = 1000;
    public int NumberOfPoints
    {
      get
      {
        return m_numPoints; 
      }

      set
      {
        m_numPoints = value;
        OnPropertyChanged("NumberOfPoints");
      }
    }

    private void GenerateMap(object sender, RoutedEventArgs e)
    {
      m_map = null;
      m_logOutput = "";

      Stopwatch totalTime = new Stopwatch();
      totalTime.Start();

      m_layerSources.Clear();

      m_map = new Map(m_mapWidth, m_mapHeight, 1234, m_numPoints);
      m_map.LogStatusMessage += LogStatus;

      m_map.generate();
      Stopwatch watch = new Stopwatch();

      LogStatus("Generating Noise Polygon Edges");
      watch.Start();
      EdgeProperties.CalculateNoisyEdges(m_map.Mesh, 5);
      watch.Stop();
      LogStatus(string.Format("{0} milliseconds elapsed.", watch.ElapsedMilliseconds));
      watch.Reset();

      LogStatus("Generating Ocean Bitmap Layer");

      watch.Start();
      Bitmap oceanBmp = new Bitmap(m_mapWidth, m_mapHeight);
      using (var graphics = Graphics.FromImage(oceanBmp))
      {
        graphics.FillRectangle(new SolidBrush(System.Drawing.Color.FromArgb(0x00, 0x55, 0xA0)), 0, 0, oceanBmp.Width, oceanBmp.Height);
      }

      m_layerSources.Add(new VisualLayer("Ocean", false, MapImager.GetBitmapImage(oceanBmp)));
      watch.Stop();
      LogStatus(string.Format("{0} milliseconds elapsed.", watch.ElapsedMilliseconds));
      watch.Reset();

      LogStatus("Generating Land Bitmap Layer");
      watch.Start();
      m_layerSources.Add(new VisualLayer("Land", true, MapImager.GetBitmapImage(MapImager.GetLandLayer(m_map))));
      watch.Stop();
      LogStatus(string.Format("{0} milliseconds elapsed.", watch.ElapsedMilliseconds));
      watch.Reset();

      LogStatus("Generating Noisy Land Elevation Bitmap Layer");
      watch.Start();
      m_layerSources.Add(new VisualLayer("Noisy Elevation Land", false, MapImager.GetBitmapImage(MapImager.GetNoisyElevationLandLayer(m_map))));
      watch.Stop();
      LogStatus(string.Format("{0} milliseconds elapsed.", watch.ElapsedMilliseconds));
      watch.Reset();

      LogStatus("Generating Mountain Bitmap Layer");
      watch.Start();
      m_layerSources.Add(new VisualLayer("Mountains", false, MapImager.GetBitmapImage(MapImager.GetMountainLayer(m_map))));
      watch.Stop();
      LogStatus(string.Format("{0} milliseconds elapsed.", watch.ElapsedMilliseconds));
      watch.Reset();

      LogStatus("Generating Border Faces Bitmap Layer");


      //watch.Start();
      //
      //m_layerSources.Add(new VisualLayer("Border Faces", false, MapImager.GetBitmapImage(MapImager.GetBorderFaces(m_map))));
      //watch.Stop();
      //LogStatus(string.Format("{0} milliseconds elapsed.", watch.ElapsedMilliseconds));
      //watch.Reset();


      LogStatus("Generating Voronoi Bitmap Layer");
      watch.Start();
      m_layerSources.Add(new VisualLayer("Voronoi", true, MapImager.GetBitmapImage(MapImager.GetVoronoiLayer(m_map))));
      watch.Stop();
      LogStatus(string.Format("{0} milliseconds elapsed.", watch.ElapsedMilliseconds));
      watch.Reset();


      LogStatus("Generating Delaunay Vertex Centers Bitmap Layer");
      watch.Start();
      m_layerSources.Add(new VisualLayer("Vertex Centers", false, MapImager.GetBitmapImage(MapImager.GetDelaunayVerticesLayer(m_map))));
      watch.Stop();
      LogStatus(string.Format("{0} milliseconds elapsed.", watch.ElapsedMilliseconds));
      watch.Reset();

      LogStatus("Generating Delaunay Triangle Bitmap Layer");
      watch.Start();
      m_layerSources.Add(new VisualLayer("Delaunay", false, MapImager.GetBitmapImage(MapImager.GetDelaunayLayer(m_map))));
      watch.Stop();
      LogStatus(string.Format("{0} milliseconds elapsed.", watch.ElapsedMilliseconds));
      watch.Reset();

      LogStatus("Generating Rivers Bitmap Layer");
      watch.Start();
      m_layerSources.Add(new VisualLayer("Rivers", false, MapImager.GetBitmapImage(MapImager.GetRiverLayer(m_map))));
      watch.Stop();
      LogStatus(string.Format("{0} milliseconds elapsed.", watch.ElapsedMilliseconds));
      watch.Reset();

      LogStatus("Generating Lakes Bitmap Layer");
      watch.Start();
      m_layerSources.Add(new VisualLayer("Lakes", false, MapImager.GetBitmapImage(MapImager.GetLakes(m_map))));
      watch.Stop();
      LogStatus(string.Format("{0} milliseconds elapsed.", watch.ElapsedMilliseconds));
      watch.Reset();

      LogStatus("Generating Temperature Bitmap Layer");
      watch.Start();
      m_layerSources.Add(new VisualLayer("Temperature Overlay", false, MapImager.GetBitmapImage(MapImager.GetTemperatureLayer(m_map))));
      watch.Stop();
      LogStatus(string.Format("{0} milliseconds elapsed.", watch.ElapsedMilliseconds));
      watch.Reset();

      LogStatus("Generating Latitude Divider Bitmap Layer");
      watch.Start();
      m_layerSources.Add(new VisualLayer("Latitude", false, MapImager.GetBitmapImage(MapImager.GetLatitudeDividers(m_map))));
      watch.Stop();
      LogStatus(string.Format("{0} milliseconds elapsed.", watch.ElapsedMilliseconds));
      watch.Reset();

      LogStatus("Generating Vertex Elevation Bitmap Layer");
      watch.Start();
      m_layerSources.Add(new VisualLayer("Vertex Elevation", false, MapImager.GetBitmapImage(MapImager.GetElevationLayer(m_map))));
      watch.Stop();
      LogStatus(string.Format("{0} milliseconds elapsed.", watch.ElapsedMilliseconds));
      watch.Reset();

      LogStatus("Generating Polygon Elevation Bitmap Layer");
      watch.Start();
      m_layerSources.Add(new VisualLayer("Polygon Elevation", false, MapImager.GetBitmapImage(MapImager.GetPolygonElevationLayer(m_map))));
      watch.Stop();
      LogStatus(string.Format("{0} milliseconds elapsed.", watch.ElapsedMilliseconds));
      watch.Reset();

      LogStatus("Generating Lake Vertices Layer");
      watch.Start();
      m_layerSources.Add(new VisualLayer("Lake Vertices", false, MapImager.GetBitmapImage(MapImager.GetLakeVerticesLayer(m_map))));
      watch.Stop();
      LogStatus(string.Format("{0} milliseconds elapsed.", watch.ElapsedMilliseconds));
      watch.Reset();

      LogStatus("Generating Polygon Temperature Bitmap Layer");
      watch.Start();
      m_layerSources.Add(new VisualLayer("Polygon Temperature", false, MapImager.GetBitmapImage(MapImager.GetTemperatureValueOverlayLayer(m_map))));
      watch.Stop();
      LogStatus(string.Format("{0} milliseconds elapsed.", watch.ElapsedMilliseconds));
      watch.Reset();

      LogStatus("Generating Vertex Moisture Bitmap Layer");
      watch.Start();
      m_layerSources.Add(new VisualLayer("Vertex Moisture", false, MapImager.GetBitmapImage(MapImager.GetVertexMoistureLayer(m_map))));
      watch.Stop();
      LogStatus(string.Format("{0} milliseconds elapsed.", watch.ElapsedMilliseconds));
      watch.Reset();

      LogStatus("Generating Polygon Moisture Bitmap Layer");
      watch.Start();
      m_layerSources.Add(new VisualLayer("Polygon Moisture", false, MapImager.GetBitmapImage(MapImager.GetPolygonMoistureLayer(m_map))));
      watch.Stop();
      LogStatus(string.Format("{0} milliseconds elapsed.", watch.ElapsedMilliseconds));
      watch.Reset();

      LogStatus("Generating Biome Labels Layer");
      watch.Start();
      m_layerSources.Add(new VisualLayer("Biome Labels", false, MapImager.GetBitmapImage(MapImager.GetBiomeOverlayLayer(m_map))));
      watch.Stop();
      LogStatus(string.Format("{0} milliseconds elapsed.", watch.ElapsedMilliseconds));
      watch.Reset();
      

      LogStatus("Committing Layers to Visual Plane");
      watch.Start();
      OnPropertyChanged("LayerSources");
      watch.Stop();
      LogStatus(string.Format("{0} milliseconds elapsed.", watch.ElapsedMilliseconds));
      watch.Reset();

      m_cellular = m_map.cellularAutomata;
      OnPropertyChanged("cellular");
      /**/

      totalTime.Stop();
      LogStatus(string.Format("{0} milliseconds elapsed.", totalTime.ElapsedMilliseconds));
    }

    private void ExportMapHandler(object sender, RoutedEventArgs e)
    {
      Microsoft.Win32.SaveFileDialog saveDialogue = new Microsoft.Win32.SaveFileDialog();
      saveDialogue.Filter = "Ultima Online Map File (*.mul) | *.mul";
      saveDialogue.FileName = "map32";
      saveDialogue.Title = "Save As";
      if (saveDialogue.ShowDialog() == true)
      {
        MapGenerator.View.MapMulView.ConvertMap(m_map, saveDialogue.FileName);
      }
    }
  }
}
