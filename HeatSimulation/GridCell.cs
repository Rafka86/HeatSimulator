using System;
using SharpDX;
using SharpDX.Direct2D1;
using static System.Math;
using Factory2D = SharpDX.Direct2D1.Factory;

namespace HeatSimulation {
  public struct GridCell : IDisposable {
    public RectangleGeometry Geometry { get; }

    private readonly double[] status;
    private readonly int target;

    public GridCell(Factory2D factory, int windowWidth, int windowHeight, int i, int j, int divNum, double[] status) {
      this.status = status;
      target = i * divNum + j;

      var sizeX = (float) windowWidth / divNum;
      var sizeY = (float) windowHeight / divNum;
      Geometry = new RectangleGeometry(factory,
                                       new RectangleF(j * sizeX, (divNum - 1 - i) * sizeY, sizeX + 0.75f,
                                                      sizeY + 0.75f));
    }

    public void Dispose() => Geometry?.Dispose();

    public Color4 Color(double maxValue) {
      var rgb = GenColor(status[target]);
      return new Color4((float) rgb.r, (float) rgb.g, (float) rgb.b, 1.0f);

      (double r, double g, double b) GenColor(double value) {
        var divVal = maxValue / 4.0;
        var blendVal = -Cos(PI * value / divVal) / 2.0 + 0.5;
        double r, g, b;
             if (value < divVal * 0) {r = 0.0;      g = 0.0;      b = 1.0;     }
        else if (value < divVal * 1) {r = 0.0;      g = blendVal; b = 1.0;     }
        else if (value < divVal * 2) {r = 0.0;      g = 1.0;      b = blendVal;}
        else if (value < divVal * 3) {r = blendVal; g = 1.0;      b = 0.0;     }
        else if (value < divVal * 4) {r = 1.0;      g = blendVal; b = 0.0;     }
        else                         {r = 1.0;      g = 0.0;      b = 0.0;     }
        return (r, g, b);
      }
    }

    public void Heating(double heat) => status[target] += heat;
  }
}