# Network Sledgehammer
*Because Wi-Fi should just f%@#ing work*

## Synopsis
**Network Sledgehammer** is a networking tool which automates reconnecting to a
Wi-Fi access point upon detecting a disconnection. It's a **last resort**
application for when you have no other choice (Bad network adapter, no
up-to-date drivers, etc) and need a nearly constant Internet connection.

The application is intuitive to use. Just select a Wi-Fi network, an address to
try to ping (e.g. `www.google.com`), and a frequency to ping. Let it run in
the background. Easy.

## Compilation Instruction
This project requires the **managedwifi** package. Open the project in Visual
Studio, then go to **Tools** -\> **NuGet Package Manager** -\> **Package
Manager Console**. Type in the following command:
```
Install-Package managedwifi
```
After that, simply click "Run" and you're good to go.
