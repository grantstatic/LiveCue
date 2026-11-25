using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
namespace LiveCue;
public sealed class OutputAppearance {
 public static readonly string[] FontChoices={"Arial","Arial Narrow","Verdana","Tahoma","Calibri","Segoe UI","Franklin Gothic Medium","Trebuchet MS","Lucida Sans Unicode","Courier New"};
 public string Font {get;set;}="Arial";
 public double Points {get;set;}=36;
 public string ColorHex {get;set;}="#FFFFFF";
 public string Weight {get;set;}="As scripted";
 public bool PreserveColors {get;set;}=true;
 public OutputAppearance Copy()=>new(){Font=Font,Points=Points,ColorHex=ColorHex,Weight=Weight,PreserveColors=PreserveColors};
 public static bool Installed(string name)=>Fonts.SystemFontFamilies.Any(x=>string.Equals(x.Source,name,StringComparison.OrdinalIgnoreCase));
 public string EffectiveFont=>Installed(Font)?Font:"Arial";
 public Color TextColor {get {try{return (Color)ColorConverter.ConvertFromString(ColorHex);}catch{return Colors.White;}}}
 public double SafePoints=>double.IsFinite(Points)?Math.Clamp(Points,12,160):36;
 public static FlowDocument Clone(FlowDocument source){
  var result=new FlowDocument();using var ms=new MemoryStream();new TextRange(source.ContentStart,source.ContentEnd).Save(ms,DataFormats.Xaml);ms.Position=0;new TextRange(result.ContentStart,result.ContentEnd).Load(ms,DataFormats.Xaml);return result;
 }
 public FlowDocument Render(FlowDocument source,double scale=1){
  var doc=Clone(source);var elements=new List<TextElement>();var colors=new Dictionary<Run,Brush>();
  for(TextPointer? p=doc.ContentStart;p!=null&&p.CompareTo(doc.ContentEnd)<0;p=p.GetNextContextPosition(LogicalDirection.Forward)){
   if(p.GetPointerContext(LogicalDirection.Forward)==TextPointerContext.ElementStart&&p.GetAdjacentElement(LogicalDirection.Forward) is TextElement el){
    elements.Add(el);
    if(el is Run run&&run.Foreground is SolidColorBrush brush&&brush.Color!=Colors.White&&brush.Color!=Colors.Black&&brush.Color.A!=0)colors[run]=brush;
   }
  }
  var family=new FontFamily(EffectiveFont);var size=SafePoints*96.0/72.0*scale;var foreground=new SolidColorBrush(TextColor);
  doc.FontFamily=family;doc.FontSize=size;doc.Foreground=foreground;
  if(Weight=="Bold")doc.FontWeight=FontWeights.Bold;else if(Weight=="Regular")doc.FontWeight=FontWeights.Normal;
  foreach(var el in elements){
   el.FontFamily=family;el.FontSize=size;el.Foreground=foreground;
   if(Weight=="Bold")el.FontWeight=FontWeights.Bold;else if(Weight=="Regular")el.FontWeight=FontWeights.Normal;
   if(PreserveColors&&el is Run run&&colors.TryGetValue(run,out var original))run.Foreground=original;
  }
  return doc;
 }
}