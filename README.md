# Network Sledgehammer
*Because Wi-Fi should just f%@#ing work*

## Synopsis
**Network Sledgehammer** is a networking tool which automates reconnecting to a
Wi-Fi access point upon detecting a disconnection. It's a **last resort**
application for when you have no other choice (Bad network adapter, no
up-to-date drivers, etc) and need a nearly constant Internet connection.

The application is intuitive to use. Just select a Wi-Fi network, an address to
try to ping (e.g. `https://www.google.com`), and a frequency to ping. Let it
run in the background. Easy.

## Compilation Instruction
This project uses the **Costura.Fody** package to embed resources into the
final executable (like `ManagedWifi.dll`...). To install this, Open the project
in Visual Studio, then go to **Tools** -\> **NuGet Package Manager** -\>
**Package Manager Console**. Type in the following command:
```
Install-Package Costura.Fody
```
Afterwards, just compile the project and it'll run. Easy.

## External Dependencies (ManagedWifi)
This project includes the source code of **managedwifi**, as the NuGet version
has a bug that crashes the program upon connection to an access point. This
means you don't need to add an additional NuGet package to the project to have
it compiled. Just click "Run" and you're good to go. License JSON is available
in the `ManagedWifi` directory straight from the source code release.
