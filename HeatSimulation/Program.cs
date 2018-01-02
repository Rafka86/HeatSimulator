using System;

namespace HeatSimulation {
  class Program {
    [STAThread]
    static void Main(string[] args) {
      var hs = new HeatSimulator();
      hs.DoStep();
      hs.PrintState();
    }
  }
}