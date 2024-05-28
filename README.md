# virtuoso-midi-companion
An open-source companion application for [Virtuoso VR](https://www.virtuoso-vr.com/remote-control/) that listens to Virtuoso Remote Control messages and converts them to MIDI using a virtual MIDI port.

Building the project using the supplied build.bat requires the [.NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

The companion application will automatically prompt the user to download and install the [loopBe1 Virtual MIDI Port](https://www.nerds.de/en/loopbe1.html). If you'd like to build the application for macOS, you can rewrite it to instead use the built in virtual MIDI ports that macOS can create. Feel free to fork the project or create a pull request if you'd like to add macOS support!

## How to use
1) Make sure the device running Virtuoso and the computer running your music program are on the same local network, or is the same computer.

2) Download a build and run it on the computer that has your music program installed. 

3) If you get a warning saying that Windows has protected your machine from an unknown app click More info then Run anyway.

4) Follow the instructions in the companion app to install and run LoopBe1 virtual MIDI port.

5) Restart the companion app and make sure you allow it to access the network if you get a firewall warning.

6) Start your music program and enable LoopBe1 as the input MIDI port for the sampler or synthesizer plug-in you want to control.

7) Start Virtuoso, go to Track Settings and click "Enable Remote Control".

8) If you're running Virtuoso on a Windows computer, you may need to allow access to network again if you get a firewall warning.

9) The companion app should now report that Virtuoso has connected to it. If not, double-check that Virtuoso and the companion app are running on the same local network and that there are no firewall limitations for the programs.

10) Play on an instrument in Virtuoso. The sampler or synthesizer plug-in in your music program should now play. If not, check that the LoopBe1 MIDI port is properly enabled and connected in your music program, and that sounds from the plug-in can be heard if you trigger them directly from the music program.

### Known bugs and limitations
1) The companion app is currently Windows only, but may be ported to macOS using the source code.

2) The Microphone, Looper and Tape Recorder cannot be used while running Remote Control, since they rely on internally generated audio.

3) By default, the Wavemin will not pitch sounds up and down, since pitch shift is limited to a few semitones in most plug-ins. It does however send parameters mapped to all axes, and depending on your music program and plug-in you can connect the corresponding MIDI CC to effects including tuning and pitch.

For more help or to participate in creating Virtuoso Remote Control applications, please visit [our Discord!](https://discord.gg/virtuoso)
