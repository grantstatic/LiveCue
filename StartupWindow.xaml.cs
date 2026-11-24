using System;
using System.Windows;
using System.Windows.Controls;
namespace LiveCue;
public partial class StartupWindow : Window {
 public int Choice { get; private set; }
 public StartupWindow(bool recoveryAvailable){InitializeComponent();RecoveryCard.Visibility=recoveryAvailable?Visibility.Visible:Visibility.Collapsed;MaxHeight=Math.Max(300,SystemParameters.WorkArea.Height-32);Width=Math.Min(900,SystemParameters.WorkArea.Width-32);}
 void Choose(object sender,RoutedEventArgs e){if(sender is Button b && int.TryParse(b.Tag?.ToString(),out int choice)){Choice=choice;DialogResult=true;}}
}