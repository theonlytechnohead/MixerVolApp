﻿using System;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows;
using CoreAudio;

namespace MixerVolApp {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {

		public static MainWindow instance;

		private class RenderDevice {
			public readonly string Name;
			public readonly MMDevice Device;

			public RenderDevice (MMDevice device) {
				Device = device;
				Name = $"{device.Properties[PKEY.PKEY_Device_DeviceDesc].Value} ({device.FriendlyName})";
			}

			public override string ToString () {
				return Name;
			}
		}

		ObservableCollection<RenderDevice> devices = new ObservableCollection<RenderDevice>();
		MMDevice selectedDevice;
		AudioSessionManager2 audioSessionManager2;

		public MainWindow () {
			InitializeComponent();

			instance = this;

			deviceBox.DataContext = this;
			deviceBox.ItemsSource = devices;
			deviceBox.SelectionChanged += (_, __) => EnumerateSessions();

			ListDevices();
		}

		private void ListDevices () {
			MMDeviceEnumerator deviceEnumerator = new MMDeviceEnumerator();
			MMDevice defaultDevice = deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia);
			audioSessionManager2 = defaultDevice.AudioSessionManager2;
			MMDeviceCollection devCol = deviceEnumerator.EnumerateAudioEndPoints(EDataFlow.eRender, DEVICE_STATE.DEVICE_STATE_ACTIVE);
			for (int i = 0; i < devCol.Count; i++) {
				devices.Add(new RenderDevice(devCol[i]));
				if (devCol[i].FriendlyName == defaultDevice.FriendlyName) {
					deviceBox.SelectedIndex = i;
				}
			}
		}

		private void EnumerateSessions () {
			selectedDevice = ((RenderDevice)deviceBox.SelectedItem).Device;
			audioSessionManager2.OnSessionCreated -= OnSessionCreated;
			audioSessionManager2 = selectedDevice.AudioSessionManager2;
			audioSessionManager2.OnSessionCreated += OnSessionCreated;
			audioSessionManager2.RefreshSessions();
			SessionCollection sessions = audioSessionManager2.Sessions;

			sessionControlStackPanel.Children.Clear();

			foreach (AudioSessionControl2 session in sessions) {
				if (session.State != AudioSessionState.AudioSessionStateExpired) {
					SessionUI sessionUI = new SessionUI();
					sessionUI.SetSession(session);
					sessionControlStackPanel.Children.Add(sessionUI);
				}
			}
		}

		private void OnSessionCreated (object sender, CoreAudio.Interfaces.IAudioSessionControl2 newSession) {
			Dispatcher.Invoke(() => {
				SessionUI sessionUI = new SessionUI();
				ConstructorInfo contructor = typeof(AudioSessionControl2).GetConstructors(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)[0];
				AudioSessionControl2 session = contructor.Invoke(new object[] { newSession }) as AudioSessionControl2;
				sessionUI.SetSession(session);
				sessionControlStackPanel.Children.Add(sessionUI);
			});
		}
	}
}
