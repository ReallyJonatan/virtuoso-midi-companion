# virtuoso-midi-companion
An open-source companion application for [Virtuoso VR](https://www.virtuoso-vr.com/remote-control/) that listens to Virtuoso Remote Control messages and converts them to MIDI using a virtual MIDI port.

Building the project using the supplied build.bat requires the [.NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

The companion application will automatically prompt the user to download and install the [loopBe1 Virtual MIDI Port](https://www.nerds.de/en/loopbe1.html). If you'd like to build the application for macOS, you can rewrite it to instead use the built in virtual MIDI ports that macOS can create. Feel free to fork the project or create a pull request if you'd like to add macOS support!
