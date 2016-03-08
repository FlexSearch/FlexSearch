## Directory Structure
FlexSearch follow a specific directory structure, understanding this will help you in troubleshooting issues with FlexSearch. This will also help you in understanding the folders which needs to be backed up when taking an on-site or offsite backup.

```
flexsearch
+-- conf
| +-- config.ini
+-- data
+-- lib
+-- licenses
+-- logs
+-- plugins
+-- web
```

#### Configuration folder
This folder contains all the configuration related data used by FlexSearch. Backing this folder will allow you to the restore indexes, analysers and all related settings to a new server. This will also contains scripts folder which is used restored custom scripts. Config.ini file present in the folder is a global configuration file.

#### Data folder

This folder contains all indexing related data used by FlexSearch. Each index present in the system has its own subfolder below this folder. Each subfolder can be independently backed up and re-stored. Backing this folder along with configuration folder will give you everything that is needed to migrate a server.

```
data
+-- duplicates
 	|+-- shards
	 	|+-- 0
		 	|+-- index
			|+-- txlogs
		|+-- 1
			|+-- index
			|+-- txlogs

```
Each index folder contains a folder called shards which in turn can contain multiple subfolders each representing a shard present in the index. The shards are numbered from 0 and up. Each shard folder contains two subfolders index and txlogs. Each FlexSearch shard is a valid Lucene index and can be opened using tools like Luke. FlexSearch uses write ahead logging to store the information which is not committed to the physical medium. This gives FlexSearch the ability to recover data which is not saved to the physical index yet. The transaction log folder is used to save per shard logs.

#### Library folder
This folder contains all the third-party libraries used by FlexSearch.

#### Logs folder
This folder contains all the server logs. FlexSearch only writes physical files for logging when it cannot access Windows ETW logger. This folder also contains a special file called startup-log.txt, this file is always written by FlexSearch irrespective of the logger setting and can be used to identify server start-up related issues.

#### Plug-ins folder
This folder contains all the custom/third-party plug-ins written for FlexSearch. These plug-ins are loaded during the start-up.

#### Web folder
This folder contains portal related files.

## Configure global Settings

Global configuration can be accessed from `Config.ini` file under `Conf` folder present in the root directory.

`HttpPort` key and the server section can be used to configure the port number used by FlexSearch to start the server. This file can also be used by custom plug-ins to allow user to configure the plug-in.

```
[Server]
HttpPort = 9800
DataFolder = ./data
NodeName = FlexNode
```
