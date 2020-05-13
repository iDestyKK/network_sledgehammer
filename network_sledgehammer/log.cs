﻿/*
 * Logger
 * 
 * Description:
 *     Very simple static class for handling proper logging via a textbox in
 *     WPF. This is so we don't need to keep passing dispatcher and WPF objects
 *     to other classes... annoyingly.
 *     
 * Author:
 *     Clara Nguyen (@iDestyKK)
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;

namespace network_sledgehammer {
	static class log {
		private static TextBox    textbox_log;
		private static Dispatcher dis_obj; //lol oh my god

		static log() {
			textbox_log = null;
			dis_obj     = null;
		}

		/*
		 * setup_textbox
		 * 
		 * Sets up the WPF textbox for logging.
		 */

		public static void setup_textbox(TextBox tb) {
			textbox_log = tb;
		}

		/*
		 * setup_dispatcher
		 * 
		 * Sets up the dispatcher so we know which thread the textbox belongs
		 * to. This is mainly so VS would shut the hell up about writing to the
		 * textbox...
		 */

		public static void setup_dispatcher(Dispatcher d) {
			dis_obj = d;
		}

		/*
		 * write
		 * 
		 * Logs data to "textbox_log" if it's setup. A newline isn't printed in
		 * addition to the "line" and must be added manually.
		 */

		public static void write(string line) {
			//Skip if not set
			if (textbox_log == null || dis_obj == null)
				return;

			//fight some stupid race condition i guess
			dis_obj.Invoke(() => {
				textbox_log.Text += line;
				textbox_log.ScrollToEnd();
			});
		}
	}
}
