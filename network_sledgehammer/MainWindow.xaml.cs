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
		// Network Functionality                                           {{{1
		// --------------------------------------------------------------------

		/*
		 * Functions/Variables for dealing with the network.
		 */

		//Data Structures
		private Wlan.WlanAvailableNetwork[] networks;
		private WlanClient                  client;
		private WlanClient.WlanInterface[]  interfaces;

		//Cache existing profile information
		private Dictionary<
			WlanClient.WlanInterface, Dictionary<string, string> > interface_profile;

		static string GetStringForSSID(Wlan.Dot11Ssid ssid) {
			return Encoding.ASCII.GetString(ssid.SSID, 0, (int)ssid.SSIDLength);
		}

		/*
		 * network_init
		 * 
		 * Sets up the network variables to default values. Run this in the
		 * Window's constructor.
		 */

		private void network_init() {
			client = new WlanClient();

			interface_profile = new Dictionary<
				WlanClient.WlanInterface, Dictionary<string, string> >();
		}

		/*
		 * network_update_adapter
		 * 
		 * Updates network adapter data structures to hold recent information.
		 */

		private void network_update_adapter(ComboBox cb, ComboBox cb_ap) {
			//Wipe out "cb"'s information
			cb.Items.Clear();
			cb.SelectedItem = null;

			//Since "cb" was cleared out, "cb_ap" also needs to be cleared out.
			cb_ap.Items.Clear();
			cb_ap.SelectedItem = null;

			//Clear out data structures as well
			interface_profile.Clear();

			interfaces = client.Interfaces;

			foreach (WlanClient.WlanInterface mw_inter in interfaces) {
				cb.Items.Add(mw_inter.InterfaceName);

				interface_profile.Add(mw_inter, new Dictionary<string, string>());

				//Add profiles to the cache
				foreach (Wlan.WlanProfileInfo pinf in mw_inter.GetProfiles()) {
					interface_profile[mw_inter].Add(
						pinf.profileName,
						mw_inter.GetProfileXml(pinf.profileName)
					);
				}
			}
		}

		/*
		 * network_update_access_point
		 * 
		 * Updates access points available in the access point combobox. This
		 * requires information from the Adapter Combobox "cb" to modify the
		 * Access Point Combobox "cb_ap".
		 */

		private void network_update_access_point(ComboBox cb, ComboBox cb_ap) {
			//Invalid/none item being selected
			if (cb.SelectedIndex == -1)
				return;

			//Wipe out "cb_ap"'s information
			cb_ap.Items.Clear();
			cb_ap.SelectedItem = null;

			//Ok, get all access points and populate "cb_ap".
			interfaces[cb.SelectedIndex].Scan();
			networks = interfaces[cb.SelectedIndex].GetAvailableNetworkList(0);

			foreach (Wlan.WlanAvailableNetwork network in networks)
				cb_ap.Items.Add(GetStringForSSID(network.dot11Ssid));
		}

		/*
		 * network_try_connect
		 * 
		 * Try to connect to a Wifi Access Point. This relies on the interface
		 * specified in "cb" as well as the access point specified in "cb_ap".
		 * If they are not specified properly or the Wi-Fi profile does not
		 * exist, the command will fail with an appropriate error code
		 * returned.
		 */

		private int network_try_connect(ComboBox cb, ComboBox cb_ap) {
			WlanClient.WlanInterface mw_inter;
			Wlan.WlanAvailableNetwork network;
			string ssid, prof_name, prof_xml;

			//Invalid adapter
			if (cb.SelectedIndex == -1)
				return 1;

			//Invalid access point
			if (cb_ap.SelectedIndex == -1)
				return 2;

			mw_inter = interfaces[cb   .SelectedIndex];
			network  = networks  [cb_ap.SelectedIndex];
			ssid     = GetStringForSSID(network.dot11Ssid);

			//Profile doesn't exist
			if (!interface_profile[mw_inter].ContainsKey(ssid))
				return 3;

			//Ok, time to connect
			prof_name = ssid;
			prof_xml  = interface_profile[mw_inter][ssid];

			try {
				mw_inter.SetProfile(
					Wlan.WlanProfileFlags.AllUser,
					prof_xml,
					true
				);

				mw_inter.Connect(
					Wlan.WlanConnectionMode.Profile,
					Wlan.Dot11BssType.Any,
					prof_name
				);
			}
			catch (Exception e) {
				/* A VERY bad idea but whatever */
			}

			//Ok... assume it worked.
			return 0;
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

			//Run any other setup functions needed
			tab_init();
			network_init();

			//p sweet tbh
			network_update_adapter(
				combobox_network_interfaces,
				combobox_network_access_points
			);
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
			network_update_access_point(
				combobox_network_interfaces,
				combobox_network_access_points
			);
		}

		private void button_connect_Click(object sender, RoutedEventArgs e) {
			int result = network_try_connect(
				combobox_network_interfaces,
				combobox_network_access_points
			);

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

		private void rect_console_MouseDown(object sender, MouseButtonEventArgs e) {
			tab_switch("console");
		}
	}
}
