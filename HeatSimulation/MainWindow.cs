using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DirectWrite;
using SharpDX.DXGI;
using SharpDX.Windows;

using AlphaMode = SharpDX.Direct2D1.AlphaMode;
using Device = SharpDX.Direct3D11.Device;
using InputDevice = SharpDX.RawInput.Device;
using Factory = SharpDX.DXGI.Factory;
using Factory2D = SharpDX.Direct2D1.Factory;
using FactoryW = SharpDX.DirectWrite.Factory;
using FeatureLevel = SharpDX.Direct3D.FeatureLevel;
using RectangleF = SharpDX.RectangleF;
using Resource = SharpDX.Direct3D11.Resource;
using TextAntialiasMode = SharpDX.Direct2D1.TextAntialiasMode;

namespace HeatSimulation {
  public class MainWindow : RenderForm, IDisposable {
    private readonly Factory factory;
    private readonly Factory2D factory2d;
    private readonly FactoryW factoryWrite;
    private readonly Device device;
    private readonly SwapChain swapChain;
    private readonly Texture2D backBuffer;
    private readonly RenderTargetView renderView;
    private readonly Surface surface;
    private readonly RenderTarget renderTarget2d;

    private readonly TextFormat textFormat;
    private readonly RectangleF textArea;
    private readonly SolidColorBrush sColorBrush;

    private readonly HeatSimulator simulator;
    private readonly GridCell[,] cells;
    private readonly Stopwatch stopWatch;
    private readonly List<GridCell> pinCells;
    private bool printDebug;
    private string debugInfo = "";

    public MainWindow() : base("Heat Simulator") {
      ClientSize = new Size(400, 400);
      MouseClick += MainWindow_MouseClick;
      MouseMove += MainWindow_MouseMove;
      MaximizeBox = false;
      KeyPreview = true;
      KeyPress += MainWindow_KeyPress;
      
      var desc            = new SwapChainDescription {
        BufferCount       = 1,
        ModeDescription   = new ModeDescription(Width, Height, new Rational(60, 1), Format.B8G8R8A8_UNorm),
        IsWindowed        = true,
        OutputHandle      = Handle,
        SampleDescription = new SampleDescription(1, 0),
        SwapEffect        = SwapEffect.Discard,
        Usage             = Usage.RenderTargetOutput
      };
      Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.BgraSupport,
                                 new[] {FeatureLevel.Level_11_1}, desc, out device, out swapChain);
      
      factory = swapChain.GetParent<Factory>();
      factory.MakeWindowAssociation(Handle, WindowAssociationFlags.IgnoreAll);
      backBuffer = Resource.FromSwapChain<Texture2D>(swapChain, 0);
      renderView = new RenderTargetView(device, backBuffer);
      surface = backBuffer.QueryInterface<Surface>();
      
      factory2d = new Factory2D();
      renderTarget2d = new RenderTarget(factory2d, surface,
                                        new RenderTargetProperties(new PixelFormat(Format.Unknown,
                                                                                   AlphaMode.Premultiplied)));
      
      sColorBrush = new SolidColorBrush(renderTarget2d, Color4.White);

      factoryWrite = new FactoryW();
      textFormat           = new TextFormat(factoryWrite, "Ricty", 25) {
        TextAlignment      = TextAlignment.Leading,
        ParagraphAlignment = ParagraphAlignment.Near
      };
      renderTarget2d.TextAntialiasMode = TextAntialiasMode.Cleartype;
      textArea = new RectangleF(0, 0, Width / 2f, Height / 2f);
      
      simulator = new HeatSimulator();
      stopWatch = new Stopwatch();
      cells = new GridCell[simulator.DivNum, simulator.DivNum];
      for (var i = 0; i < simulator.DivNum; i++)
        for (var j = 0; j < simulator.DivNum; j++)
          cells[i, j] = new GridCell(factory2d, Width, Height, i, j, simulator.DivNum, simulator.State);
      pinCells = new List<GridCell>();
    }

    ~MainWindow() => Dispose();
    public new void Dispose() => ReleaseResources();
    private void ReleaseResources() {
      ReleaseD2DObjects();
      ReleaseRenderingObjects();
      Console.WriteLine("Call Release Method.");

      void ReleaseD2DObjects() {
        renderTarget2d?.Dispose();
        surface?.Dispose();
        renderView?.Dispose();
        backBuffer?.Dispose();
        swapChain?.Dispose();
        device?.Dispose();
        factoryWrite?.Dispose();
        factory2d?.Dispose();
        factory?.Dispose();
      }
      void ReleaseRenderingObjects() {
        foreach(var cell in cells) cell.Dispose();
        textFormat?.Dispose();
        sColorBrush?.Dispose();
      }
    }

    public void MainLoop() => RenderLoop.Run(this, Draw);

    private void Draw() {
      stopWatch.Restart();
      foreach (var cell in pinCells) cell.Heating(simulator.MaxValue / simulator.DeltaT);
      simulator.DoStep();
      
      renderTarget2d.BeginDraw();
      renderTarget2d.Clear(Color4.Black);
      foreach (var cell in cells) {
        sColorBrush.Color = cell.Color(simulator.MaxValue);
        renderTarget2d.FillGeometry(cell.Geometry, sColorBrush);
      }
      sColorBrush.Color = Color4.Black;
      renderTarget2d.DrawText(debugInfo, textFormat, textArea, sColorBrush);
      renderTarget2d.EndDraw();
      swapChain.Present(0, PresentFlags.None);

      if (stopWatch.ElapsedMilliseconds * 1e-3 < simulator.DeltaT)
        Thread.Sleep((int) ((simulator.DeltaT - stopWatch.ElapsedMilliseconds * 1e-3) * 1000));
      debugInfo = printDebug ? $"FPS:{1.0 / (stopWatch.ElapsedMilliseconds * 1e-3):0.0}\n" : "";
    }

    private void MainWindow_MouseMove(object sender, MouseEventArgs e) {
      var divSize = Width / (float)simulator.DivNum;
      var x = (int) (e.X / divSize);
      var y = (int) (simulator.DivNum - e.Y / divSize - 1);
      cells[y, x].Heating(simulator.MaxValue * 100);
    }

    private void MainWindow_MouseClick(object sender, MouseEventArgs e) {
      var divSize = Width / (float)simulator.DivNum;
      var x = (int) (e.X / divSize);
      var y = (int) (simulator.DivNum - e.Y / divSize - 1);
      if (pinCells.Contains(cells[y, x])) pinCells.Remove(cells[y, x]);
      else pinCells.Add(cells[y, x]);
    }

    private void MainWindow_KeyPress(object sender, KeyPressEventArgs e) {
      switch (e.KeyChar) {
        case 'c': pinCells.Clear(); break;
        case 'z': if (pinCells.Count > 0) pinCells.RemoveAt(pinCells.Count - 1); break;
        case 'p': printDebug = !printDebug; break;
      }
    }
  }
}