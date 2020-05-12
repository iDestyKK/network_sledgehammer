using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;
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

namespace Network_Sledgehammer {
	public partial class MainWindow : Window {

		/* helper classes */
		private net_func net_util;

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
		 * tab_init
		 * 
		 * Sets up tab variables for application interaction.
		 */

		private void tab_init() {
			//Generate brushes
			brush_active = (SolidColorBrush)
				new BrushConverter().ConvertFromString("#CF33333A");

			brush_inactive = (SolidColorBrush)
				new BrushConverter().ConvertFromString("#00000000");

			//Populate Dictionary
			tab_rect = new Dictionary<string, KeyValuePair<Rectangle, Grid> >();
			tab_rect.Add("networks", new KeyValuePair<Rectangle, Grid>(rect_networks, grid_networks));
			tab_rect.Add("settings", new KeyValuePair<Rectangle, Grid>(rect_settings, grid_settings));
			tab_rect.Add("console" , new KeyValuePair<Rectangle, Grid>(rect_console , grid_console ));

			tab_switch("networks");
		}

		// --------------------------------------------------------------------
		// Window Functions/Handlers                                       {{{1
		// --------------------------------------------------------------------

		public MainWindow() {
			InitializeComponent();

			//Setup config form fields
			config.textbox_ping_url = textBox_url;
			config.textbox_attempts = textBox_attempts;
			config.textbox_delay    = textBox_delay;

			//Load configuration file (or create if it doesn't exist)
			config.load_config("config.txt");

			//Run any other setup functions needed
			tab_init();
			net_util = new net_func();

			net_util.setup_interface_combobox   (combobox_network_interfaces   );
			net_util.setup_access_point_combobox(combobox_network_access_points);

			//p sweet tbh
			net_util.update_adapter();
		}

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

		private void button_minimise_Click(object sender, RoutedEventArgs e) {
			this.WindowState = WindowState.Minimized;
		}

		private void button_close_Click(object sender, RoutedEventArgs e) {
			this.Close();
		}

		private void combobox_network_interfaces_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			net_util.update_access_point();
		}

		private void button_connect_Click(object sender, RoutedEventArgs e) {
			int result = net_util.try_connect();

			switch (result) {
				case 0:
					//Success
					break;
				case 1:
					//Invalid Interface
					MessageBox.Show(
						"Error: Interface is not set or is invalid."
					);
					break;
				case 2:
					//Invalid Access Point
					MessageBox.Show(
						"Error: Access Point is not set or is invalid."
					);
					break;
				case 3:
					//Access Point has no existing profile
					MessageBox.Show(
						"Error: Access Point has no profile set. Connect to " +
						"this Wi-Fi network through Windows first. Then try " +
						"to connect through this."
					);
					break;
			}
		}

		private void button_save_Click(object sender, RoutedEventArgs e) {
			config.save_config("config.txt");
		}

		private void rect_console_MouseDown(object sender, MouseButtonEventArgs e) {
			tab_switch("console");
		}
	}
}
