﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Configuration;
using OscJack;
using Sanford.Multimedia.Midi;

namespace VirtuosoMIDICompanion {
    class Program {
        public class Settings {
            public int MaxParameterMessageRate { get; set; } = 300;
            public bool RemapParameters { get; set; } = false;
            public bool EnableAdditionalLogging { get; set; } = false;
        }

        private const int ApiVersion = 0;

        private static UdpClient _udpClient;
        private static int _broadcastPort = 9002;
        private static OscServer _oscServer;
        private static OutputDevice _outputDevice;
        private static ChannelMessageBuilder _channelMessageBuilder;
        private static Timer _broadcastTimer;

        private static int _receivedMessagesSinceLastMeasurement;
        private static float _messageRate;
        private static float _passRate = 1.0f;
        private static DateTime _lastMeasurement;
        private static Random _random = new Random();

        private static bool _warnedAboutIncorrectFormatMessage = false;

        private const int AddressParts = 3;
        private const int DataParts = 3;

        private static Settings _settings;
        private static Dictionary<int, int> _parameterRemaps = new Dictionary<int, int>();
        private static List<int> _invertParameters = new List<int>();

        static void Main(string[] args) {
            _lastMeasurement = DateTime.Now;

            Console.WriteLine("---- Virtuoso MIDI Companion App ----");
            Console.WriteLine("- Press any key at any time to quit -");
            
            ReadConfigurationFile();

            if (NetworkInterface.GetIsNetworkAvailable()) {
                if (!SetupVirtualMidiPort()){
                    DisposeAll();
                    return;
                }
                StartOscServer();
                StartBroadcastingIP();
                Console.WriteLine("Ready. Waiting for remote messages from Virtuoso.");
            }
            else {
                Console.WriteLine("No network connection detected. Please check your connection and restart program.");
            }
            Console.ReadKey();
            DisposeAll();
        }

        private static void DisposeAll() {
            _oscServer?.Dispose();
            _udpClient?.Dispose();
            _outputDevice?.Dispose();
        }

        private static void ReadConfigurationFile() {
            IConfiguration config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddIniFile("VirtuosoMIDICompanion.ini", optional: true, reloadOnChange: true)
                .Build();

            var parameterRemapSection = config.GetSection("ParameterRemapping");
            foreach (var remap in parameterRemapSection.GetChildren()) {
                if (int.TryParse(remap.Key, out int key) && int.TryParse(remap.Value, out int value)) {
                    _parameterRemaps.Add(key, value);
                }
            }

            var invertParameterSection = config.GetSection("InvertParameters");
            var values = invertParameterSection["values"];
            if (values != null) {
                foreach (var invert in values.Split(',')) {
                    if (int.TryParse(invert.Trim(), out int value)) {
                        _invertParameters.Add(value);
                    }
                }
            }

            _settings = new Settings();
            config.GetSection("Settings").Bind(_settings);

            if (_settings.EnableAdditionalLogging) {
                if (_parameterRemaps.Count > 0) {
                    Console.WriteLine("Parameter remapping enabled:");
                    foreach (var remap in _parameterRemaps) {
                        Console.WriteLine($"  {remap.Key} -> {remap.Value}");
                    }
                }
            
                if (_settings.MaxParameterMessageRate > 0) {
                    Console.WriteLine("Max parameter rate: " + _settings.MaxParameterMessageRate);
                }
            }
        }

        private static void StartOscServer() {
            _oscServer = new OscServer(9003);
            _oscServer.MessageDispatcher.AddCallback("", OnOscMessageReceived);
        }

        private static void OnOscMessageReceived(string oscAddress, OscDataHandle oscData) {
            string[] splitOscAddress = oscAddress.TrimStart('/').Split("/");
            if (splitOscAddress.Length != AddressParts || oscData.GetElementCount() != DataParts) {
                if (!_warnedAboutIncorrectFormatMessage) {
                    Console.WriteLine($"Received message with incorrect format ({splitOscAddress.Length}, {oscData.GetElementCount()}). Make sure you have the latest version of Virtuoso and this app!");
                    _warnedAboutIncorrectFormatMessage = true;
                }
                return;
            }
            
            switch (splitOscAddress[2]) {
                case "connect":
                    Console.WriteLine("Virtuoso sent connection message.");
                    if (oscData.GetElementAsInt(1) > ApiVersion) {
                        Console.WriteLine("Virtuoso's Remote API version is newer than this app, and it may not work as intended.");
                        Console.WriteLine("You can find the latest supported companions at https://virtuoso-vr.com/remote-control/");
                    }
                    break;
                case "disconnect":
                    Console.WriteLine("Virtuoso sent disconnection message.");
                    break;
                case "noteon":
                case "noteoff":
                case "parameter":
                case "volume":
                    BuildAndSendMidiMessage(splitOscAddress, oscData);
                    break;
                default:
                    // Message not implemented
                    if (_settings.EnableAdditionalLogging) {
                        Console.WriteLine($"Message not implemented: {oscAddress}");
                    }
                    return;
            }
        }

        private static void StartBroadcastingIP() {
            _udpClient = new UdpClient();
            Console.WriteLine("Starting broadcast of companion app local IP: " + GetLocalIPAddress());
            _broadcastTimer = new System.Threading.Timer(
                e => SendBroadcast(),
                null,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(3));
        }

        private static string GetLocalIPAddress() {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList) {
                if (ip.AddressFamily == AddressFamily.InterNetwork) {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        private static void SendBroadcast() {
            string ipString = "VirtuosoCompanion:" + GetLocalIPAddress();
            var data = Encoding.UTF8.GetBytes(ipString);
            _udpClient.Send(data, data.Length, "255.255.255.255", _broadcastPort);
        }

        private static bool SetupVirtualMidiPort() {
            // TODO: Port for macOS using their built in virtual MIDI device capability
            for(int i = 0; i < OutputDevice.DeviceCount; i++) {
                if(OutputDevice.GetDeviceCapabilities(i).name == "LoopBe Internal MIDI") {
                    Console.WriteLine($"Found virtual midi port: {OutputDevice.GetDeviceCapabilities(i).name}. Using as target.");
                    _outputDevice = new OutputDevice(i);
                    _outputDevice.Reset();
                    _channelMessageBuilder = new ChannelMessageBuilder();
                }
            }
            if (_outputDevice != null) {
                return true;
            }
            else {
                Console.WriteLine("To use this app, LoopBe1 Virtual MIDI device must be installed and enabled.");
                Console.WriteLine("Press any key to quit and proceed to file download.");
                Console.ReadKey();
                Process.Start(new ProcessStartInfo("https://www.nerds.de/data/setuploopbe1.exe") { UseShellExecute = true });
                return false;
            }
        }

        private static void BuildAndSendMidiMessage(string[] splitOscAddress, OscDataHandle oscData) {
            _receivedMessagesSinceLastMeasurement++;
            float fValue = oscData.GetElementAsFloat(0);
            int value = Math.Clamp((int)(fValue * 128), 0, 127);
            int channel = Math.Clamp(oscData.GetElementAsInt(1) - 1, 0, 15);
            int noteOrParameterNumber = Math.Clamp(oscData.GetElementAsInt(2), 0, 127);

            switch (splitOscAddress[2]) {
                case "noteon":
                    if (_settings.EnableAdditionalLogging) {
                        Console.WriteLine($"Note on: {noteOrParameterNumber} on channel {channel} with velocity {value}");
                    }
                    _channelMessageBuilder.Command = ChannelCommand.NoteOn;
                    break;
                case "noteoff":
                    if (_settings.EnableAdditionalLogging) {
                        Console.WriteLine($"Note off: {noteOrParameterNumber} on channel {channel}");
                    }
                    value = 0;
                    _channelMessageBuilder.Command = ChannelCommand.NoteOff;
                    break;
                case "parameter":
                    if (ShouldDropToLimitMessageRate()) {
                        return;
                    }
                    if (_settings.RemapParameters && _parameterRemaps.ContainsKey(noteOrParameterNumber)) {
                        noteOrParameterNumber = _parameterRemaps[noteOrParameterNumber];
                    }
                    if (_invertParameters.Contains(noteOrParameterNumber)) {
                        value = 127 - value;
                    }
                    if (noteOrParameterNumber == 128) { // Remapped to pitch wheel
                        noteOrParameterNumber = 0; // Pitch wheel has no CC number
                        _channelMessageBuilder.Command = ChannelCommand.PitchWheel;
                    } 
                    else {
                        _channelMessageBuilder.Command = ChannelCommand.Controller;
                    }
                    break;
                case "volume":
                    if (ShouldDropToLimitMessageRate()) {
                        return;
                    }
                    noteOrParameterNumber = 7; // Volume is always CC 7
                    _channelMessageBuilder.Command = ChannelCommand.Controller;
                    break;
                default:
                    break;
            }
            _channelMessageBuilder.MidiChannel = channel;
            _channelMessageBuilder.Data1 = noteOrParameterNumber;
            _channelMessageBuilder.Data2 = value;
            _channelMessageBuilder.Build();
            _outputDevice.Send(_channelMessageBuilder.Result);
        }

        private static bool ShouldDropToLimitMessageRate() {
            if (_settings.MaxParameterMessageRate > 0 || _settings.EnableAdditionalLogging) {
                DateTime now = DateTime.Now;
                TimeSpan span = now.Subtract(_lastMeasurement);
                double measureInterval = 0.10;
                float safetyFactor = 0.5f;
                if (span.TotalSeconds > measureInterval || (_settings.MaxParameterMessageRate > 0 && _receivedMessagesSinceLastMeasurement > _settings.MaxParameterMessageRate)) {
                    _lastMeasurement = now;
                    _messageRate = _receivedMessagesSinceLastMeasurement / (float) span.TotalSeconds;
                    if (_settings.EnableAdditionalLogging) {
                        Console.WriteLine($"Messages per second: {_messageRate}");
                    }
                    _receivedMessagesSinceLastMeasurement = 0;
                }
                if (_settings.MaxParameterMessageRate > 0) {
                    if (_messageRate > _settings.MaxParameterMessageRate) {
                        float overloadFactor = (float)_messageRate / _settings.MaxParameterMessageRate;
                        _passRate = Math.Clamp(1.0f / overloadFactor * safetyFactor, 0.0f, 1.0f);
                    } else {
                        _passRate = 1.0f;
                    }
                    if (_random.NextSingle() > _passRate) {
                        if (_settings.EnableAdditionalLogging) {
                            Console.WriteLine("Dropped message to limit message rate.");
                        }
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
