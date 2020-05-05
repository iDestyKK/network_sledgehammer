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

namespace Network_Sledgehammer {
	public partial class MainWindow : Window {

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

		private Dictionary<string, Rectangle> tab_rect;
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
			foreach (KeyValuePair<string, Rectangle> obj in tab_rect) {
				obj.Value.Fill = (id == obj.Key)
					? brush_active
					: brush_inactive;
			}
		}

		/*
		 * setup_tabs
		 * 
		 * Sets up tab variables for application interaction.
		 */

		private void setup_tabs() {
			//Generate brushes
			brush_active = (SolidColorBrush)
				new BrushConverter().ConvertFromString("#CF33333A");

			brush_inactive = (SolidColorBrush)
				new BrushConverter().ConvertFromString("#00000000");

			//Populate Dictionary
			tab_rect = new Dictionary<string, Rectangle>();
			tab_rect.Add("networks", rect_networks);
			tab_rect.Add("settings", rect_settings);
			tab_rect.Add("console", rect_console);

			tab_switch("networks");
		}

		// --------------------------------------------------------------------
		// Window Functions/Handlers                                       {{{1
		// --------------------------------------------------------------------

		public MainWindow() {
			InitializeComponent();

			//Run any other setup functions needed
			setup_tabs();
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

		private void rect_console_MouseDown(object sender, MouseButtonEventArgs e) {
			tab_switch("console");
		}
	}
}
