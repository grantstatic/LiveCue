using System.Windows;
using Forms=System.Windows.Forms;
namespace LiveCue;
public partial class OutputWindow:Window {
 public PresenterSurface Surface {get;}
 public OutputWindow(PresenterSurface surface){InitializeComponent();Surface=surface;Host.Children.Add(surface);}
 public void ReleaseSurface()=>Host.Children.Remove(Surface);
 public bool IsFullscreen=>WindowStyle==WindowStyle.None;
 public void MoveToScreen(Forms.Screen screen){WindowState=WindowState.Normal;WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;Left=screen.Bounds.Left;Top=screen.Bounds.Top;Width=screen.Bounds.Width;Height=screen.Bounds.Height;WindowState=WindowState.Maximized;}
 public void ExitFullscreen(){Surface.Pause();if(IsFullscreen)Floating();}
 public void Floating(){WindowState=WindowState.Normal;WindowStyle=WindowStyle.SingleBorderWindow;ResizeMode=ResizeMode.CanResize;Width=900;Height=600;}
}