using System;
using SharpDX;
using SharpDX.Direct2D1;

using Factory2D = SharpDX.Direct2D1.Factory;

namespace HeatSimulation {
  public struct GridCell : IDisposable {
    public RectangleGeometry Geometry { get; }

    private readonly double[] status;
    private readonly int target;

    public GridCell(Factory2D factory, int windowSize, int i, int j, int divNum, double[] status) {
      this.status = status;
      target = i * divNum + j;

      var size = (float) windowSize / divNum;
      Geometry = new RectangleGeometry(factory,
                                       new RectangleF(j * size, (divNum - 1 - i) * size, (j + 1) * size,
                                                      (divNum - i) * size));
    }

    public void Dispose() => Geometry?.Dispose();

    public Color4 Color => new Color4(1.0f * (float) status[target], 0.0f, 1.0f * (float) (1.0 - status[target]), 1.0f);
  }
}