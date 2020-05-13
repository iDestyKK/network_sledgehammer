/*
 * Sledgehammer
 * 
 * Description:
 *     Functions dedicated to the sledgehammer functionality of the program,
 *     including the thread function and network helpers required to run it.
 *     
 * Author:
 *     Clara Nguyen (@iDestyKK)
 */

using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;
using Network_Sledgehammer;

namespace network_sledgehammer {
    class sledgehammer {
		/*
		 * ping
		 * 
		 * Performs a ping with a timeout of 1 second.
		 */

		private bool ping(string url, int timeout) {
			try {
				HttpWebRequest request =
					(HttpWebRequest)HttpWebRequest.Create(url);

				request.Timeout = timeout;
				request.AllowAutoRedirect = true;
				request.Method = "HEAD";

				using (var response = request.GetResponse()) {
					return true;
				}
			}
			catch {
				return false;
			}
		}

		/*
		 * thread
		 * 
		 * Thread that handles pinging and checking connections.
		 */

		public void thread(net_func net_util, int i_id, int ap_id) {
			int i;
			bool res;

			log.write("Starting sledgehammer...\n");

			while (true) {
				for (i = 0; i < config.attempts; i++) {
					//Let's see here...
					log.write("Pinging " + config.ping_url + "... ");

					res = ping(config.ping_url, 1000);

					log.write(res ? "Success\n" : "Failure\n");

					if (!res) {
						//If last try, force a reconnection.
						if (i == config.attempts - 1)
							net_util.try_naive_connect(i_id, ap_id);
					}
					else
						break;

					Thread.Sleep(config.attempt_delay);
				}

				Thread.Sleep(config.attempt_delay);
			}

			//This should never happen
			log.write("Sledgehammer ended...\n");
		}
	}
}
