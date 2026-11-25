using System;
using System.IO;
using System.Globalization;
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
 OutputAppearance appearance=new(); FlowDocument? publishedDocument;
 readonly PresenterSurface surface=new(); bool appearanceReady,updatingAppearance,appearanceQueued;
 readonly DispatcherTimer saveTimer=new(){Interval=TimeSpan.FromSeconds(3)};
 readonly DispatcherTimer editTimer=new(){Interval=TimeSpan.FromMilliseconds(500)};
 readonly string recovery=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"LiveCue","recovery.rtf");
 public MainWindow(){
  InitializeComponent();
  AttachStandby();surface.TransportChanged+=UpdateStatus;FontChoice.ItemsSource=OutputAppearance.FontChoices;appearanceReady=true;LoadAppearanceControls();
  foreach(var s in Forms.Screen.AllScreens)DisplaySelect.Items.Add(s.DeviceName+" "+s.Bounds.Width+"x"+s.Bounds.Height);
  DisplaySelect.SelectedIndex=Forms.Screen.AllScreens.Length>1?1:0;
  
  Editor.TextChanged+=(_,_)=>{if(loading)return;dirty=true;if(output==null||LiveSyncBox.IsChecked==true){editTimer.Stop();editTimer.Start();}};
  editTimer.Tick+=(_,_)=>{editTimer.Stop();SyncLive();};
  saveTimer.Tick+=(_,_)=>SaveRecovery();saveTimer.Start();
  PreviewKeyDown+=Keys;
  Loaded+=(_,_)=>Startup();
  Closing+=(_,e)=>{if(output!=null&&output.IsVisible&&MessageBox.Show(this,"Close the live presenter output?","LiveCue",MessageBoxButton.YesNo)!=MessageBoxResult.Yes){e.Cancel=true;return;}SaveRecovery();};
  Closed+=(_,_)=>{saveTimer.Stop();editTimer.Stop();surface.Pause();preview?.Close();output?.Close();};
 }
 void SaveRecovery(){if(!dirty)return;try{Directory.CreateDirectory(Path.GetDirectoryName(recovery)!);var temp=recovery+".tmp";using(var f=File.Create(temp))new TextRange(Editor.Document.ContentStart,Editor.Document.ContentEnd).Save(f,DataFormats.Rtf);File.Move(temp,recovery,true);}catch(Exception ex){Status.Text="Autosave failed: "+ex.Message;}}
 void Open_Click(object s,RoutedEventArgs e){var d=new OpenFileDialog{Filter="Scripts|*.rtf;*.txt"};if(d.ShowDialog()!=true)return;try{loading=true;using var f=File.OpenRead(d.FileName);if(Path.GetExtension(d.FileName).Equals(".rtf",StringComparison.OrdinalIgnoreCase))new TextRange(Editor.Document.ContentStart,Editor.Document.ContentEnd).Load(f,DataFormats.Rtf);else using(var reader=new StreamReader(f)){Editor.Document.Blocks.Clear();Editor.Document.Blocks.Add(new Paragraph(new Run(reader.ReadToEnd())));}dirty=false;SyncLive();}catch(Exception ex){MessageBox.Show(ex.Message);}finally{loading=false;}}
 void Save_Click(object s,RoutedEventArgs e){var d=new SaveFileDialog{Filter="Rich text|*.rtf",DefaultExt=".rtf"};if(d.ShowDialog()!=true)return;try{using var f=File.Create(d.FileName);new TextRange(Editor.Document.ContentStart,Editor.Document.ContentEnd).Save(f,DataFormats.Rtf);dirty=false;if(File.Exists(recovery))File.Delete(recovery);}catch(Exception ex){MessageBox.Show(ex.Message);}}
 void Bold_Click(object s,RoutedEventArgs e)=>Editor.Selection.ApplyPropertyValue(TextElement.FontWeightProperty,FontWeights.Bold);
 void White_Click(object s,RoutedEventArgs e)=>SetColor(Brushes.White);
 void Amber_Click(object s,RoutedEventArgs e)=>SetColor(Brushes.Orange);
 void Cyan_Click(object s,RoutedEventArgs e)=>SetColor(Brushes.Cyan);
 void SetColor(Brush color)=>Editor.Selection.ApplyPropertyValue(TextElement.ForegroundProperty,color);
 void AttachStandby(){surface.Width=1280;surface.Height=720;StandbyHost.Child=surface;StandbyHost.Visibility=Visibility.Visible;EmbeddedMirror.Fill=null;EmbeddedMirror.Visibility=Visibility.Collapsed;}
 void Live_Click(object s,RoutedEventArgs e){
  if(output!=null){output.Activate();return;}
  editTimer.Stop();publishedDocument=OutputAppearance.Clone(Editor.Document);surface.UpdateDocument(appearance.Render(publishedDocument));
  StandbyHost.Child=null;StandbyHost.Visibility=Visibility.Collapsed;surface.Width=double.NaN;surface.Height=double.NaN;
  var window=new OutputWindow(surface);output=window;EmbeddedMirror.Fill=surface.Mirror();EmbeddedMirror.Visibility=Visibility.Visible;
  window.Closed+=(_,_)=>{surface.Pause();window.ReleaseSurface();output=null;publishedDocument=null;AttachStandby();RefreshEmbedded();UpdateStatus();};
  window.PreviewKeyDown+=Keys;window.Show();Activate();surface.SetMode(ModeSelect.SelectedIndex==1);surface.SetSpeed(SpeedSlider.Value);UpdateStatus();
 }
 void SyncLive(){try{if(output!=null)publishedDocument=OutputAppearance.Clone(Editor.Document);RefreshEmbedded();}catch(Exception ex){Status.Text="Live sync error: "+ex.Message;}}
 void SyncNow_Click(object s,RoutedEventArgs e)=>SyncLive();
 void ModeChanged(object s,SelectionChangedEventArgs e){if(ModeSelect!=null)surface.SetMode(ModeSelect.SelectedIndex==1);UpdateStatus();}
 void SpeedChanged(object s,RoutedPropertyChangedEventArgs<double> e)=>surface.SetSpeed(e.NewValue);
 void RefreshEmbedded(){surface.UpdateDocument(appearance.Render(publishedDocument??Editor.Document));}
 void LoadAppearanceControls(){updatingAppearance=true;try{FontChoice.SelectedItem=appearance.Font;if(FontChoice.SelectedIndex<0)FontChoice.SelectedIndex=0;SizeSlider.Value=appearance.SafePoints;SizeField.Text=appearance.SafePoints.ToString("0.##",CultureInfo.InvariantCulture);WeightChoice.SelectedIndex=appearance.Weight=="Bold"?2:appearance.Weight=="Regular"?1:0;CaseChoice.SelectedIndex=appearance.AllCaps?1:0;ColorField.Text=appearance.ColorHex;ColorSwatch.Background=new SolidColorBrush(appearance.TextColor);Preserve.IsChecked=appearance.PreserveColors;FontNotice.Text=appearance.EffectiveFont==appearance.Font?"":"Font unavailable — using Arial";AppearanceError.Text="";}finally{updatingAppearance=false;}}
 void AppearanceChanged(object s,RoutedEventArgs e)=>ReadAppearance();
 void ColorChanged(object s,TextChangedEventArgs e)=>ReadAppearance();
 void SizeChanged(object s,RoutedPropertyChangedEventArgs<double> e){if(!appearanceReady||updatingAppearance)return;updatingAppearance=true;SizeField.Text=e.NewValue.ToString("0",CultureInfo.InvariantCulture);updatingAppearance=false;ReadAppearance();}
 void SizeTyped(object s,TextChangedEventArgs e){if(!appearanceReady||updatingAppearance)return;if(double.TryParse(SizeField.Text,NumberStyles.Number,CultureInfo.InvariantCulture,out var val)&&double.IsFinite(val)&&val>=12&&val<=160){updatingAppearance=true;SizeSlider.Value=val;updatingAppearance=false;}ReadAppearance();}
 void ReadAppearance(){
  if(!appearanceReady||updatingAppearance)return;
  if(!double.TryParse(SizeField.Text,NumberStyles.Number,CultureInfo.InvariantCulture,out var size)||!double.IsFinite(size)||size<12||size>160){AppearanceError.Text="Size must be 12–160 pt.";return;}
  var hex=ColorField.Text.Trim();if(hex.Length!=7||!hex.StartsWith("#")||!uint.TryParse(hex.Substring(1),NumberStyles.HexNumber,CultureInfo.InvariantCulture,out _)){AppearanceError.Text="Use #RRGGBB for colour.";return;}
  appearance=new OutputAppearance{Font=FontChoice.SelectedItem as string??"Arial",Points=size,Weight=(WeightChoice.SelectedItem as ComboBoxItem)?.Content?.ToString()??"As scripted",AllCaps=CaseChoice.SelectedIndex==1,ColorHex=hex,PreserveColors=Preserve.IsChecked==true};
  AppearanceError.Text="";FontNotice.Text=appearance.EffectiveFont==appearance.Font?"":"Font unavailable — using Arial";ColorSwatch.Background=new SolidColorBrush(appearance.TextColor);dirty=true;QueueAppearance();
 }
 void QueueAppearance(){if(appearanceQueued)return;appearanceQueued=true;Dispatcher.BeginInvoke(new Action(()=>{appearanceQueued=false;try{RefreshEmbedded();}catch(Exception ex){AppearanceError.Text="Display update: "+ex.Message;}}),DispatcherPriority.Render);}
 void ResetAppearance(object s,RoutedEventArgs e){appearance=new();LoadAppearanceControls();dirty=true;QueueAppearance();}
 void ChooseColor(object s,RoutedEventArgs e){using var d=new Forms.ColorDialog();var color=appearance.TextColor;d.Color=System.Drawing.Color.FromArgb(color.R,color.G,color.B);if(d.ShowDialog()==Forms.DialogResult.OK)ColorField.Text=$"#{d.Color.R:X2}{d.Color.G:X2}{d.Color.B:X2}";}
 void Pause_Click(object s,RoutedEventArgs e)=>surface.Pause();
 void Play_Click(object s,RoutedEventArgs e){if(output==null)Live_Click(s,e);surface.TogglePlay();}
 void Reverse_Click(object s,RoutedEventArgs e)=>surface.Reverse();
 void JogBack_Click(object s,RoutedEventArgs e)=>surface.Jog(-1);
 void JogForward_Click(object s,RoutedEventArgs e)=>surface.Jog(1);
 void SendDisplay_Click(object s,RoutedEventArgs e){if(output==null)Live_Click(s,e);var screens=Forms.Screen.AllScreens;if(output!=null&&DisplaySelect.SelectedIndex>=0&&DisplaySelect.SelectedIndex<screens.Length)output.MoveToScreen(screens[DisplaySelect.SelectedIndex]);Activate();}
 void Floating_Click(object s,RoutedEventArgs e){if(output==null)Live_Click(s,e);output?.Floating();}
 void Preview_Click(object s,RoutedEventArgs e){if(preview==null){preview=new PreviewWindow(surface);preview.Owner=this;preview.Closed+=(_,_)=>preview=null;preview.PreviewKeyDown+=Keys;preview.Show();}else preview.Activate();}
 void UpdateStatus(){if(Status!=null)Status.Text=output==null?"Output closed":(surface.Manual?"Manual jog":"Continuous")+" / "+(surface.Playing?"Running":"Paused");}
 void PreviewFocus_Click(object s,MouseButtonEventArgs e){if(s is UIElement el)el.Focus();}
 void Keys(object s,KeyEventArgs e){
  var mods=Keyboard.Modifiers;
  if(e.Key==Key.Escape&&output!=null){output.ExitFullscreen();UpdateStatus();e.Handled=true;return;}
  if(mods==ModifierKeys.Control){switch(e.Key){case Key.M:ModeSelect.SelectedIndex=ModeSelect.SelectedIndex==1?0:1;break;case Key.R:surface.Reverse();break;case Key.Enter:SyncLive();break;default:return;}e.Handled=true;UpdateStatus();return;}
  if(mods!=ModifierKeys.None)return;
  if(Editor.IsKeyboardFocusWithin||DisplaySelect.IsKeyboardFocusWithin||ModeSelect.IsKeyboardFocusWithin||AppearanceControls.IsKeyboardFocusWithin)return;
  if(e.Key==Key.Space){if(!e.IsRepeat)Play_Click(s,new RoutedEventArgs());e.Handled=true;return;}
  if(output==null)return;
  switch(e.Key){case Key.Up:surface.Jog(-1);break;case Key.Down:surface.Jog(1);break;case Key.PageUp:surface.Jog(-8);break;case Key.PageDown:surface.Jog(8);break;case Key.Left:SpeedSlider.Value=Math.Max(SpeedSlider.Minimum,SpeedSlider.Value-5);break;case Key.Right:SpeedSlider.Value=Math.Min(SpeedSlider.Maximum,SpeedSlider.Value+5);break;default:return;}
  e.Handled=true;UpdateStatus();
 }
 void Startup(){
  var dialog=new StartupWindow(File.Exists(recovery)){Owner=this};
  dialog.ShowDialog();
  int choice=dialog.Choice;
  if(choice==1){if(!LoadProject())Practice();}else if(choice==2){Open_Click(this,new RoutedEventArgs());if(new TextRange(Editor.Document.ContentStart,Editor.Document.ContentEnd).Text.Trim().Length==0)Practice();}else if(choice==4){try{using var stream=File.OpenRead(recovery);new TextRange(Editor.Document.ContentStart,Editor.Document.ContentEnd).Load(stream,DataFormats.Rtf);dirty=true;}catch(Exception ex){MessageBox.Show(ex.Message);Practice();}}else if(choice!=5)Practice();
  RefreshEmbedded();
 }
 void Practice(){
  loading=true;try{Editor.Document.Blocks.Clear();foreach(var line in PracticeScript.Text.Split('\n')){if(string.IsNullOrWhiteSpace(line))continue;var run=new Run(line);if(line.StartsWith("[")){run.Foreground=Brushes.Cyan;run.FontWeight=FontWeights.Bold;}Editor.Document.Blocks.Add(new Paragraph(run));}dirty=false;}finally{loading=false;}
 }
 sealed class ProjectData{public string Rtf{get;set;}="";public double Speed{get;set;}=55;public bool Manual{get;set;}public int Display{get;set;}public bool AutoPush{get;set;}public OutputAppearance? Appearance{get;set;}}
 void SaveProject_Click(object s,RoutedEventArgs e){var d=new SaveFileDialog{Filter="LiveCue project|*.livecue",DefaultExt=".livecue"};if(d.ShowDialog()!=true)return;try{using var ms=new MemoryStream();new TextRange(Editor.Document.ContentStart,Editor.Document.ContentEnd).Save(ms,DataFormats.Rtf);var p=new ProjectData{Rtf=Convert.ToBase64String(ms.ToArray()),Speed=SpeedSlider.Value,Manual=ModeSelect.SelectedIndex==1,Display=DisplaySelect.SelectedIndex,AutoPush=LiveSyncBox.IsChecked==true,Appearance=appearance};File.WriteAllText(d.FileName,System.Text.Json.JsonSerializer.Serialize(p));dirty=false;if(File.Exists(recovery))File.Delete(recovery);}catch(Exception ex){MessageBox.Show(ex.Message);}}
 bool LoadProject(){var d=new OpenFileDialog{Filter="LiveCue project|*.livecue"};if(d.ShowDialog()!=true)return false;try{var p=System.Text.Json.JsonSerializer.Deserialize<ProjectData>(File.ReadAllText(d.FileName))??throw new InvalidDataException("Invalid project");using var ms=new MemoryStream(Convert.FromBase64String(p.Rtf));loading=true;new TextRange(Editor.Document.ContentStart,Editor.Document.ContentEnd).Load(ms,DataFormats.Rtf);SpeedSlider.Value=Math.Clamp(p.Speed,0,300);ModeSelect.SelectedIndex=p.Manual?1:0;if(p.Display>=0&&p.Display<DisplaySelect.Items.Count)DisplaySelect.SelectedIndex=p.Display;LiveSyncBox.IsChecked=p.AutoPush;appearance=p.Appearance??new OutputAppearance();LoadAppearanceControls();dirty=false;return true;}catch(Exception ex){MessageBox.Show(ex.Message);return false;}finally{loading=false;}}
}