using System.Collections.Generic;
using SharpMKL;
using static SharpMKL.Lapack;

namespace HeatSimulation {
  public class HeatSimulator {
    private const double d = 1.0;
    private const double heat = 4.0;
    private static readonly int[] divNumList = {5, 10, 50, 100, 150, 200};

    private int useDivNumI = 3;
    private int divNum;
    public double DeltaT { get; } = 1 / 60.0;

    private int size, lda, bl;
    private double h, c;
    private double[] a;
    private double[] b;
    private readonly List<int> alwaysHeatList;
    
    public HeatSimulator() {
      alwaysHeatList = new List<int>();
      divNum = divNumList[useDivNumI];
      UpdateParameter();
      b = new double[size];
      for (var i = 0; i < b.Length; i++) b[i] = heat;
    }

    private void UpdateParameter() {
      size = divNum * divNum;
      bl = divNum;
      lda = bl + 1;
      h = d / (divNum - 1);
      c = DeltaT / (h * h);
      GenerateMatrix();
      alwaysHeatList.Clear();
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

    public void NextDim() {
      useDivNumI = (useDivNumI + 1) % divNumList.Length;
      DivNum = divNumList[useDivNumI];
    }
    public void PrevDim() {
      useDivNumI = useDivNumI == 0 ? divNumList.Length - 1 : useDivNumI - 1;
      DivNum = divNumList[useDivNumI];
    }
    public int DivNum {
      get => divNum;
      private set {
        divNum = value;
        UpdateParameter();
        b = new double[size];
      }
    }

    public double[] State => b;
    public double MaxValue => heat;

    public void DoStep() {
      foreach (var target in alwaysHeatList) b[target] += heat / h;
      pbtrs(LapackLayout.ColumnMajor, LapackUpLo.Lower, size, bl, 1, a, lda, b, size);
    }
    public void PinnedCell(int target) {
      if (alwaysHeatList.Contains(target)) alwaysHeatList.Remove(target);
      else alwaysHeatList.Add(target);
    }
    public void ClearPinnedList() => alwaysHeatList.Clear();
    public void UnPinnedLastCell() {
      if (alwaysHeatList.Count > 0) alwaysHeatList.RemoveAt(alwaysHeatList.Count - 1);
    }
    public void AllHeat() {
      for (var i = 0; i < b.Length; i++) b[i] += heat / h;
    }
  }
}
