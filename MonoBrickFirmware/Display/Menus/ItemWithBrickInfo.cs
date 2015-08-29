﻿using System;
using System.Reflection;
using MonoBrickFirmware.FirmwareUpdate;
using MonoBrickFirmware.Connections;
using MonoBrickFirmware.Display.Dialogs;

namespace MonoBrickFirmware.Display.Menus
{
	public class ItemWithBrickInfo : IChildItem, IParentItem
	{
		private bool hasFocus = false;
		private ItemWithDialog<ProgressDialog> dialog;
		private Information information = null;

		public ItemWithBrickInfo ()
		{
			dialog = new ItemWithDialog<ProgressDialog>(new ProgressDialog("Getting Info", new StepContainer(LoadSettings, "Loading", "Failed to load info")));
		}

		public void OnEnterPressed ()
		{
			if (!hasFocus) 
			{
				if (information == null) 
				{
					dialog.SetFocus (this,OnDialogExit );
				} 
				else 
				{
					Parent.SetFocus (this);
					hasFocus = true;
				}
			} 
			else 
			{
				hasFocus = false;
				Parent.RemoveFocus (this);
			}
		}

		public void OnLeftPressed ()
		{
			
		}

		public void OnRightPressed ()
		{
			
		}

		public void OnUpPressed ()
		{
			
		}

		public void OnDownPressed ()
		{
			
		}

		public void OnEscPressed ()
		{
			if (hasFocus) 
			{
				Parent.RemoveFocus (this);
				hasFocus = false;
			}
		}

		public void OnDrawTitle (Font font, Rectangle rectangle, bool selected)
		{
			Lcd.WriteTextBox(font, rectangle, "Brick information", selected);	
		}

		public void OnDrawContent ()
		{
			string monoVersion = "Unknown";
			Type type = Type.GetType("Mono.Runtime");
			if (type != null)
			{                                          
				MethodInfo displayName = type.GetMethod("GetDisplayName", BindingFlags.NonPublic | BindingFlags.Static); 
				if (displayName != null)                   
					monoVersion = (string)displayName.Invoke(null, null); 
			}	
			string monoCLR = System.Reflection.Assembly.GetExecutingAssembly().ImageRuntimeVersion;
			var currentVersion = UpdateHelper.InstalledVersion ();

			Point offset = new Point(0, (int)Font.MediumFont.maxHeight);
			Point startPos = new Point(0,0);
			Lcd.Clear();
			Lcd.WriteText(Font.MediumFont, startPos+offset*0, "Firmware: " + currentVersion.Firmware, true);
			Lcd.WriteText(Font.MediumFont, startPos+offset*1, "Image: " + currentVersion.Image , true);
			Lcd.WriteText(Font.MediumFont, startPos+offset*2, "Mono version: " + monoVersion.Substring(0,7), true);
			Lcd.WriteText(Font.MediumFont, startPos+offset*3, "Mono CLR: " + monoCLR, true);			
			Lcd.WriteText(Font.MediumFont, startPos+offset*4, "IP: " + WiFiDevice.GetIpAddress(), true);			
			Lcd.Update();
		}

		public void SetFocus (IChildItem item)
		{
			Parent.SetFocus (item);	
		}

		private void OnDialogExit(ProgressDialog dialog)
		{
			if (dialog.Ok) 
			{
				Parent.SetFocus (this);
				hasFocus = true;
			} 
			else 
			{
				Parent.RemoveFocus (this);
			}
		}
	
		public void RemoveFocus (IChildItem item)
		{
			
		}

		public void SuspendButtonEvents ()
		{
			Parent.SuspendButtonEvents ();
		}

		public void ResumeButtonEvents ()
		{
			Parent.ResumeButtonEvents ();
		}

		public void OnHideContent ()
		{
			
		}

		private void OnInformationLoaded(Information info)
		{
			information = info;
		}

		private bool LoadSettings()
		{
			bool ok = true;
			try
			{
				string monoVersion = "Unknown";
				Type type = Type.GetType("Mono.Runtime");
				if (type != null)
				{                                          
					MethodInfo displayName = type.GetMethod("GetDisplayName", BindingFlags.NonPublic | BindingFlags.Static); 
					if (displayName != null)                   
						monoVersion = (string)displayName.Invoke(null, null); 
				}	
				string monoCLR = System.Reflection.Assembly.GetExecutingAssembly().ImageRuntimeVersion;
				var currentVersion = UpdateHelper.InstalledVersion ();
				string ip = WiFiDevice.GetIpAddress();
				information = new Information();
				information.FirmwareVersion = currentVersion.Firmware;
				information.ImageVersion = currentVersion.Image;
				information.IpAddress = ip;
				information.MonoCLRVersion = monoCLR;
				information.MonoVersion = monoVersion;
			}
			catch
			{
				ok = false;
			}
			return ok;
		}

		public IParentItem Parent { get; set; }
	}

	internal class Information
	{
		public string FirmwareVersion;
		public string ImageVersion;
		public string MonoVersion;
		public string MonoCLRVersion;
		public string IpAddress;	
	}
}

