using System;
using SharpMKL;
using static SharpMKL.Lapack;

namespace HeatSimulation {
  public class HeatSimulator {
    private const double d = 1.0;
    private const double heat = 4.0;

    private int divNum = 150;
    private double deltaT = 1 / 60.0;

    private int size, lda, bl;
    private double h, c;
    private double[] a;
    private double[] b;
    
    public HeatSimulator() {
      UpdateParameter();
      b = new double[size];
      for (var i = 0; i < b.Length; i++) b[i] = heat;
    }

    private void UpdateParameter() {
      size = divNum * divNum;
      bl = divNum;
      lda = bl + 1;
      h = d / (divNum - 1);
      c = deltaT / (h * h);
      GenerateMatrix();
    }
    
    private void GenerateMatrix() {
      a = new double[size * lda];
      int Index(int i, int j) => j * lda + i - j;
      
      for (var i = 0; i < divNum; i++) {
        for (var j = 0; j < divNum; j++) {
          var k = i * divNum + j;
          a[Index(k, k)] = 1 + 4 * c;
          if (i > 0) {
            var kDown = k - divNum;
            a[Index(k, kDown)] = -c;
          }
          if (j <= 0) continue;
          var kLeft = k - 1;
          a[Index(k, kLeft)] = -c;
        }
      }

      pbtrf(LapackLayout.ColumnMajor, LapackUpLo.Lower, size, bl, a, lda);
    }
    
    public int DivNum {
      get => divNum;
      set {
        divNum = value;
        UpdateParameter();
        b = new double[size];
      }
    }
    public double DeltaT {
      get => deltaT;
      set {
        deltaT = value;
        UpdateParameter();
      }
    }
    public double[] State => b;
    public double MaxValue => heat;

    public void DoStep() => pbtrs(LapackLayout.ColumnMajor, LapackUpLo.Lower, size, bl, 1, a, lda, b, size);

    public void PrintState()  {
      int Index(int i, int j) => i * divNum + j;
      
      for (var i = 0; i < divNum; i++)
        for (var j = 0; j < divNum; j++)
          Console.Write($"{b[Index(i, j)]}{(j == divNum - 1 ? "\n" : " ")}");
    }
  }
}
