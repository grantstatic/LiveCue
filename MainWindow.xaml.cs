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
  
  Editor.TextChanged+=(_,_)=>{if(loading)return;dirty=true;if(output==null)RefreshEmbedded();else if(LiveSyncBox.IsChecked==true){editTimer.Stop();editTimer.Start();}};
  editTimer.Tick+=(_,_)=>{editTimer.Stop();SyncLive();};
  saveTimer.Tick+=(_,_)=>SaveRecovery();saveTimer.Start();
  previewTimer.Tick+=(_,_)=>{if(output!=null){double f=output.Range>0?output.Offset/output.Range:0;preview?.SetFraction(f);EmbeddedViewer.ScrollToVerticalOffset(f*EmbeddedViewer.ScrollableHeight);}};previewTimer.Start();
  PreviewKeyDown+=Keys;
  Loaded+=(_,_)=>Startup();
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
 void Live_Click(object s,RoutedEventArgs e){if(output!=null){output.Activate();return;}output=new OutputWindow(Editor.Document);output.Closed+=(_,_)=>{output=null;preview?.Close();preview=null;UpdateStatus();};output.TransportChanged+=UpdateStatus;output.PreviewKeyDown+=Keys;output.Show();RefreshEmbedded();Activate();output.SetMode(ModeSelect.SelectedIndex==1);output.SetSpeed(SpeedSlider.Value);UpdateStatus();}
 void SyncLive(){if(output==null){RefreshEmbedded();return;}try{output.UpdateDocument(Editor.Document);preview?.UpdateDocument(Editor.Document);RefreshEmbedded();}catch(Exception ex){Status.Text="Live sync error: "+ex.Message;}}
 void SyncNow_Click(object s,RoutedEventArgs e)=>SyncLive();
 void ModeChanged(object s,SelectionChangedEventArgs e){output?.SetMode(ModeSelect.SelectedIndex==1);UpdateStatus();}
 void SpeedChanged(object s,RoutedPropertyChangedEventArgs<double> e)=>output?.SetSpeed(e.NewValue);
 void RefreshEmbedded(){using var ms=new MemoryStream();new TextRange(Editor.Document.ContentStart,Editor.Document.ContentEnd).Save(ms,DataFormats.Xaml);ms.Position=0;new TextRange(EmbeddedScript.Document.ContentStart,EmbeddedScript.Document.ContentEnd).Load(ms,DataFormats.Xaml);}
 void Pause_Click(object s,RoutedEventArgs e){output?.Pause();UpdateStatus();}
 void Play_Click(object s,RoutedEventArgs e){if(output==null)Live_Click(s,e);output?.TogglePlay();UpdateStatus();}
 void Reverse_Click(object s,RoutedEventArgs e)=>output?.Reverse();
 void JogBack_Click(object s,RoutedEventArgs e)=>output?.Jog(-1);
 void JogForward_Click(object s,RoutedEventArgs e)=>output?.Jog(1);
 void SendDisplay_Click(object s,RoutedEventArgs e){if(output==null)Live_Click(s,e);var screens=Forms.Screen.AllScreens;if(output!=null&&DisplaySelect.SelectedIndex>=0&&DisplaySelect.SelectedIndex<screens.Length)output.MoveToScreen(screens[DisplaySelect.SelectedIndex]);Activate();}
 void Floating_Click(object s,RoutedEventArgs e){if(output==null)Live_Click(s,e);output?.Floating();}
 void Preview_Click(object s,RoutedEventArgs e){if(output==null)Live_Click(s,e);if(output==null)return;if(preview==null){preview=new PreviewWindow(Editor.Document);preview.Owner=this;preview.Closed+=(_,_)=>preview=null;preview.PreviewKeyDown+=Keys;preview.Show();}else preview.Activate();}
 void UpdateStatus(){if(Status!=null)Status.Text=output==null?"Output closed":(output.Manual?"Manual jog":"Continuous")+" / "+(output.Playing?"Running":"Paused");}
 void PreviewFocus_Click(object s,MouseButtonEventArgs e){if(s is UIElement el)el.Focus();}
 void Keys(object s,KeyEventArgs e){
  var mods=Keyboard.Modifiers;
  if(e.Key==Key.Escape&&output!=null){output.ExitFullscreen();UpdateStatus();e.Handled=true;return;}
  if(mods==ModifierKeys.Control){switch(e.Key){case Key.M:ModeSelect.SelectedIndex=ModeSelect.SelectedIndex==1?0:1;break;case Key.R:output?.Reverse();break;case Key.Enter:SyncLive();break;default:return;}e.Handled=true;UpdateStatus();return;}
  if(mods!=ModifierKeys.None)return;
  if(Editor.IsKeyboardFocusWithin||DisplaySelect.IsKeyboardFocusWithin||ModeSelect.IsKeyboardFocusWithin)return;
  if(e.Key==Key.Space){if(!e.IsRepeat)Play_Click(s,new RoutedEventArgs());e.Handled=true;return;}
  if(output==null)return;
  switch(e.Key){case Key.Up:output.Jog(-1);break;case Key.Down:output.Jog(1);break;case Key.PageUp:output.Jog(-8);break;case Key.PageDown:output.Jog(8);break;case Key.Left:SpeedSlider.Value=Math.Max(SpeedSlider.Minimum,SpeedSlider.Value-5);break;case Key.Right:SpeedSlider.Value=Math.Min(SpeedSlider.Maximum,SpeedSlider.Value+5);break;default:return;}
  e.Handled=true;UpdateStatus();
 }
 void Startup(){
  var dialog=new Window{Title="Welcome to LiveCue",Owner=this,Width=480,SizeToContent=SizeToContent.Height,WindowStartupLocation=WindowStartupLocation.CenterOwner,ResizeMode=ResizeMode.NoResize,Background=new SolidColorBrush(Color.FromRgb(27,27,31)),Foreground=Brushes.White};
  var panel=new StackPanel{Margin=new Thickness(24)};dialog.Content=panel;
  panel.Children.Add(new TextBlock{Text="Welcome to LiveCue",FontSize=26,Foreground=Brushes.White,Margin=new Thickness(0,0,0,18)});
  int choice=0;
  void Add(string title,int value){var b=new Button{Content=title,Padding=new Thickness(12),Margin=new Thickness(0,4,0,4)};b.Click+=(_,_)=>{choice=value;dialog.DialogResult=true;};panel.Children.Add(b);}
  Add("Load a LiveCue project",1);Add("Import a script (TXT / RTF)",2);Add("Use the practice script",3);if(File.Exists(recovery))Add("Recover unsaved script",4);
  Add("Start with a blank script",5);
  dialog.ShowDialog();
  if(choice==1){if(!LoadProject())Practice();}else if(choice==2){Open_Click(this,new RoutedEventArgs());if(new TextRange(Editor.Document.ContentStart,Editor.Document.ContentEnd).Text.Trim().Length==0)Practice();}else if(choice==4){try{using var stream=File.OpenRead(recovery);new TextRange(Editor.Document.ContentStart,Editor.Document.ContentEnd).Load(stream,DataFormats.Rtf);dirty=true;}catch(Exception ex){MessageBox.Show(ex.Message);Practice();}}else if(choice!=5)Practice();
  RefreshEmbedded();
 }
 void Practice(){
  loading=true;try{Editor.Document.Blocks.Clear();foreach(var line in PracticeScript.Text.Split('\n')){if(string.IsNullOrWhiteSpace(line))continue;var run=new Run(line);if(line.StartsWith("[")){run.Foreground=Brushes.Cyan;run.FontWeight=FontWeights.Bold;}Editor.Document.Blocks.Add(new Paragraph(run));}dirty=false;}finally{loading=false;}
 }
 sealed class ProjectData{public string Rtf{get;set;}="";public double Speed{get;set;}=55;public bool Manual{get;set;}public int Display{get;set;}public bool AutoPush{get;set;}}
 void SaveProject_Click(object s,RoutedEventArgs e){var d=new SaveFileDialog{Filter="LiveCue project|*.livecue",DefaultExt=".livecue"};if(d.ShowDialog()!=true)return;try{using var ms=new MemoryStream();new TextRange(Editor.Document.ContentStart,Editor.Document.ContentEnd).Save(ms,DataFormats.Rtf);var p=new ProjectData{Rtf=Convert.ToBase64String(ms.ToArray()),Speed=SpeedSlider.Value,Manual=ModeSelect.SelectedIndex==1,Display=DisplaySelect.SelectedIndex,AutoPush=LiveSyncBox.IsChecked==true};File.WriteAllText(d.FileName,System.Text.Json.JsonSerializer.Serialize(p));dirty=false;if(File.Exists(recovery))File.Delete(recovery);}catch(Exception ex){MessageBox.Show(ex.Message);}}
 bool LoadProject(){var d=new OpenFileDialog{Filter="LiveCue project|*.livecue"};if(d.ShowDialog()!=true)return false;try{var p=System.Text.Json.JsonSerializer.Deserialize<ProjectData>(File.ReadAllText(d.FileName))??throw new InvalidDataException("Invalid project");using var ms=new MemoryStream(Convert.FromBase64String(p.Rtf));loading=true;new TextRange(Editor.Document.ContentStart,Editor.Document.ContentEnd).Load(ms,DataFormats.Rtf);SpeedSlider.Value=Math.Clamp(p.Speed,0,300);ModeSelect.SelectedIndex=p.Manual?1:0;if(p.Display>=0&&p.Display<DisplaySelect.Items.Count)DisplaySelect.SelectedIndex=p.Display;LiveSyncBox.IsChecked=p.AutoPush;dirty=false;return true;}catch(Exception ex){MessageBox.Show(ex.Message);return false;}finally{loading=false;}}
}