using System;
using System.IO;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Threading;
using Forms = System.Windows.Forms;
namespace LiveCue;
public partial class OutputWindow : Window {
 public bool Manual {get;private set;} public bool Playing {get;private set;} public double Speed {get;private set;}=55;
 public double Offset=>Viewer.VerticalOffset; public double Range=>Viewer.ScrollableHeight;
 int direction=1; double targetOffset; DateTime last=DateTime.UtcNow; public bool IsFullscreen => WindowStyle==WindowStyle.None; public event Action? TransportChanged;
 readonly DispatcherTimer timer=new(){Interval=TimeSpan.FromMilliseconds(16)};
 public OutputWindow(FlowDocument document){InitializeComponent();UpdateDocument(document);timer.Tick+=(_,_)=>{var now=DateTime.UtcNow;var elapsed=Math.Min((now-last).TotalSeconds,0.1);last=now;if(Playing&&!Manual){targetOffset=Math.Clamp(targetOffset+Speed*direction*elapsed,0,Range);Viewer.ScrollToVerticalOffset(targetOffset);if((direction>0&&targetOffset>=Range)||(direction<0&&targetOffset<=0)){Pause();}}};timer.Start();Closed+=(_,_)=>timer.Stop();}
 public void UpdateDocument(FlowDocument document){double fraction=Range>0?Offset/Range:0;OutputAppearance.SizeMarkers(CueLeft,CueRight,Script,document.FontSize);var range=new TextRange(document.ContentStart,document.ContentEnd);using var ms=new MemoryStream();range.Save(ms,DataFormats.Xaml);ms.Position=0;new TextRange(Script.Document.ContentStart,Script.Document.ContentEnd).Load(ms,DataFormats.Xaml);Dispatcher.BeginInvoke(new Action(()=>{targetOffset=fraction*Range;Viewer.ScrollToVerticalOffset(targetOffset);}),DispatcherPriority.Loaded);}
 public void TogglePlay(){if(Manual)return;Playing=!Playing;targetOffset=Offset;last=DateTime.UtcNow;TransportChanged?.Invoke();}
 public void Pause(){Playing=false;TransportChanged?.Invoke();}
 public void SetMode(bool manual){Manual=manual;Playing=false;}
 public void SetSpeed(double speed)=>Speed=speed;
 public void Reverse()=>direction=-direction;
 public void Jog(double steps){targetOffset=Math.Clamp(targetOffset+steps*32,0,Range);Viewer.ScrollToVerticalOffset(targetOffset);}
 public void MoveToScreen(Forms.Screen screen){WindowState=WindowState.Normal;WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;Left=screen.Bounds.Left;Top=screen.Bounds.Top;Width=screen.Bounds.Width;Height=screen.Bounds.Height;WindowState=WindowState.Maximized;}
 public void ExitFullscreen(){Pause();if(IsFullscreen)Floating();}
 public void Floating(){WindowState=WindowState.Normal;WindowStyle=WindowStyle.SingleBorderWindow;ResizeMode=ResizeMode.CanResize;Width=900;Height=600;}
}