using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MapGenerator.MapModel
{
  public class CellularAutomataMap
  {
    public int[][] Map
    {
      get { return m_cellMap; }
    }

    public const double CHANCE_TO_CLEAR_INITIAL_MAP_POINT = 0.50;

    public int NumberOfIterationsToApply { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }
    private int[][] m_cellMap = null;

    public CellularAutomataMap(int width, int height, int iterations)
    {
      Width = width;
      Height = height;
      m_cellMap = new int[width][];
      NumberOfIterationsToApply = iterations;

      for (int x = 0; x < width; x++)
      {
        m_cellMap[x] = new int[height];
      }
    }

    public double GetCoverage()
    {
      double coverage = 0.0;
      double count = 0;
      for (int x = 0; x < Width; ++x)
      {
        for (int y = 0; y < Height; ++y)
        {
          if (m_cellMap[x][y] == 1)
          {
            count++;
          }
        }
      }

      coverage = count / (double)(Width * Height);

      return coverage;
    }

    public void generate()
    {
      randomize();

      for (int i = 0; i < NumberOfIterationsToApply; ++i)
      {
        applyCellularRule();
      }
    }

    private void randomize()
    {
      Random rand = new Random((int)DateTime.Now.Ticks);

      for (int x = 0; x < Width; ++x)
      {
        for (int y = 0; y < Height; ++y)
        {
          if (rand.NextDouble() < CHANCE_TO_CLEAR_INITIAL_MAP_POINT)
          {
            m_cellMap[x][y] = 0;
          }
          else
          {
            m_cellMap[x][y] = 1;
          }
        }
      }
    }

    private void applyCellularRule()
    {
      for (int automataIdx = 0; automataIdx < 4; ++automataIdx)
      {
        for (int x = 0; x < Width; ++x)
        {
          for (int y = 0; y < Height; ++y)
          {
            if (checkCell(x, y))
            {
              if (m_cellMap[x][y] == 0)
              {
                m_cellMap[x][y] = 1;
              }
              else
              {
                m_cellMap[x][y] = 0;
              }
            }
          }
        }
      }
    }

    static int[] g_offsets = { -1, 0, 1 };
    private bool checkCell(int x, int y)
    {
      bool result = false;
      int count = 0;

      foreach (int offsetI in g_offsets)
      {
        foreach (int offsetJ in g_offsets)
        {
          int neighborX = offsetI + x;
          if (neighborX < 0 || neighborX >= Width)
          {
            continue;
          }

          int neighborY = offsetJ + y;
          if (neighborY < 0 || neighborY >= Height)
          {
            continue;
          }

          if (m_cellMap[neighborX][neighborY] != 0)
          {
            count++;
          }
        }
      }

      if (m_cellMap[0][0] != 0)
      {
        count--;
      }

      if (m_cellMap[x][y] == 0)
      {
        if (count > 4)
        {
          result = true;
        }
      }
      else
      {
        if (count <= 4)
        {
          result = true;
        }
      }

      return result;
    }
  }
}
