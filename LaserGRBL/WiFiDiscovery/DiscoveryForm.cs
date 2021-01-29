﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LaserGRBL.WiFiDiscovery
{
	public partial class DiscoveryForm : Form
	{
		String RV;

		Task T;
		CancellationTokenSource C;

		public DiscoveryForm()
		{
			InitializeComponent();

			ComWrapper.WrapperType currentWrapper = Settings.GetObject("ComWrapper Protocol", ComWrapper.WrapperType.UsbSerial);
			if (currentWrapper == ComWrapper.WrapperType.Telnet)
				UdPort.Value = 23;
			else if (currentWrapper == ComWrapper.WrapperType.LaserWebESP8266)
				UdPort.Value = 81;
			else
				UdPort.Value = 666;

		}

		private void BtnScan_Click(object sender, EventArgs e)
		{
			StartScan();
		}

		private void StartScan()
		{
			if (T == null)
			{
				Cursor = Cursors.WaitCursor;
				LV.Items.Clear();
				BtnScan.Visible = false;
				BtnStop.Visible = true;
				BtnStop.Enabled = true;
				UdPort.Enabled = false;

				T = Task.Factory.StartNew(() => { ScanAsync(); });
			}
		}

		private void ScanAsync()
		{
			C = new CancellationTokenSource();
			IPAddressHelper.ScanIP(ScanOutput, ScanProgress, C.Token, (int)UdPort.Value); //bloccante
			OnScanEnd();
		}

		void OnScanEnd()
		{
			if (InvokeRequired)
			{
				BeginInvoke((MethodInvoker)(() => { OnScanEnd(); }));
			}
			else
			{
				if (C.IsCancellationRequested)
					LblProgress.Text = "The scan was aborted";
				else
					LblProgress.Text = "The scan was finished";

				BtnScan.Visible = true;
				BtnStop.Visible = false;
				BtnStop.Enabled = true;
				UdPort.Enabled = true;

				T = null;
				C = null;

				Cursor = Cursors.Default;
			}
		}

		void ScanOutput(IPAddress ip, IPAddressHelper.ScanResult result)
		{
			if (InvokeRequired)
			{
				BeginInvoke((MethodInvoker)(() => { ScanOutput(ip, result); }));
			}
			else
			{
				ListViewItem LVA = new ListViewItem(new string[] { ip.ToString(), result.MAC, result.Ping, result.Telnet });
				LVA.Tag = result;
				LV.Items.Add(LVA);
			}
		}

		void ScanProgress(int count, int total)
		{
			if (InvokeRequired)
			{
				BeginInvoke((MethodInvoker)(() => { ScanProgress(count, total); }));
			}
			else
			{
				LblProgress.Text = $"Progress: {count}/{total}";
			}
		}

		private void BtnStop_Click(object sender, EventArgs e)
		{
			LblProgress.Text = "Abort scan...";
			StopScan();
		}

		private void StopScan()
		{
			try
			{
				BtnStop.Enabled = false;
				Cursor = Cursors.WaitCursor;
				C?.Cancel();
			}
			catch { }
		}

		internal static string CreateAndShowDialog(Form parent)
		{
			String RV;
			using (DiscoveryForm F = new DiscoveryForm())
			{
				F.ShowDialog(parent);
				RV = F.RV;
			}
			return RV;
		}

		private void LV_SelectedIndexChanged(object sender, EventArgs e)
		{
			BtnConnect.Enabled = LV.SelectedItems.Count == 1;
		}

		private void BtnConnect_Click(object sender, EventArgs e)
		{
			if (LV.SelectedItems.Count == 1)
				ReturnItem(LV.SelectedItems[0].Tag as IPAddressHelper.ScanResult);
		}

		private void ReturnItem(IPAddressHelper.ScanResult result)
		{
			if (result != null)
			{
				ComWrapper.WrapperType currentWrapper = Settings.GetObject("ComWrapper Protocol", ComWrapper.WrapperType.UsbSerial);
				if (currentWrapper == ComWrapper.WrapperType.Telnet)
					RV = $"{result.IP}:{result.Port}";
				else if (currentWrapper == ComWrapper.WrapperType.LaserWebESP8266)
					RV = $"ws://{result.IP}:{result.Port}/";
				Close();
			}
		}

		private void DiscoveryForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			StopScan();
		}

		private void UdPort_ValueChanged(object sender, EventArgs e)
		{
			ChConnection.Text = $"Connection (Port {UdPort.Value})";
		}

		private void LV_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			ListView senderList = (ListView)sender;
			ListViewItem clickedItem = senderList.HitTest(e.Location).Item;
			if (clickedItem != null)
				ReturnItem(clickedItem.Tag as IPAddressHelper.ScanResult);
		}
	}
}
