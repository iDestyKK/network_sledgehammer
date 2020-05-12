/*
 * Network Functionality Helper
 * 
 * Description:
 *     Supplies functions for interfacing with the network.
 *     
 * Author:
 *     Clara Nguyen (@iDestyKK)
 */

using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NativeWifi;

namespace network_sledgehammer {
    class net_func {
		//Data Structures
		private Wlan.WlanAvailableNetwork[] networks;
		private WlanClient client;
		private WlanClient.WlanInterface[] interfaces;

		//WPF Elements
		private ComboBox cb_interfaces;
		private ComboBox cb_access_points;

		//Cache existing profile information
		private Dictionary<
			WlanClient.WlanInterface, Dictionary<string, string>> interface_profile;

		private static string GetStringForSSID(Wlan.Dot11Ssid ssid) {
			return Encoding.ASCII.GetString(ssid.SSID, 0, (int)ssid.SSIDLength);
		}

		/* constructor */
		public net_func() {
			//Default values
			cb_interfaces = null;
			cb_access_points = null;

			client = new WlanClient();

			interface_profile = new Dictionary<
				WlanClient.WlanInterface, Dictionary<string, string>>();
		}

		/*
		 * setup_interface_combobox
		 * 
		 * Tells the class which combobox has the interfaces.
		 */

		public void setup_interface_combobox(ComboBox cb) {
			cb_interfaces = cb;
		}

		/*
		 * setup_access_point_combobox
		 * 
		 * Tells the class which combobox has the access points.
		 */

		public void setup_access_point_combobox(ComboBox cb) {
			cb_access_points = cb;
		}

		/*
		 * update_adapter
		 * 
		 * Updates network adapter data structures to hold recent information.
		 */

		public void update_adapter() {
			//Wipe out "cb"'s information
			cb_interfaces.Items.Clear();
			cb_interfaces.SelectedItem = null;

			//Since "cb" was cleared out, "cb_ap" also needs to be cleared out.
			cb_access_points.Items.Clear();
			cb_access_points.SelectedItem = null;

			//Clear out data structures as well
			interface_profile.Clear();

			interfaces = client.Interfaces;

			foreach (WlanClient.WlanInterface mw_inter in interfaces) {
				cb_interfaces.Items.Add(mw_inter.InterfaceName);

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
		 * update_access_point
		 * 
		 * Updates access points available in the access point combobox. This
		 * requires information from the Adapter Combobox "cb" to modify the
		 * Access Point Combobox "cb_ap".
		 */

		public void update_access_point() {
			//Invalid/none item being selected
			if (cb_interfaces.SelectedIndex == -1)
				return;

			//Wipe out "cb_ap"'s information
			cb_access_points.Items.Clear();
			cb_access_points.SelectedItem = null;

			//Ok, get all access points and populate "cb_ap".
			interfaces[cb_interfaces.SelectedIndex].Scan();
			networks = interfaces[cb_interfaces.SelectedIndex].GetAvailableNetworkList(0);

			foreach (Wlan.WlanAvailableNetwork network in networks)
				cb_access_points.Items.Add(GetStringForSSID(network.dot11Ssid));
		}

		/*
		 * try_connect
		 * 
		 * Try to connect to a Wifi Access Point. This relies on the interface
		 * specified in "cb" as well as the access point specified in "cb_ap".
		 * If they are not specified properly or the Wi-Fi profile does not
		 * exist, the command will fail with an appropriate error code
		 * returned.
		 */

		public int try_connect() {
			WlanClient.WlanInterface mw_inter;
			Wlan.WlanAvailableNetwork network;
			string ssid, prof_name, prof_xml;

			//Invalid adapter
			if (cb_interfaces.SelectedIndex == -1)
				return 1;

			//Invalid access point
			if (cb_access_points.SelectedIndex == -1)
				return 2;

			mw_inter = interfaces[cb_interfaces   .SelectedIndex];
			network  = networks  [cb_access_points.SelectedIndex];
			ssid = GetStringForSSID(network.dot11Ssid);

			//Profile doesn't exist
			if (!interface_profile[mw_inter].ContainsKey(ssid))
				return 3;

			//Ok, time to connect
			prof_name = ssid;
			prof_xml = interface_profile[mw_inter][ssid];

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
	}
}
