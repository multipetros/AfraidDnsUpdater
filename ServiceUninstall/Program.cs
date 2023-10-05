/*
 * ServiceUninstall - v1.0
 * A CLI scheduled job uninstaller for AfraidDnsUpdater
 * An MS Windows program for auto update Afraid.org dynamic DNS service.
 * Copyright (C) 2023, Petros Kyladitis
 *
 * This program is free software distributed under the FreeBSD License,
 * for license details see at 'license.txt' file, distributed with
 * this project, or see at <http://www.multipetros.gr/freebsd-license/>.
 */
using System;
using System.IO;
using System.Diagnostics;
using AfraidDnsUpdater;
using Microsoft.Win32;
using Microsoft.Win32.TaskScheduler;
using System.Security.Principal;

namespace ServiceUninstall{
	class Program{
		public static void Main(string[] args){
   			WindowsPrincipal wp = new WindowsPrincipal(WindowsIdentity.GetCurrent());
   			Console.WriteLine(AfraidDnsUpdater.NotificationIcon.ProductName) ;
   			if(wp.IsInRole(WindowsBuiltInRole.Administrator)){
   				try{
	   				//terminate icon tray proc if running
					foreach (Process process in Process.GetProcessesByName(AfraidDnsUpdater.NotificationIcon.ProductName)){
	    				process.Kill();
					}
	   				//delete data files
	   				if(Directory.Exists(AfraidDnsUpdater.NotificationIcon.AppDataDir)){
	   					Directory.Delete(AfraidDnsUpdater.NotificationIcon.AppDataDir, true) ;
	   				}
	   				//remove task from scheduler
					TaskService ts = new TaskService() ;
					ts.RootFolder.DeleteTask(AfraidDnsUpdater.NotificationIcon.TASK_NAME, false) ;
					//remove autorun key from registry
					RegistryKey autorunReg = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true) ;
					if(autorunReg.GetValue(AfraidDnsUpdater.NotificationIcon.ProductName) != null){
						autorunReg.DeleteValue(AfraidDnsUpdater.NotificationIcon.ProductName, false) ;
		    		}
   				}catch(Exception ex){
   					Console.WriteLine(ex.Message) ;
   				}
   			}else{
   				Console.WriteLine("You must run this program with administrator privileges.") ;
   			}			
		}
	}
}