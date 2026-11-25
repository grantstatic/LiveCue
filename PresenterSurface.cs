using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;
namespace LiveCue;

// One visual tree: the operator and floating previews use a VisualBrush of this surface.
public sealed class PresenterSurface : Grid {
 readonly Canvas viewport=new(){ClipToBounds=true};
 readonly RichTextBox script=new(){IsReadOnly=true,Focusable=false,IsHitTestVisible=false,BorderThickness=new Thickness(0),Background=Brushes.Transparent,Foreground=Brushes.White,VerticalScrollBarVisibility=ScrollBarVisibility.Disabled,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,Padding=new Thickness(0),UseLayoutRounding=false,SnapsToDevicePixels=false};
 readonly TranslateTransform movement=new();
 readonly Polygon left=new(){Fill=Brushes.White,Stretch=Stretch.Fill,HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(6,0,0,0),IsHitTestVisible=false,Points=new PointCollection{new Point(0,0),new Point(24,20),new Point(0,40)}};
 readonly Polygon right=new(){Fill=Brushes.White,Stretch=Stretch.Fill,HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(0,0,6,0),IsHitTestVisible=false,Points=new PointCollection{new Point(24,0),new Point(0,20),new Point(24,40)}};
 double offset,extent,em=48,lastWidth,lastHeight; int direction=1; bool reflowing;
 TimeSpan? lastFrame; bool attached;
 public bool Manual {get;private set;}
 public bool Playing {get;private set;}
 public double Speed {get;private set;}=55;
 public event Action? TransportChanged;
 public PresenterSurface(){
  Background=Brushes.Black;ClipToBounds=true;UseLayoutRounding=false;SnapsToDevicePixels=false;
  TextOptions.SetTextFormattingMode(script,TextFormattingMode.Ideal);
  script.RenderTransform=movement;viewport.Children.Add(script);Children.Add(viewport);Children.Add(left);Children.Add(right);
  SizeChanged+=(_,_)=>Reflow(null);
  Loaded+=(_,_)=>{if(!attached){CompositionTarget.Rendering+=OnFrame;attached=true;}lastFrame=null;Reflow(null);};
  Unloaded+=(_,_)=>{if(attached){CompositionTarget.Rendering-=OnFrame;attached=false;}lastFrame=null;};
 }
 public VisualBrush Mirror()=>new(this){Stretch=Stretch.Uniform,AlignmentX=AlignmentX.Center,AlignmentY=AlignmentY.Center};
 record Anchor(int Symbol,double Relative,double Fraction);
 Anchor GetAnchor(){
  double fraction=extent>0?offset/extent:0;
  if(offset<=0)return new(-1,0,0);
  try{
   var p=script.GetPositionFromPoint(new Point(script.Padding.Left+1,offset+lastHeight/2),true);
   if(p!=null){var r=p.GetCharacterRect(LogicalDirection.Forward);if(!r.IsEmpty&&double.IsFinite(r.Top))return new(script.Document.ContentStart.GetOffsetToPosition(p),r.Top-offset-lastHeight/2,fraction);}
  }catch{}
  return new(-1,0,fraction);
 }
 public void UpdateDocument(FlowDocument doc)=>Reflow(doc);
 void Reflow(FlowDocument? replacement){
  if(reflowing)return;
  var a=GetAnchor();
  reflowing=true;
  try{
   if(replacement!=null){em=replacement.FontSize;script.Document=replacement;script.FontSize=em;script.FontFamily=replacement.FontFamily;script.Foreground=replacement.Foreground;}
   double w=ActualWidth,h=ActualHeight;
   if(w<1||h<1)return;
   lastWidth=w;lastHeight=h;
   // Preserve margins for unusually narrow windows and very large fonts.
   double marker=Math.Min(em*.60,Math.Max(1,w*.18));left.Width=right.Width=marker;left.Height=right.Height=marker*(.85/.60);
   double gutter=Math.Min(marker+14,w*.23);
   script.Padding=new Thickness(gutter,Math.Max(0,h/2-em*.60),gutter,h/2);
   script.Width=w;script.Height=double.NaN;script.Document.PageWidth=double.NaN;script.Document.ColumnWidth=double.PositiveInfinity;script.Document.PagePadding=new Thickness(0);
   script.Measure(new Size(w,double.PositiveInfinity));
   double textHeight=script.DesiredSize.Height;
   script.Arrange(new Rect(0,0,w,textHeight));script.UpdateLayout();
   extent=Math.Max(0,textHeight-h);
   double next=a.Fraction*extent;
   if(a.Symbol>=0){try{var p=script.Document.ContentStart.GetPositionAtOffset(Math.Min(a.Symbol,script.Document.ContentStart.GetOffsetToPosition(script.Document.ContentEnd)),LogicalDirection.Forward);if(p!=null){var r=p.GetCharacterRect(LogicalDirection.Forward);if(!r.IsEmpty&&double.IsFinite(r.Top))next=r.Top-h/2-a.Relative;}}catch{}}
   SetOffset(next);lastFrame=null;
  }finally{reflowing=false;}
 }
 void SetOffset(double value){offset=Math.Clamp(double.IsFinite(value)?value:0,0,extent);movement.Y=-offset;}
 void OnFrame(object? sender,EventArgs e){
  if(e is not RenderingEventArgs frame)return;
  double dt=lastFrame.HasValue?(frame.RenderingTime-lastFrame.Value).TotalSeconds:0;lastFrame=frame.RenderingTime;
  if(!Playing||Manual||reflowing||dt<=0)return;
  // Only the render transform moves; no per-frame text layout or ScrollViewer rounding.
  SetOffset(offset+Speed*direction*Math.Min(dt,.10));
  if((direction>0&&offset>=extent)||(direction<0&&offset<=0))Pause();
 }
 public void TogglePlay(){if(Manual)return;Playing=!Playing;lastFrame=null;TransportChanged?.Invoke();}
 public void Pause(){Playing=false;TransportChanged?.Invoke();}
 public void SetMode(bool manual){Manual=manual;Pause();}
 public void SetSpeed(double speed)=>Speed=Math.Clamp(speed,0,300);
 public void Reverse()=>direction=-direction;
 public void Jog(double steps)=>SetOffset(offset+steps*32);
}
