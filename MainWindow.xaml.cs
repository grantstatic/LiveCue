using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using Forms = System.Windows.Forms;
namespace LiveCue;
public partial class MainWindow : Window {
 OutputWindow? output; PreviewWindow? preview; bool dirty; bool loading;
 readonly DispatcherTimer saveTimer=new(){Interval=TimeSpan.FromSeconds(3)};
 readonly DispatcherTimer editTimer=new(){Interval=TimeSpan.FromMilliseconds(500)};
 readonly DispatcherTimer previewTimer=new(){Interval=TimeSpan.FromMilliseconds(50)};
 readonly string recovery=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"LiveCue","recovery.rtf");
 public MainWindow(){
  InitializeComponent();
  foreach(var s in Forms.Screen.AllScreens)DisplaySelect.Items.Add(s.DeviceName+" "+s.Bounds.Width+"x"+s.Bounds.Height);
  DisplaySelect.SelectedIndex=Forms.Screen.AllScreens.Length>1?1:0;
  Editor.Document.Blocks.Add(new Paragraph(new Run("WELCOME TO THE SHOW\n\nGood evening everyone!\n\n[PAUSE FOR APPLAUSE]")));
  Editor.TextChanged+=(_,_)=>{if(loading)return;dirty=true;if(output!=null&&LiveSyncBox.IsChecked==true){editTimer.Stop();editTimer.Start();}};
  editTimer.Tick+=(_,_)=>{editTimer.Stop();SyncLive();};
  saveTimer.Tick+=(_,_)=>SaveRecovery();saveTimer.Start();
  previewTimer.Tick+=(_,_)=>{if(output!=null){double f=output.Range>0?output.Offset/output.Range:0;preview?.SetFraction(f);EmbeddedViewer.ScrollToVerticalOffset(f*EmbeddedViewer.ScrollableHeight);}};previewTimer.Start();
  PreviewKeyDown+=Keys;
  Loaded+=(_,_)=>{if(File.Exists(recovery)&&MessageBox.Show(this,"Recover autosaved script?","LiveCue",MessageBoxButton.YesNo)==MessageBoxResult.Yes){try{loading=true;using var f=File.OpenRead(recovery);new TextRange(Editor.Document.ContentStart,Editor.Document.ContentEnd).Load(f,DataFormats.Rtf);dirty=true;}catch(Exception ex){MessageBox.Show(ex.Message);}finally{loading=false;}}};
  Closing+=(_,e)=>{if(output!=null&&output.IsVisible&&MessageBox.Show(this,"Close the live presenter output?","LiveCue",MessageBoxButton.YesNo)!=MessageBoxResult.Yes){e.Cancel=true;return;}SaveRecovery();};
  Closed+=(_,_)=>{preview?.Close();output?.Close();};
 }
 void SaveRecovery(){if(!dirty)return;try{Directory.CreateDirectory(Path.GetDirectoryName(recovery)!);var temp=recovery+".tmp";using(var f=File.Create(temp))new TextRange(Editor.Document.ContentStart,Editor.Document.ContentEnd).Save(f,DataFormats.Rtf);File.Move(temp,recovery,true);}catch(Exception ex){Status.Text="Autosave failed: "+ex.Message;}}
 void Open_Click(object s,RoutedEventArgs e){var d=new OpenFileDialog{Filter="Scripts|*.rtf;*.txt"};if(d.ShowDialog()!=true)return;try{loading=true;using var f=File.OpenRead(d.FileName);if(Path.GetExtension(d.FileName).Equals(".rtf",StringComparison.OrdinalIgnoreCase))new TextRange(Editor.Document.ContentStart,Editor.Document.ContentEnd).Load(f,DataFormats.Rtf);else using(var reader=new StreamReader(f)){Editor.Document.Blocks.Clear();Editor.Document.Blocks.Add(new Paragraph(new Run(reader.ReadToEnd())));}dirty=false;SyncLive();}catch(Exception ex){MessageBox.Show(ex.Message);}finally{loading=false;}}
 void Save_Click(object s,RoutedEventArgs e){var d=new SaveFileDialog{Filter="Rich text|*.rtf",DefaultExt=".rtf"};if(d.ShowDialog()!=true)return;try{using var f=File.Create(d.FileName);new TextRange(Editor.Document.ContentStart,Editor.Document.ContentEnd).Save(f,DataFormats.Rtf);dirty=false;if(File.Exists(recovery))File.Delete(recovery);}catch(Exception ex){MessageBox.Show(ex.Message);}}
 void Bold_Click(object s,RoutedEventArgs e)=>Editor.Selection.ApplyPropertyValue(TextElement.FontWeightProperty,FontWeights.Bold);
 void White_Click(object s,RoutedEventArgs e)=>SetColor(Brushes.White);
 void Amber_Click(object s,RoutedEventArgs e)=>SetColor(Brushes.Orange);
 void Cyan_Click(object s,RoutedEventArgs e)=>SetColor(Brushes.Cyan);
 void SetColor(Brush color)=>Editor.Selection.ApplyPropertyValue(TextElement.ForegroundProperty,color);
 void Live_Click(object s,RoutedEventArgs e){if(output!=null){output.Activate();return;}output=new OutputWindow(Editor.Document);output.Closed+=(_,_)=>{output=null;preview?.Close();preview=null;UpdateStatus();};output.Show();RefreshEmbedded();output.SetMode(ModeSelect.SelectedIndex==1);output.SetSpeed(SpeedSlider.Value);UpdateStatus();}
 void SyncLive(){if(output==null)return;try{output.UpdateDocument(Editor.Document);preview?.UpdateDocument(Editor.Document);RefreshEmbedded();}catch(Exception ex){Status.Text="Live sync error: "+ex.Message;}}
 void SyncNow_Click(object s,RoutedEventArgs e)=>SyncLive();
 void ModeChanged(object s,SelectionChangedEventArgs e){output?.SetMode(ModeSelect.SelectedIndex==1);UpdateStatus();}
 void SpeedChanged(object s,RoutedPropertyChangedEventArgs<double> e)=>output?.SetSpeed(e.NewValue);
 void RefreshEmbedded(){using var ms=new MemoryStream();new TextRange(Editor.Document.ContentStart,Editor.Document.ContentEnd).Save(ms,DataFormats.Xaml);ms.Position=0;new TextRange(EmbeddedScript.Document.ContentStart,EmbeddedScript.Document.ContentEnd).Load(ms,DataFormats.Xaml);}
 void Pause_Click(object s,RoutedEventArgs e){output?.Pause();UpdateStatus();}
 void Play_Click(object s,RoutedEventArgs e){output?.TogglePlay();UpdateStatus();}
 void Reverse_Click(object s,RoutedEventArgs e)=>output?.Reverse();
 void JogBack_Click(object s,RoutedEventArgs e)=>output?.Jog(-1);
 void JogForward_Click(object s,RoutedEventArgs e)=>output?.Jog(1);
 void SendDisplay_Click(object s,RoutedEventArgs e){if(output==null)Live_Click(s,e);var screens=Forms.Screen.AllScreens;if(output!=null&&DisplaySelect.SelectedIndex>=0&&DisplaySelect.SelectedIndex<screens.Length)output.MoveToScreen(screens[DisplaySelect.SelectedIndex]);}
 void Floating_Click(object s,RoutedEventArgs e){if(output==null)Live_Click(s,e);output?.Floating();}
 void Preview_Click(object s,RoutedEventArgs e){if(output==null)Live_Click(s,e);if(output==null)return;if(preview==null){preview=new PreviewWindow(Editor.Document);preview.Owner=this;preview.Closed+=(_,_)=>preview=null;preview.Show();}else preview.Activate();}
 void UpdateStatus(){if(Status!=null)Status.Text=output==null?"Output closed":(output.Manual?"Manual jog":"Continuous")+" / "+(output.Playing?"Running":"Paused");}
 void Keys(object s,KeyEventArgs e){
  if(e.Key==Key.F5){Live_Click(s,new RoutedEventArgs());e.Handled=true;return;}
  if(output==null)return;
  switch(e.Key){case Key.Escape:output.Pause();break;case Key.F6:output.TogglePlay();break;case Key.F7:output.Pause();break;case Key.F8:ModeSelect.SelectedIndex=ModeSelect.SelectedIndex==1?0:1;break;case Key.F9:output.Jog(-1);break;case Key.F10:output.Jog(1);break;case Key.F11:output.Reverse();break;case Key.F12:SyncLive();break;default:return;}
  e.Handled=true;UpdateStatus();
 }
}