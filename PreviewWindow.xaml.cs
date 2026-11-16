using System;
using System.IO;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Threading;
namespace LiveCue;
public partial class PreviewWindow : Window {
 public PreviewWindow(FlowDocument doc){InitializeComponent();UpdateDocument(doc);}
 public void UpdateDocument(FlowDocument doc){using var ms=new MemoryStream();new TextRange(doc.ContentStart,doc.ContentEnd).Save(ms,DataFormats.Xaml);ms.Position=0;new TextRange(Script.Document.ContentStart,Script.Document.ContentEnd).Load(ms,DataFormats.Xaml);}
 public void SetFraction(double f)=>Viewer.ScrollToVerticalOffset(Math.Max(0,Math.Min(1,f))*Viewer.ScrollableHeight);
}