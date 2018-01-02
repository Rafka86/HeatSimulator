using System;
using System.Threading;
using static System.Console;

namespace HeatSimulation {
  class Program {
    [STAThread]
    static void Main(string[] args) {
      using (var form = new MainWindow()) {
        form.Visible = true;
        form.Update();
        for (var i = 0; i < 100; i++) {
          form.Update();
        }
      }
      WriteLine("Press enter key to finish this program.");
      ReadLine();
      //var hs = new HeatSimulator();
      //hs.DoStep();
      //hs.PrintState();
    }
  }
}