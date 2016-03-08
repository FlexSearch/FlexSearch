## Requirements

* .net Runtime Environment version 4.6 or higher
* Windows 7, Windows 2008 R2 or higher
* Ram : 4GB minimum / 8GB recommended
* CPU : Dual Core CPU (minimum) / Quad Core CPU (recommended)

## Download pre-compiled binaries
Latest pre-build releases can be downloaded from the following link:

https://github.com/FlexSearch/FlexSearch/releases

## Building from source

Download the source code from Github and run `build.bat` inside `src` folder.
This will compile the source code and create a distributable package under
deploy directory.

## Running FlexSearch

* Unpack the FlexSearch distribution to your desired location. It is suggested
that the root of a drive should be used to install the server properly, for
example `c:\flexsearch`.
* Start the server by executing `FlexSearch Server.exe`

## Running FlexSearch as a Windows service

* Open command prompt and navigate to the root of FlexSearch install directory
and type `FlexSearch-Server.exe --install`.  

@alert important
The command prompt should be run under administrator privileges, otherwise the
install will fail with insufficient permissions.
@end

Failing to start the command prompt in administrator mode will result in the
below error:
```
FlexSearch Server
Flexible and fast search engine for the .Net Platform
------------------------------------------------------------------
Version : 0.23.2.0
Copyright (C) 2010 - 2015 - FlexSearch
------------------------------------------------------------------


Configuration Result:
[Success] Name FlexSearch-Server
[Success] DisplayName FlexSearch Server
[Success] Description FlexSearch Server
[Success] ServiceName FlexSearch-Server
Topshelf v3.2.150.0, .NET Framework v4.0.30319.42000
The FlexSearch-Server service can only be installed as an administrator
Press any key to continue . . .
```

A successful install will output results as shown below:

```
FlexSearch Server
Flexible and fast search engine for the .Net Platform
------------------------------------------------------------------
Version : 0.23.2.0
Copyright (C) 2010 - 2015 - FlexSearch
------------------------------------------------------------------


Configuration Result:
[Success] Name FlexSearch-Server
[Success] DisplayName FlexSearch Server
[Success] Description FlexSearch Server
[Success] ServiceName FlexSearch-Server
Topshelf v3.2.150.0, .NET Framework v4.0.30319.42000

Running a transacted installation.

Beginning the Install phase of the installation.
Installing FlexSearch Server service
Installing service FlexSearch-Server...
Service FlexSearch-Server has been successfully installed.
Installing the ETW manifest...
Reserving the port 9800

Url reservation add failed, Error: 183
Cannot create a file when that file already exists.



The Install phase completed successfully, and the Commit phase is beginning.

The Commit phase completed successfully.

The transacted install has completed.
Press any key to continue . . .
```

### Viewing Logs

Once configured successfully you can access the logs from Event Viewer.

![Event Viewer](..\images\event-viewer.png)

### Accessing Portal
To confirm your installation, go to the FlexSearch root page at http://hostname:9800/.
The below screen will confirm a proper install of the server.

![FlexSearch Portal](..\images\flexsearch-portal.png)

## Troubleshooting install issues
FlexSearch installation compromises of three parts:

1. Installing the server as a Windows service
2. Installing the ETW manifest: FlexSearch uses Event tracing for Windows (ETW)
for logging. One can use PerfView or any other ETW compatible tool to trace the
application in real-time. In order to set-up ETW to display messages in Windows
Event Viewer, a manifest needs to be installed on the target machine. Explaining
the internal workings of ETW is out of scope of this document. For more
information about ETW visit: http://msdn.microsoft.com/en-us/library/ms751538(v=vs.110).aspx
3. Registering the HTTP port for the server to start.

<div class="note">
Event Tracing for Windows (ETW) is a general-purpose, high-speed tracing facility
provided by the operating system. Using a buffering and logging mechanism
implemented in the kernel, ETW provides a tracing mechanism for events raised by
both user-mode applications and kernel-mode device drivers. Additionally, ETW
gives you the ability to enable and disable logging dynamically, making it easy
to perform detailed tracing in production environments without requiring reboots
or application restarts. The logging mechanism uses per-processor buffers that
are written to disk by an asynchronous writer thread. This allows large-scale
server applications to write events with minimum disturbance. - MSDN
</div>

You can run any of the about three steps independently in case you encounter an
error.

### Manifest update/install issues

You can install/uninstall/upgrade ETW manifest from the command line using
the manifest switches. In order to see what options are available, just type
``FlexSearch-server.exe --help`` on the command prompt. The output should look
like below:

```
Usage: FlexSearch-Server.exe [options]
------------------------------------------------------------------
Options:

       --install [-i]: Installs the Windows Service
       --uninstall [-u]: Un-install the Windows Service
       --start: Starts the service if it is not already running
       --stop: Stops the service if it is running
       --installmanifest [-im]: Install the ETW manifest
       --uninstallmanifest [-um]: Un-install the ETW manifest
       --systeminfo: Print basic information about the running system
       --help [-h|/h|/help|/?]: display this list of options.
```

While updating FlexSearch if you receive the below warning, you can safely ignore it.

```
**** Warning: Publisher {a80d9e07-f298-55c2-6b8b-f15c4a504ca3} is installed on the system. Only new values would be added. If you want to update previous settings, uninstall the manifest first.
```

@alert info
We will make utmost effort to never update an existing value. Reinstalling an manifest can corrupt the existing logged data if the existing manifest values are modified.
@end

@alert tip
`Failed to open metadata for publisher <EventProviderName>. Access denied.` error usually indicate that there is an issue in accessing the path. Make sure that the executing user has necessary permissions over the path.
@end

@alert tip
Use `wevtutil gp <EventProviderName>` to get more information about a manifest.
@end

@alert tip
If you don\'t want to use an administrator account to configure the manifest then use an account which is a member of `Event Log Readers`.'
@end

@alert tip
Installing FlexSearch at the root of the drive can help in circumventing some folder specific access issues.
@end

### Adding URL reservation manually

Use the below command to add URL reservation.

```
netsh http add urlacl url=http://+:{port}/ user=everyone listen=yes
```
