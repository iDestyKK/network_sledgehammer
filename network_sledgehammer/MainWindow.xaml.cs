using System;
using System.Net;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Interop;
using System.Security.Policy;
using NativeWifi;
using network_sledgehammer;
using System.Reflection;
using System.Diagnostics;

namespace Network_Sledgehammer {
	public partial class MainWindow : Window {

		private bool sh_on;
		private Thread thd_sh;

		/* helper classes */
		private net_func     net_util;
		private sledgehammer hammer;

		/*
		 * test_connection
		 * 
		 * Tests connecting to the internet with a specified wifi interface and
		 * access point. Returns true if it was successful. Returns false
		 * otherwise.
		 * 
		 * Errors regarding connection will be displayed via MessageBox.
		 */

		private bool test_connection() {
			int    result;
			string error_str;

			result    = net_util.try_connect();
			error_str = "";

			switch (result) {
				case 0:
					//Success
					break;

				case 1:
					//Invalid Interface
					error_str = "Error: Interface is not set or is invalid.";
					break;

				case 2:
					//Invalid Access Point
					error_str =
						"Error: Access Point is not set or is invalid.";
					break;

				case 3:
					//Access Point has no existing profile
					error_str =
						"Error: Access Point has no profile set. Connect to " +
						"this Wi-Fi network through Windows first. Then try " +
						"to connect through this.";
					break;

				case 4:
					//Failed to establish connection
					error_str =
						"Error: Failed to establish connection in time. " +
						"Timed out after 5 seconds.";
					break;
			}

			if (result > 0) {
				//Show error message box
				MessageBox.Show(
					error_str, "Error",
					MessageBoxButton.OK, MessageBoxImage.Error
				);
			}

			return (result == 0);
		}

		/*
		 * sledgehammer_toggle
		 * 
		 * Enables/Disables the sledgehammer.
		 */

		private bool sledgehammer_toggle(sledgehammer hammer) {
			if (!sh_on) {
				int i_id, ap_id;

				//Test configuration
				if (config.is_valid() != 0)
					return false;

				//Start by testing connection.
				if (!test_connection())
					return false;

				//Ok it was able to connect. Now start up the thread.
				i_id  = combobox_network_interfaces   .SelectedIndex;
				ap_id = combobox_network_access_points.SelectedIndex;

				thd_sh = new Thread(new ThreadStart(() => hammer.thread(
					net_util, i_id, ap_id
				)));

				thd_sh.Start();
			}
			else {
				//Brutally slaughter the thread. We're done here.
				thd_sh.Abort();
			}

			//Flip variable and we're done.
			sh_on = !sh_on;
			return true;
		}

		// --------------------------------------------------------------------
		// Windows 10 Blur Behind Hack                                     {{{1
		// --------------------------------------------------------------------

		/*
		 * Transparent window stuff clearly stolen from this:
		 * https://github.com/riverar/sample-win10-aeroglass/blob/master/MainWindow.xaml.cs
		 */

		internal enum AccentState {
			ACCENT_DISABLED = 0,
			ACCENT_ENABLE_GRADIENT = 1,
			ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
			ACCENT_ENABLE_BLURBEHIND = 3,
			ACCENT_INVALID_STATE = 4
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct AccentPolicy {
			public AccentState AccentState;
			public int AccentFlags;
			public int GradientColor;
			public int AnimationId;
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct WindowCompositionAttributeData {
			public WindowCompositionAttribute Attribute;
			public IntPtr Data;
			public int SizeOfData;
		}

		internal enum WindowCompositionAttribute {
			WCA_ACCENT_POLICY = 19
		}

		[DllImport("user32.dll")]
		internal static extern int SetWindowCompositionAttribute(
			IntPtr hwnd, ref WindowCompositionAttributeData data
		);

		internal void EnableBlur() {
			var windowHelper = new WindowInteropHelper(this);

			var accent = new AccentPolicy();
			accent.AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND;

			var accentStructSize = Marshal.SizeOf(accent);

			var accentPtr = Marshal.AllocHGlobal(accentStructSize);
			Marshal.StructureToPtr(accent, accentPtr, false);

			var data = new WindowCompositionAttributeData();
			data.Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY;
			data.SizeOfData = accentStructSize;
			data.Data = accentPtr;

			SetWindowCompositionAttribute(windowHelper.Handle, ref data);

			Marshal.FreeHGlobal(accentPtr);
		}

		// --------------------------------------------------------------------
		// Tab Interaction                                                 {{{1
		// --------------------------------------------------------------------

		private Dictionary<string, KeyValuePair<Rectangle, Grid> > tab_rect;
		private SolidColorBrush brush_active, brush_inactive;

		/*
		 * tab_switch
		 * 
		 * Switch to a tab based on it's ID "id". Rectangles behind the left
		 * bar's text will change visibility and right side elements will
		 * change.
		 */

		private void tab_switch(string id) {
			//Don't do anything if key is invalid
			if (!tab_rect.ContainsKey(id))
				return;

			//Adjust visibility accordingly
			foreach (KeyValuePair<string, KeyValuePair<Rectangle, Grid> > obj in tab_rect) {
				//Tab Roundrect
				obj.Value.Key.Fill = (id == obj.Key)
					? brush_active
					: brush_inactive;

				//Grid
				obj.Value.Value.Visibility = (id == obj.Key)
					? Visibility.Visible
					: Visibility.Hidden;
			}
		}

		/*
		 * brush_from_rgba
		 * 
		 * Self-explanatory
		 */

		SolidColorBrush brush_from_rgba(string hex) {
			return (SolidColorBrush)
				new BrushConverter().ConvertFromString(hex);
		}

		/*
		 * tab_init
		 * 
		 * Sets up tab variables for application interaction.
		 */

		private void tab_init() {
			//Generate brushes
			brush_active   = brush_from_rgba("#CF33333A");
			brush_inactive = brush_from_rgba("#00000000");

			//Populate Dictionary
			tab_rect = new Dictionary<string, KeyValuePair<Rectangle, Grid> >();

			tab_rect.Add("networks", new KeyValuePair<Rectangle, Grid>(
				rect_networks, grid_networks));

			tab_rect.Add("settings", new KeyValuePair<Rectangle, Grid>(
				rect_settings, grid_settings));

			tab_rect.Add("console" , new KeyValuePair<Rectangle, Grid>(
				rect_console , grid_console ));

			tab_rect.Add("about", new KeyValuePair<Rectangle, Grid>(
				rect_about   , grid_about));

			//Default is the "networks" tab.
			tab_switch("networks");
		}

		// --------------------------------------------------------------------
		// Window Initialiser                                              {{{1
		// --------------------------------------------------------------------

		private SolidColorBrush brush_sh_on_bg, brush_sh_off_bg,
		                        brush_sh_on_bd, brush_sh_off_bd,
		                        brush_sh_on_fg, brush_sh_off_fg;

		public MainWindow() {
			Assembly assem;
			string ver;

			InitializeComponent();

			//Fill in version number
			assem = Assembly.GetExecutingAssembly();
			ver = FileVersionInfo.GetVersionInfo(assem.Location).FileVersion;
			ver = ver.Substring(0, ver.LastIndexOf('.'));
			version_num.Text = ver;

			//Setup logger
			log.setup_dispatcher(this.Dispatcher);
			log.setup_textbox   (textBox_console);

			//Setup config form fields
			config.textbox_ping_url = textBox_url;
			config.textbox_attempts = textBox_attempts;
			config.textbox_delay    = textBox_delay;

			//Load configuration file (or create if it doesn't exist)
			config.load_config("config.txt");

			//Run any other setup functions needed
			tab_init();

			//Setup net function helper
			net_util = new net_func();

			net_util.setup_interface_combobox   (combobox_network_interfaces   );
			net_util.setup_access_point_combobox(combobox_network_access_points);
			net_util.update_adapter();

			//Setup sledgehammer helper
			hammer = new sledgehammer();

			//Setup the brushes for sledgehammer on and off
			brush_sh_off_bg = brush_from_rgba("#FF3A0A0A");
			brush_sh_off_bd = brush_from_rgba("#FF640000");
			brush_sh_off_fg = brush_from_rgba("#FFD40000");
			brush_sh_on_bg  = brush_from_rgba("#FF0C3A0A");
			brush_sh_on_bd  = brush_from_rgba("#FF006409");
			brush_sh_on_fg  = brush_from_rgba("#FF30D400");

			//Sledgehammer is "off" by default;
			sh_on = false;
		}

		// --------------------------------------------------------------------
		// Window Event Handlers                                           {{{1
		// --------------------------------------------------------------------

		private void Window_Loaded(object sender, RoutedEventArgs e) {
			EnableBlur();
		}

		private void Rectangle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
			this.DragMove();
		}

		private void rect_networks_MouseDown(object sender, MouseButtonEventArgs e) {
			tab_switch("networks");
		}

		private void rect_settings_MouseDown(object sender, MouseButtonEventArgs e) {
			tab_switch("settings");
		}

		private void rect_about_MouseDown(object sender, MouseButtonEventArgs e) {
			tab_switch("about");
		}

		private void button_minimise_Click(object sender, RoutedEventArgs e) {
			this.WindowState = WindowState.Minimized;
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
			//If closing while the thread is doing work, just obliterate it.
			if (sh_on)
				thd_sh.Abort();
		}

		private void button_close_Click(object sender, RoutedEventArgs e) {
			this.Close();
		}

		private void combobox_network_interfaces_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			net_util.update_access_point();
		}

		private void button_connect_Click(object sender, RoutedEventArgs e) {
			test_connection();
		}

		private void button_save_Click(object sender, RoutedEventArgs e) {
			config.save_config("config.txt");
		}

		private void button_start_Click(object sender, RoutedEventArgs e) {
			if (sledgehammer_toggle(hammer)) {
				//We changed modes successfully.

				//Change colours
				button_start.Background  = sh_on ? brush_sh_on_bg : brush_sh_off_bg;
				button_start.BorderBrush = sh_on ? brush_sh_on_bd : brush_sh_off_bd;
				button_start.Foreground  = sh_on ? brush_sh_on_fg : brush_sh_off_fg;

				//Adjust whether we can modify fields or not
				textBox_url                   .IsReadOnly = sh_on;
				textBox_delay                 .IsReadOnly = sh_on;
				textBox_attempts              .IsReadOnly = sh_on;
				combobox_network_interfaces   .IsReadOnly = sh_on; /* TODO: Fix. This doesn't work */
				combobox_network_access_points.IsReadOnly = sh_on; /* ditto tbh */

				//Adjust text too
				button_start.Content = sh_on ? "Sledgehammer On" : "Sledgehammer Off";
			}
		}

		private void rect_console_MouseDown(object sender, MouseButtonEventArgs e) {
			tab_switch("console");
		}
	}
}
