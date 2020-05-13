/*
 * Configuration Helper
 * 
 * Description:
 *     Handles loading configuration files (config.txt). This is a static
 *     class, as configuration is global and affects all portions of the
 *     program.
 *     
 * Author:
 *     Clara Nguyen (@iDestyKK)
 */

using System;
using System.IO;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace network_sledgehammer {
	static class config {
		public static string ping_url      { get; set; }
		public static int    attempt_delay { get; set; }
		public static int    attempts      { get; set; }

		public static TextBox textbox_ping_url { get; set; }
		public static TextBox textbox_delay    { get; set; }
		public static TextBox textbox_attempts { get; set; }

		/*
		 * create_default_config
		 * 
		 * Creates a default valid configuration file for use by the program.
		 * Returns "true" upon success. Returns "false" otherwise.
		 */

		private static bool create_default_config(string path) {
			try {
				StreamWriter fp = new StreamWriter(path);
				fp.WriteLine("https://www.google.com");
				fp.WriteLine(2000);
				fp.WriteLine(3);
				fp.Close();
			}
			catch (Exception e) {
				/* a VERY bad idea */
				return false;
			}

			return true;
		}

		/*
		 * is_valid
		 * 
		 * Checks if config is valid or not. Ping URL must be a string. Delay
		 * must be an integer above 0. Attempts must be an integer at or above
		 * 0. Returns an integer based on what's wrong in the form of a
		 * bitmask. If nothing is wrong, 0 is returned.
		 * 
		 * Returns Flags:
		 *   1 - Ping URL is invalid.
		 *   2 - Delay is invalid.
		 *   4 - Attempts is invalid.
		 */

		public static int is_valid() {
			int mask = 0;
			int tmp;

			//Invalid ping URL (blank string)
			if (textbox_ping_url.Text == "")
				mask |= 0b001;

			//Invalid Delay (not int or <= 0)
			if (!int.TryParse(textbox_delay.Text, out tmp) || tmp <= 0)
				mask |= 0b010;

			//Invalid Attempts (not int or < 0)
			if (!int.TryParse(textbox_attempts.Text, out tmp) || tmp < 0)
				mask |= 0b100;

			//We're done here.
			return mask;
		}

		/*
		 * error_string
		 * 
		 * When given an error mask, returns a string with errors to print.
		 */

		public static string error_string(int mask) {
			string res = "";

			if ((mask & 0b001) > 0)
				res += ((res != "") ? "\n" : "") + " - Ping URL is invalid";

			if ((mask & 0b010) > 0)
				res += ((res != "") ? "\n" : "") + " - Delay is invalid";

			if ((mask & 0b100) > 0)
				res += ((res != "") ? "\n" : "") + " - Attempts is invalid";

			return res;
		}

		/*
		 * load_config
		 * 
		 * Loads a configuration file at "path". The format of the file is very
		 * simple... just 3 lines of text. We don't need anything complicated.
		 */

		public static bool load_config(string path) {
			string line;
			int mask;
			StreamReader fp;

			if (!File.Exists(path)) {
				create_default_config(path);
			}

			try {
				fp = new StreamReader(path);
				line = fp.ReadLine(); textbox_ping_url.Text = line;
				line = fp.ReadLine(); textbox_delay   .Text = line;
				line = fp.ReadLine(); textbox_attempts.Text = line;
				fp.Close();
			}
			catch (Exception e) {
				/* a VERY bad idea */
				return false;
			}

			/*
			 * If invalid data was read in, force the default settings to
			 * override and load that instead.
			 */

			mask = is_valid();

			if (mask != 0) {
				create_default_config(path);
				load_config(path);

				MessageBox.Show(
					"Configuration file has errors:\n" + error_string(mask) +
					"\n\nThe default configuration has been loaded instead.",
					"Errors found",
					MessageBoxButton.OK,
					MessageBoxImage.Error
				);
			}

			//Assume it worked tbh.
			ping_url      = textbox_ping_url.Text;
			attempt_delay = Convert.ToInt32(textbox_delay.Text);
			attempts      = Convert.ToInt32(textbox_attempts.Text);

			return true;
		}

		/*
		 * save_config
		 * 
		 * Saves configuration to "path".
		 */

		public static bool save_config(string path) {
			int mask;

			//Check if data is valid.
			mask = is_valid();
			if (mask > 0) {
				MessageBox.Show(
					"The following errors occurred:\n" + error_string(mask),
					"Errors found",
					MessageBoxButton.OK,
					MessageBoxImage.Error
				);
				return false;
			}

			try {
				StreamWriter fp = new StreamWriter(path);
				fp.WriteLine(textbox_ping_url.Text);
				fp.WriteLine(textbox_delay.Text);
				fp.WriteLine(textbox_attempts.Text);
				fp.Close();

				MessageBox.Show(
					"Configuration saved successfully!",
					"Success",
					MessageBoxButton.OK,
					MessageBoxImage.Information
				);
			}
			catch (Exception e) {
				/* a VERY bad idea */
				return false;
			}

			//Set the variables as well.
			ping_url = textbox_ping_url.Text;
			attempt_delay = Convert.ToInt32(textbox_delay.Text);
			attempts = Convert.ToInt32(textbox_attempts.Text);

			return true;
		}
	}
}
