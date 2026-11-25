using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Forms=System.Windows.Forms;
namespace LiveCue;
public partial class AppearanceWindow:Window {
 bool ready,updating; public OutputAppearance Result {get;private set;}
 public AppearanceWindow(OutputAppearance value){Result=value.Copy();InitializeComponent();FontChoice.ItemsSource=OutputAppearance.FontChoices;ready=true;LoadValues(Result);}
 void LoadValues(OutputAppearance a){updating=true;FontChoice.SelectedItem=a.Font;if(FontChoice.SelectedIndex<0)FontChoice.SelectedIndex=0;SizeSlider.Value=a.SafePoints;SizeField.Text=a.SafePoints.ToString("0",CultureInfo.InvariantCulture);WeightChoice.SelectedIndex=a.Weight=="Bold"?2:a.Weight=="Regular"?1:0;ColorField.Text=a.ColorHex;Preserve.IsChecked=a.PreserveColors;updating=false;RefreshSample();}
 void Changed(object s,RoutedEventArgs e)=>RefreshSample();
 void ColorChanged(object s,TextChangedEventArgs e)=>RefreshSample();
 void SizeChanged(object s,RoutedPropertyChangedEventArgs<double> e){if(!ready||updating)return;updating=true;SizeField.Text=e.NewValue.ToString("0",CultureInfo.InvariantCulture);updating=false;RefreshSample();}
 void SizeTyped(object s,TextChangedEventArgs e){if(!ready||updating)return;if(double.TryParse(SizeField.Text,NumberStyles.Number,CultureInfo.InvariantCulture,out var value)&&double.IsFinite(value)&&value>=12&&value<=160){updating=true;SizeSlider.Value=value;updating=false;}RefreshSample();}
 bool Read(out OutputAppearance a){a=new OutputAppearance{Font=FontChoice.SelectedItem as string??"Arial",Weight=(WeightChoice.SelectedItem as ComboBoxItem)?.Content?.ToString()??"As scripted",ColorHex=ColorField.Text.Trim(),PreserveColors=Preserve.IsChecked==true};if(!double.TryParse(SizeField.Text,NumberStyles.Number,CultureInfo.InvariantCulture,out var size)||!double.IsFinite(size)||size<12||size>160){ErrorText.Text="Enter a font size between 12 and 160 points.";return false;}if(a.ColorHex.Length!=7||!a.ColorHex.StartsWith("#")||!uint.TryParse(a.ColorHex.Substring(1),NumberStyles.HexNumber,CultureInfo.InvariantCulture,out _)){ErrorText.Text="Enter a colour in #RRGGBB format.";return false;}a.Points=size;ErrorText.Text="";return true;}
 void RefreshSample(){if(!ready||updating)return;if(!Read(out var a))return;FontNotice.Text=a.EffectiveFont==a.Font?"":"Font not installed — Arial will be used instead.";Sample.FontFamily=Cue.FontFamily=new FontFamily(a.EffectiveFont);Sample.FontSize=Math.Clamp(a.SafePoints*.75,14,46);Cue.FontSize=Math.Clamp(Sample.FontSize*.75,12,32);Sample.FontWeight=a.Weight=="Bold"?FontWeights.Bold:FontWeights.Normal;Cue.FontWeight=a.Weight=="Regular"?FontWeights.Normal:FontWeights.Bold;Sample.Foreground=new SolidColorBrush(a.TextColor);Cue.Foreground=a.PreserveColors?Brushes.Cyan:Sample.Foreground;}
 void ChooseColor(object s,RoutedEventArgs e){using var d=new Forms.ColorDialog();if(Read(out var a)){var c=a.TextColor;d.Color=System.Drawing.Color.FromArgb(c.R,c.G,c.B);}if(d.ShowDialog()==Forms.DialogResult.OK)ColorField.Text=$"#{d.Color.R:X2}{d.Color.G:X2}{d.Color.B:X2}";}
 void Reset(object s,RoutedEventArgs e)=>LoadValues(new OutputAppearance());
 void Apply(object s,RoutedEventArgs e){if(Read(out var a)){Result=a;DialogResult=true;}}
}