/*
 * UpdaterService - v1.0
 * A CLI updater for AfraidDnsUpdater
 * An MS Windows program for auto update Afraid.org dynamic DNS service.
 * Copyright (C) 2023, Petros Kyladitis
 *
 * This program is free software distributed under the FreeBSD License,
 * for license details see at 'license.txt' file, distributed with
 * this project, or see at <http://www.multipetros.gr/freebsd-license/>.
 */
using System;
using System.Net;
using System.IO;
using Multipetros.Config;

namespace UpdaterService{
	class Program{
		public static void Main(string[] args){
			if(!Directory.Exists(AfraidDnsUpdater.NotificationIcon.AppDataDir)){
				try {
					Directory.CreateDirectory(AfraidDnsUpdater.NotificationIcon.AppDataDir) ;
				} catch (Exception ex) {
					Console.WriteLine(ex.Message) ;
					return ;
				}
			}
			Ini ini = new Ini(AfraidDnsUpdater.NotificationIcon.AppDataIni) ;
			string dnsKey = ini[AfraidDnsUpdater.NotificationIcon.INI_KEY] ;			
			string result = "" ;
			if(dnsKey.Length > 0){
				try{
					Uri uri = new Uri("http://freedns.afraid.org/dynamic/update.php?" + dnsKey) ;
					WebClient client = new WebClient() ;
					//send request and cut the first 8 chars from the Afraids.org service responce because
					//if client ip is not changed from the prior, server's response starts with the word 'ERROR: '
					//which can disorientate user & make him thinks that request gone bad, that's not true.
					result = client.DownloadString(uri).Substring(7) ;
				}catch(Exception ex){
					//on error, result is the exception's message
					result = ex.Message ;
				}finally{
					try{
						File.WriteAllText(AfraidDnsUpdater.NotificationIcon.AppDataLog, result) ;
					}catch(Exception ex2){
						Console.WriteLine(ex2.Message) ;
					}
				}
			}else{
				Console.WriteLine("Subdomain update key not setted.") ;
			}
		}
	}
}