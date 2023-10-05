# AfraidDnsUpdater - v2.0
Copyright (C) 2017-2023, Petros Kyladitis

## Description
An MS Windows System Tray program for auto update Afraid.org Free DNS service.  
You can select the interval update time in minutes, and enable/disable program's autostart at Windows boot.  
  
For updates and more info see at <http://multipetros.gr/>

## Screenshot
![Screenshot](https://raw.githubusercontent.com/multipetros/AfraidDnsUpdater/master/.github/screenshot.png) 

## Requirements
This program is designed for MS Windows with .NET Framework 3.5 installed.  
To configure this, you __must run it__ from an account __with administrator privileges__.  

## Usage
- Starting, input the domain key for Afraid.org DNS service, which appears after pressing the _'Update Key'_ on right click at programs icon in system tray.
- Next, input the interval time _in minutes_ for DNS service the auto update, which appears after pressing the _'Update Interval'_ .
- When an DNS update process ends, the program appears a Ballon Notification with the server's response. You can enable/disable the Ballon Notifications from the _'Show Notifications'_ option.
- You can also enable/disable the auto-start of the program on Windows boot from the _'Tray Icon Autorun'_ option.
- You can anytime cancel the scheduled auto-update process by pressing the _'Disable Schedule'_ option.
- To re-enable the scheduled auto-update just select the _'Enable Schedule'_ option, that now appears at the previous possition.
- By double click on the programs icon at system tray, you can see the last server response.
- The same you can do by sellect the _'Show Last Response'_ option.
- You can run the update immediately, out of scheduler, by clicking the _'Run update now'_ option.

## Download
[![afraiddnsupdater-2.0-setup.exe](https://img.shields.io/badge/%F0%9F%92%BE%20AfraidDnsUpdater_2.0-setup.exe-lightgrey)](https://github.com/multipetros/AfraidDnsUpdater/releases/download/v2.0/afraiddnsupdater-2.0-setup.exe)

## Changelog
### v2.0
 - Move from internal timer, to the native windows task scheduler.
 - Can run on system boot, with out the need of any user log-on.
 - Option to run the dynamic DNS update on demand, from the icon tray menu.

### v1.0
 - Initial release

## License
This program is free software distributed under the FreeBSD License,
for license details see at 'license.txt' file, distributed with
this project, or see at <http://www.multipetros.gr/freebsd-license/>.

## 3rd Party Licenses & Credits
Microsoft.Win32.TaskScheduler - ver 2.10.1  
MIT Copyright (c) 2003-2010 David Hall <https://github.com/dahall/TaskScheduler> 

## Donations
If you think that this program is helpful for you and you are willing to support the developer, feel free to  make a donation through [PayPal](https://www.paypal.me/PKyladitis).  

[![Donate](https://img.shields.io/badge/Donate-PayPal-green.svg)](https://www.paypal.me/PKyladitis)