using System;
using System.Diagnostics;
using System.Drawing;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using SharpDX.Windows;

using AlphaMode = SharpDX.Direct2D1.AlphaMode;
using Device = SharpDX.Direct3D11.Device;
using Factory = SharpDX.DXGI.Factory;
using Factory2D = SharpDX.Direct2D1.Factory;
using FeatureLevel = SharpDX.Direct3D.FeatureLevel;
using Resource = SharpDX.Direct3D11.Resource;

namespace HeatSimulation {
  public class MainWindow : RenderForm, IDisposable {
    private readonly Factory factory;
    private readonly Factory2D factory2d;
    private readonly Device device;
    private readonly SwapChain swapChain;
    private readonly Texture2D backBuffer;
    private readonly RenderTargetView renderView;
    private readonly Surface surface;
    private readonly RenderTarget renderTarget2d;

    private readonly SolidColorBrush sColorBrush;

    private readonly HeatSimulator simulator;
    private readonly GridCell[,] cells;
    
    public MainWindow() : base("Heat Simulator") {
      ClientSize = new Size(400, 400);
      
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
      
      factory2d = new Factory2D();
      factory = swapChain.GetParent<Factory>();
      factory.MakeWindowAssociation(Handle, WindowAssociationFlags.IgnoreAll);
      backBuffer = Resource.FromSwapChain<Texture2D>(swapChain, 0);
      renderView = new RenderTargetView(device, backBuffer);
      surface = backBuffer.QueryInterface<Surface>();
      renderTarget2d = new RenderTarget(factory2d, surface,
                                        new RenderTargetProperties(new PixelFormat(Format.Unknown,
                                                                                   AlphaMode.Premultiplied)));
      
      sColorBrush = new SolidColorBrush(renderTarget2d, Color4.White);
      
      simulator = new HeatSimulator();
      cells = new GridCell[simulator.DivNum, simulator.DivNum];
      for (var i = 0; i < simulator.DivNum; i++)
        for (var j = 0; j < simulator.DivNum; j++)
          cells[i, j] = new GridCell(factory2d, Width, i, j, simulator.DivNum, simulator.State);
    }

    ~MainWindow() => Dispose();
    public new void Dispose() => ReleaseResources();
    private void ReleaseResources() {
      ReleaseD2DObjects();
      ReleaseRenderingObjects();

      void ReleaseD2DObjects() {
        renderTarget2d.Dispose();
        surface.Dispose();
        renderView.Dispose();
        backBuffer.Dispose();
        swapChain.Dispose();
        device.Dispose();
        factory2d.Dispose();
        factory.Dispose();
      }
      void ReleaseRenderingObjects() {
        foreach(var cell in cells) cell.Dispose();
        sColorBrush.Dispose();
      }
    }

    public void Loop() {
      var sw = new Stopwatch();
      sw.Start();
      RenderLoop.Run(this, RenderCommandList);
      
      void RenderCommandList() {
        renderTarget2d.BeginDraw();
        renderTarget2d.Clear(Color4.Black);
        renderTarget2d.EndDraw();
        swapChain.Present(0, PresentFlags.None);
      }
    }

    public new void Update() {
      simulator.DoStep();
      
      renderTarget2d.BeginDraw();
      renderTarget2d.Clear(Color4.Black);
      foreach (var cell in cells) {
        sColorBrush.Color = cell.Color;
        renderTarget2d.FillGeometry(cell.Geometry, sColorBrush);
      }
      renderTarget2d.EndDraw();
      swapChain.Present(0, PresentFlags.None);
    }
  }
}