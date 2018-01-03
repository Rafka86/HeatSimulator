using System;

namespace HeatSimulation {
  internal static class Program {
    [STAThread]
    private static void Main(string[] args) {
      using (var window = new MainWindow()) window.MainLoop();
    }
  }
}