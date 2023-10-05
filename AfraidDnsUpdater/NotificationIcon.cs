/*
 * AfraidDnsUpdater - v2.0
 * Notification icon class and program entry point
 * An MS Windows System Tray program for auto update Afraid.org dynamic DNS service.
 * Copyright (C) 2017-2023, Petros Kyladitis
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
using System.IO;
using System.Security.Principal;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using Microsoft.Win32.TaskScheduler;
using Multipetros.Config ;

namespace AfraidDnsUpdater{
	public sealed class NotificationIcon{
		private NotifyIcon notifyIcon;
		private ContextMenu notificationMenu;
		private Ini ini ;
		private RegistryKey autorunReg ;
		private WindowsPrincipal winPrincipal ;
		private FileSystemWatcher watcher ;
		public const string INI_KEY = "key" ;
		public const string INI_NOTIFICATIONS = "notifications" ;
		public const string TASK_NAME = "Afraid" ;
		public const string FN_UPDATER = "UpdaterService.exe" ;
		public const string FN_INI = "afraid.ini" ;
		public const string FN_LOG = "afraid.log" ;

		public static string AppDataDir{
			get{
				return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), Application.ProductName);
			}
		}
		
		public static string AppDataIni{
			get{
				return Path.Combine(AppDataDir, FN_INI);
			}
		}
		
		public static string AppDataLog{
			get{
				return Path.Combine(AppDataDir, FN_LOG);
			}
		}
		
		public static string AppDir{
			get{
				return Path.GetDirectoryName(Application.ExecutablePath) ;
			}
		}
		
		public static string ProductName{
			get{
				return Application.ProductName ;
			}
		}
		
		public NotificationIcon(){
			if(!Directory.Exists(AppDataDir)){
				try {
					Directory.CreateDirectory(AppDataDir) ;
				} catch (Exception ex) {
					MessageBox.Show("Can't create program's data directory\n" + ex.Message, "Init Error", MessageBoxButtons.OK, MessageBoxIcon.Error) ;
					Application.Exit() ;
				}
			}
			ini = new Ini(AppDataIni, true) ;
			autorunReg = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true) ;
			winPrincipal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
			
			bool watching ;
			bool.TryParse(ini[INI_NOTIFICATIONS], out watching) ;
			watcher = new FileSystemWatcher();
			watcher.Path = AppDataDir ;
			watcher.Filter = FN_LOG ;
			watcher.NotifyFilter = NotifyFilters.LastWrite ;
			watcher.Changed += new FileSystemEventHandler(OnLogChanged);
			watcher.EnableRaisingEvents = watching ;
			
			notifyIcon = new NotifyIcon();
			notificationMenu = new ContextMenu(InitializeMenu());
			
			notifyIcon.DoubleClick += OnIconDoubleClick;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NotificationIcon));
			notifyIcon.Icon = (Icon)resources.GetObject("dns16.icon");
			notifyIcon.ContextMenu = notificationMenu;
		}
		
		/// <summary>Menu initialization</summary>
		/// <returns>A MenuItem array</returns>
		private MenuItem[] InitializeMenu(){
			bool taskEnabled = CreateTaskIfNotExist() ? false : IsTaskEnabled() ;
			MenuItem[] menu = new MenuItem[] {
				new MenuItem(string.Format("Scheduled Update {0}abled...", taskEnabled?"En":"Dis")),
				new MenuItem(string.Format("&{0}able Schedule", taskEnabled?"Dis":"En"), OnMenuScheduleClick),
				new MenuItem("-"),
				new MenuItem("Update &Key", OnMenuKeyClick),
				new MenuItem("Update &Interval", OnMenuIntervalClick),
				new MenuItem("Show &Notifications", OnMenuNotificationsClick),
				new MenuItem("Tray Icon Auto&run", OnMenuAutorunClick),
				new MenuItem("-"),
				new MenuItem("Run update no&w", OnMenuRunnowClick),
				new MenuItem("Show &Last Response", OnMenuLastResponseClick),
				new MenuItem("&About", OnMenuAboutClick),
				new MenuItem("E&xit", OnMenuExitClick)
			};
			
			//disable 1st item and make it look like a title
			menu[0].Enabled = false ;
			menu[0].DefaultItem = true ;
			menu[0].RadioCheck = true ;
			menu[0].Checked = true ;
			
			//disable options which needs admin rights
			if(!winPrincipal.IsInRole(WindowsBuiltInRole.Administrator)){
				menu[1].Enabled = false ;
				menu[3].Enabled = false ;
				menu[4].Enabled = false ;
			}
			
			//if user had select showing ballon notifications make 6th item checked
			try{
				menu[5].Checked = bool.Parse(ini[INI_NOTIFICATIONS]) ;
			}catch(Exception){}
			
			//if user had select start on windows boot make 7th item checked
			menu[6].Checked = (autorunReg.GetValue(Application.ProductName) != null) ;
			
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

		/// <summary>Show ballon tips if new data (update response) stored in log file</summary>
		private void OnLogChanged(object sender, FileSystemEventArgs e){
			if(File.Exists(AppDataLog)){
				string result ;
				try {
					result = File.ReadAllText(AppDataLog) ;
				} catch (Exception ex) {
					result = ex.Message ;
				}
				notifyIcon.BalloonTipTitle = "Afraid DNS update result" ;
				notifyIcon.BalloonTipText = result.Length>200 ? result.Substring(0, 200) : result ;
			  	notifyIcon.ShowBalloonTip(3000) ;
			}
		}

		/// <summary>Enable/Disable update scheduler</summary>
		private void OnMenuScheduleClick(object sender, EventArgs e){
			if(ini[INI_KEY].Length>0){
				bool taskEnable = !IsTaskEnabled() ;
				notificationMenu.MenuItems[0].Text = string.Format("Scheduled Update {0}abled...", taskEnable?"En":"Dis") ;
				notificationMenu.MenuItems[1].Text = string.Format("&{0}able Schedule", taskEnable?"Dis":"En") ;
				UpdateTask(taskEnable) ;
			}else{//if no dns key exist, prompt user
				NeedKeyMsgbox() ;
			}
		}
		
		/// <summary>Set dns update user key, default value is the current user value</summary>
		private void OnMenuKeyClick(object sender, EventArgs e){
			string input = Microsoft.VisualBasic.Interaction.InputBox("Afraid DNS update key", "Give your Afraid.org FreeDNS update key", ini[INI_KEY], Screen.PrimaryScreen.WorkingArea.Width-400, Screen.PrimaryScreen.WorkingArea.Height-270);
			if(input != ""){
				ini[INI_KEY] = input ;
			}
		}
		
		/// <summary>Set service interval in minutes, default value is 60</summary>
		private void OnMenuIntervalClick(object sender, EventArgs e){
			int interval = GetTaskInterval() ;
			string input = Microsoft.VisualBasic.Interaction.InputBox("Afraid DNS update interval", "Give your Afraid.org FreeDNS update interval in minutes", interval>0?interval.ToString():"60", Screen.PrimaryScreen.WorkingArea.Width-400, Screen.PrimaryScreen.WorkingArea.Height-270);
			if(input != ""){ //if user give something, try to parse it as integer
				int mins = 60 ;
				if(int.TryParse(input, out mins) && mins > 0){
					UpdateTask(mins) ;
				}
			}
		}
		
		/// <summary>Revert ballon notification showing & item checked state</summary>
		private void OnMenuNotificationsClick(object sender, EventArgs e){
			notificationMenu.MenuItems[5].Checked = !notificationMenu.MenuItems[5].Checked ;
			ini[INI_NOTIFICATIONS] = notificationMenu.MenuItems[5].Checked.ToString() ;
			watcher.EnableRaisingEvents = notificationMenu.MenuItems[5].Checked ;
		}
				
		/// <summary>Revert start on boot param, store it in registry & item checked state</summary>
		private void OnMenuAutorunClick(object sender, EventArgs e){
			notificationMenu.MenuItems[6].Checked = !notificationMenu.MenuItems[6].Checked ;
			StoreStartOnBoot() ;
		}
		
		/// <summary>Run updater immediately, out of scheduler</summary>
		private void OnMenuRunnowClick(object sender, EventArgs e){
			if(ini[INI_KEY].Length>0){
				Process proc = new Process() ;
				ProcessStartInfo procInfo = new ProcessStartInfo() ;
				procInfo.FileName = Path.Combine(AppDir, FN_UPDATER) ;
				procInfo.UseShellExecute = false ;
				procInfo.RedirectStandardOutput = true ;
				procInfo.RedirectStandardError = true ;
				procInfo.CreateNoWindow = true ;
				proc.StartInfo = procInfo ;
				try {
					proc.Start() ;
					proc.WaitForExit() ;
					if(watcher == null){
						ShowLastMsg() ;
					}
				} catch (Exception ex) {
					MessageBox.Show(ex.Message, "Running updater error",  MessageBoxButtons.OK, MessageBoxIcon.Error) ;
				}
			}else{
				NeedKeyMsgbox() ;
			}
		}	
		
		/// <summary>Call method to show last server's response</summary>
		private void OnMenuLastResponseClick(object sender, EventArgs e){
			ShowLastMsg() ;
		}		
		
		/// <summary>Show message box with program's info</summary>
		private void OnMenuAboutClick(object sender, EventArgs e){
			MessageBox.Show(string.Format("{0} - v{1}\r\nCopyright (c) 2017-2023, {2} <www.multipetros.gr>\n\nThis is free software, distributed under the therms & conditions of the FreeBSD License. For the full License text see at the 'license.txt' file, distributed with this project or at <http://www.multipetros.gr/freebsd-license/>.", Application.ProductName, new Version(Application.ProductVersion).ToString(2), Application.CompanyName), "About AfraidDnsUpdater", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}
		
		/// <summary>Store user setting to registry and exit program</summary>
		private void OnMenuExitClick(object sender, EventArgs e){
			autorunReg.Close() ;
			Application.Exit();
		}
		
		/// <summary>Call method to show last server's response</summary>
		private void OnIconDoubleClick(object sender, EventArgs e){
			ShowLastMsg() ;
		}
		
		/// <summary>Enable/disable autostart on log-in for this tray icon at windows registry</summary>
		private void StoreStartOnBoot(){
	    	if(autorunReg.GetValue(Application.ProductName) == null){
				autorunReg.SetValue(Application.ProductName, Application.ExecutablePath.ToString()) ;
	    	}else{
				autorunReg.DeleteValue(Application.ProductName, false) ;
	    	}
		}
				
		/// <summary>Show's message box with the last server response</summary>
		private void ShowLastMsg(){
			FileInfo fi = new FileInfo(AppDataLog) ;
			string lastRes = "No update response available yet" ; 
			if(fi.Exists){
				try {
					lastRes = string.Format("{0} : {1}", fi.LastWriteTime.ToShortTimeString(), File.ReadAllText(AppDataLog)) ;
				} catch (Exception ex) {
					lastRes = ex.Message ;
				}
			}
			MessageBox.Show(lastRes, "Last update response", MessageBoxButtons.OK, MessageBoxIcon.Information) ;
		}
		
		/// <summary>Create scheduler task if not exist</summary>
		/// <returns>True if create it, or False if task already exist</returns>
		private bool CreateTaskIfNotExist(){
			TaskService ts = new TaskService() ;
			Task t = ts.GetTask(TASK_NAME);
			if(t==null){
				CreateTask(60, false) ;
				return true ;
			}
			return false ;
		}
		
		/// <summary>Create new task</summary>
		/// <param name="minutes">Interval repetition time in minutes</param>
		/// <param name="enabled">Time triger enabled</param>
		private void CreateTask(int minutes, bool enabled){
   			if(winPrincipal.IsInRole(WindowsBuiltInRole.Administrator)){
				TaskDefinition td = TaskService.Instance.NewTask();
				td.RegistrationInfo.Description = "Auto update Afraid.org dynamic DNS service";
				td.Principal.UserId = "SYSTEM" ;
				td.Principal.LogonType = TaskLogonType.ServiceAccount;
				td.Settings.Enabled = true ;
				td.Settings.DisallowStartIfOnBatteries = false ;
				
				TimeTrigger tt = new TimeTrigger();
				tt.Repetition.Interval = TimeSpan.FromMinutes(minutes>0?minutes:60);
				tt.Enabled = enabled ;
				td.Triggers.Add(tt);
				td.Actions.Add(new ExecAction(Path.Combine(AppDir, FN_UPDATER), null, AppDir));
				
				TaskService.Instance.RootFolder.RegisterTaskDefinition(TASK_NAME, td);
			}else{
				NeedAdminMsgbox() ;
			}
		}
		
		/// <summary>Update interval repetition of scheduled task</summary>
		/// <param name="minutes">Interval repetition time in minutes</param>
		private void UpdateTask(int minutes){
			TaskService ts = new TaskService() ;
			Task t = ts.GetTask(TASK_NAME);
			if(t!=null){				
				if(minutes>0){
					if(t.Definition.Triggers.Count > 0){
						t.Definition.Triggers[0].Repetition.Interval = TimeSpan.FromMinutes(minutes);
					}else{
						TimeTrigger tt = new TimeTrigger();
						tt.Repetition.Interval = TimeSpan.FromMinutes(minutes);
						t.Definition.Triggers.Add(tt);
						t.Definition.Triggers[0].Enabled = false ;
					}
					t.RegisterChanges() ;
				}
			}else{
				CreateTaskIfNotExist();
			}
		}
		
		/// <summary>Update time triger enabled of scheduled task</summary>
		/// <param name="enabled">Time triger enabled</param>
		private void UpdateTask(bool enabled){
			TaskService ts = new TaskService() ;
			Task t = ts.GetTask(TASK_NAME);
			if(t!=null && t.Definition.Triggers.Count > 0){
				if(t.Definition.Triggers.Count > 0){
					t.Definition.Triggers[0].Enabled = enabled ;
				}else{
					TimeTrigger tt = new TimeTrigger();
					tt.Repetition.Interval = TimeSpan.FromMinutes(60);
					t.Definition.Triggers.Add(tt);
					t.Definition.Triggers[0].Enabled = enabled ;
				}
				t.RegisterChanges() ;
				if(enabled){
					t.Run() ;
				}
			}else{
				CreateTaskIfNotExist();
			}
		}
		
		/// <summary>Get task time triger enabled or not</summary>
		/// <returns>True if enabled, else false</returns>
		private bool IsTaskEnabled(){
			TaskService ts = new TaskService() ;
			Task t = ts.GetTask(TASK_NAME);
			if(t!=null && t.Definition.Triggers.Count > 0){
				return t.Definition.Triggers[0].Enabled ;
			}
			return false ;
		}
		
		private int GetTaskInterval(){
			TaskService ts = new TaskService() ;
			Task t = ts.GetTask(TASK_NAME);
			if(t!=null && t.Definition.Triggers.Count > 0){
				return t.Definition.Triggers[0].Repetition.Interval.Minutes ;
			}
			return 0 ;
		}
		
		/// <summary>Show message box, inform that admin rights needed</summary>
		private void NeedAdminMsgbox(){
			MessageBox.Show("You must configure this program (start, stop or change schedule) with administrator privileges.", "Error creating scheduled task", MessageBoxButtons.OK, MessageBoxIcon.Error) ;
		}
		
		/// <summary>Show message box, inform that no update key stored</summary>
		private void NeedKeyMsgbox(){
			MessageBox.Show("You must supply first, an Afraid DNS update key, by clicking the menu 'Update Key' option.\nYou can found your subdomains keys visiting http://freedns.afraid.org/dynamic/", "No update key found", MessageBoxButtons.OK, MessageBoxIcon.Error) ;
		}
	}
}
