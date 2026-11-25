using System.Windows;
namespace LiveCue;
public partial class PreviewWindow:Window {
 public PreviewWindow(PresenterSurface surface){InitializeComponent();Mirror.Fill=surface.Mirror();}
}