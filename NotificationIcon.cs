/*
 * AfraidDnsUpdater
 * Notification icon class and program entry point - v1.0
 * An MS Windows System Tray program for auto update Afraid.org Free DNS service.
 * Copyright (C) 2017, Petros Kyladitis
 *
 * This program is free software distributed under the FreeBSD License,
 * for license details see at 'license.txt' file, distributed with
 * this project, or see at <http://www.multipetros.gr/freebsd-license/>.
 */
 
using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using System.Net;
using System.Timers;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using Multipetros.Config ;

namespace AfraidDnsUpdater{
	public sealed class NotificationIcon{
		private NotifyIcon notifyIcon;
		private ContextMenu notificationMenu;
		private System.Timers.Timer timer = new System.Timers.Timer() ;
		private string afraidUrl = "http://freedns.afraid.org/dynamic/update.php?" ; //dns update script path
		private RegistryIni ini = new RegistryIni(Application.CompanyName, Application.ProductName) ;
		private string usrKey = "" ;               //user setting: user domain key for dns update, given by afraid
		private int usrUpdateMins = 60 ;           //user setting: update interval in minutes (lower times may have no sense)
		private bool usrShowNotifications = true ; //user setting: show ballon notifications
		private bool usrAutoStart = false ;        //user setting: autostart update service when program start
		private bool usrStartOnBoot = false ;      //user setting: start on windows boot
		private bool serviceRun = false ;          //shows if update service is running
		private string lastMsg = "No update info available yet" ; //last server response
		
		public NotificationIcon(){
			LoadSettings() ;
			notifyIcon = new NotifyIcon();
			notificationMenu = new ContextMenu(InitializeMenu());
			
			notifyIcon.DoubleClick += IconDoubleClick;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NotificationIcon));
			notifyIcon.Icon = (Icon)resources.GetObject("dns16.icon");
			notifyIcon.ContextMenu = notificationMenu;
			
			//if autostart is enabled, start sending update at 2secs
			if(usrAutoStart){
				timer.Elapsed += new ElapsedEventHandler(timer_Elapsed);
				timer.Interval = 2000 ;
				timer.Start() ;
			}
		}
		
		/// <summary>Load user settings from registry </summary>
		private void LoadSettings(){
			usrKey = ini["key"] ;
			string regMins = ini["interval"] ;
			int.TryParse(ini["interval"], out usrUpdateMins) ;
			if(usrUpdateMins < 1){
				usrUpdateMins = 60 ;
			}
			if(!bool.TryParse(ini["notifications"], out usrShowNotifications)){
				usrShowNotifications = true ;
			}
			bool.TryParse(ini["autostart"], out usrAutoStart) ;
			bool.TryParse(ini["startonboot"], out usrStartOnBoot) ;
		}
		
		/// <summary>Menu initialization</summary>
		/// <returns>A MenuItem array</returns>
		private MenuItem[] InitializeMenu(){
			MenuItem[] menu = new MenuItem[] {
				new MenuItem("Service Started..."),
				new MenuItem("&Stop Service", menuUpdateClick),
				new MenuItem("-"),
				new MenuItem("Update &Key", menuSettingsClick),
				new MenuItem("Update &Interval", menuIntervalClick),
				new MenuItem("Show &Notifications", menuShowNotificationsClick),
				new MenuItem("Start on System &Boot", menuStartOnBootClick),
				new MenuItem("-"),
				new MenuItem("Show &Last Action", menuShowLastAction),
				new MenuItem("&About", menuAboutClick),
				new MenuItem("E&xit", menuExitClick)
			};
			
			//disable 1st item and make it look like a title
			menu[0].Enabled = false ;
			menu[0].DefaultItem = true ;
			menu[0].RadioCheck = true ;
			menu[0].Checked = true ;

			//if user had select service autosart change 1st & 2nd items text
			if(!usrAutoStart){
				menu[0].Text = "Service Stoped..." ;
				menu[1].Text = "&Start Service" ;
			}
			
			//if user had select showing ballon notifications make 6th item checked
			menu[5].Checked = usrShowNotifications ;
			
			//if user had select start on windows boot make 7th item checked
			menu[6].Checked = usrStartOnBoot ;
			
			return menu;
		}
		
		/// <summary>Program entry point.</summary>
		/// <param name="args">Command Line Arguments</param>
		[STAThread]
		public static void Main(string[] args){
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			
			bool isFirstInstance;
			//A unique name for the mutex to prevent conflicts with other programs
			using (Mutex mtx = new Mutex(true, "AfraidDnsUpdater", out isFirstInstance)) {
				if (isFirstInstance) {
					NotificationIcon notificationIcon = new NotificationIcon();
					notificationIcon.notifyIcon.Visible = true;
					Application.Run();
					notificationIcon.notifyIcon.Dispose();
				} else {
					//If application is already running show message box and exit
					MessageBox.Show("An instance of the program is already running.\r\nOnly one can run at a time.", Application.ProductName + " is already running", MessageBoxButtons.OK, MessageBoxIcon.Exclamation) ;
					Application.Exit() ;
				}
			} 
		}

		/// <summary>Start/Stop update service</summary>
		private void menuUpdateClick(object sender, EventArgs e){
			if(serviceRun){ //stop the timer & change item titles
				timer.Stop() ;
				notificationMenu.MenuItems[0].Text = "Service Stoped..." ;
				notificationMenu.MenuItems[1].Text = "&Start Service" ;
			}else{ //send an update & change item titles
				ServiceTrigger() ;
				notificationMenu.MenuItems[0].Text = "Service Started..." ;
				notificationMenu.MenuItems[1].Text = "&Stop Service" ;
			}
		}
		
		/// <summary>Set dns update user key, default value is the current user value</summary>
		private void menuSettingsClick(object sender, EventArgs e){
			string input = Microsoft.VisualBasic.Interaction.InputBox("Afraid DNS update key", "Give your Afraid.org FreeDNS update key", usrKey, Screen.PrimaryScreen.WorkingArea.Width-400, Screen.PrimaryScreen.WorkingArea.Height-270);
			if(input != ""){
				usrKey = input ;
			}
			if(serviceRun){ //restart service
				timer.Stop() ;
				StartTimer() ;
			}
		}
		
		/// <summary>Set service interval in minutes, default value is the current user value</summary>
		private void menuIntervalClick(object sender, EventArgs e){
			string input = Microsoft.VisualBasic.Interaction.InputBox("Afraid DNS update interval", "Give your Afraid.org FreeDNS update interval in minutes", usrUpdateMins.ToString(), Screen.PrimaryScreen.WorkingArea.Width-400, Screen.PrimaryScreen.WorkingArea.Height-270);
			if(input != ""){ //if user give something, try to parse it as integer
				int temp = usrUpdateMins ;
				if(!int.TryParse(input, out usrUpdateMins) || usrUpdateMins < 1){
					usrUpdateMins = temp ;
				}
			}
			if(serviceRun){ //restart service
				timer.Stop() ;
				StartTimer() ;
			}
		}
		
		/// <summary>Revert ballon notification showing & item checked state</summary>
		private void menuShowNotificationsClick(object sender, EventArgs e){
			usrShowNotifications = !usrShowNotifications ;
			notificationMenu.MenuItems[5].Checked = usrShowNotifications ;
		}
				
		/// <summary>Revert start on boot param, store it in registry & item checked state</summary>
		private void menuStartOnBootClick(object sender, EventArgs e){
			usrStartOnBoot = !usrStartOnBoot ;
			notificationMenu.MenuItems[6].Checked = usrStartOnBoot ;
			StoreStartOnBoot() ;
		}
		
		/// <summary>Call method to show last server's response</summary>
		private void menuShowLastAction(object sender, EventArgs e){
			ShowLastMsg() ;
		}		
		
		/// <summary>Show message box with program's info</summary>
		private void menuAboutClick(object sender, EventArgs e){
			MessageBox.Show("AfraidDnsUpdater - v1.0\r\nCopyright (c) 2017, Petros Kyladitis <www.multipetros.gr>\n\nThis is free software, distributed under the therms & conditions of the FreeBSD License. For the full License text see at the 'license.txt' file, distributed with this project or at <http://www.multipetros.gr/freebsd-license/>.", "About AfraidDnsUpdater", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}
		
		/// <summary>Store user setting to registry and exit program</summary>
		private void menuExitClick(object sender, EventArgs e){
			ini["key"] = usrKey ;
			ini["interval"] = usrUpdateMins.ToString() ;
			ini["notifications"] = usrShowNotifications.ToString() ;
			ini["autostart"] = serviceRun.ToString() ;
			ini["startonboot"] = usrStartOnBoot.ToString() ;
			Application.Exit();
		}
		
		/// <summary>Call method to show last server's response</summary>
		private void IconDoubleClick(object sender, EventArgs e){
			ShowLastMsg() ;
		}
		
		/// <summary>Send dns update GET request to the Afraid.org service</summary>
		void ServiceTrigger(){			
			if(usrKey != ""){
				serviceRun = true ;	
				Uri uri = new Uri(afraidUrl + usrKey) ;
				WebClient client = new WebClient() ;
				string result = "" ;
				try{ 
					//send request and cut the first 8 chars from the Afraids.org service responce because
					//if client ip is not changed from the prior, server's response starts with the word 'ERROR: '
					//which can disorientate user & make him thinks that request gone bad, that's not true.
					result = client.DownloadString(uri).Substring(7) ;
				}catch(Exception e){
					//on error, result is the exception's message
					result = e.Message ;
				}finally{
					if(usrShowNotifications){
						notifyIcon.BalloonTipTitle = "Trying to update DNS" ;
						notifyIcon.BalloonTipText = result ;
						notifyIcon.ShowBalloonTip(2000) ;
					}
					//save last server response
					lastMsg = DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString() + "\r\n" + result ;
					//start service timer again
					StartTimer() ;
				}
			}else{ //if update user key not setted (like 1st running time), prompt user to give it
				string input = Microsoft.VisualBasic.Interaction.InputBox("Please configure it first and press 'Update DNS Now!' to start the service.", "Afraid.org FreeDNS key not setted", usrKey, Screen.PrimaryScreen.WorkingArea.Width-400, Screen.PrimaryScreen.WorkingArea.Height-270);
				if(input != "") //if user give a value store it & send again an update request
					usrKey = input ;
					ServiceTrigger() ;
				}
		}
		
		/// <summary>Enable/disable start on boot for this program at windows registry</summary>
		private void StoreStartOnBoot(){
	    	RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true) ;

			if (usrStartOnBoot)
				key.SetValue(Application.ProductName, Application.ExecutablePath.ToString()) ;
	        else
				key.DeleteValue(Application.ProductName, false) ; 			
		}
				
		/// <summary>Show's message box with the last server response</summary>
		private void ShowLastMsg(){
			MessageBox.Show(lastMsg, "Last update info", MessageBoxButtons.OK, MessageBoxIcon.Information) ;
		}
		
		/// <summary>Set service timer options & start the countdown</summary>
		private void StartTimer(){
			timer.Elapsed += new ElapsedEventHandler(timer_Elapsed); //when time elpased call dns service update method
			timer.Interval = usrUpdateMins * 60000 ; //convert minutes to ms
			timer.Start() ;
		}

		/// <summary>Stop timer & call dns service update method</summary>
		void timer_Elapsed(object sender, ElapsedEventArgs e){
			timer.Stop() ;
			ServiceTrigger() ;
		}
	}
}
