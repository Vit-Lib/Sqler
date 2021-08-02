# sqler说明书-docker
> 源码地址： https://github.com/serset/sqler  
> 注： 在容器中  sqler = dotnet /root/app/Sqler.dll  

---------------------------------
# 1.查看帮助

``` bash
#查看全部帮助信息
docker run --rm -it serset/sqler dotnet Sqler.dll help
docker run --rm -it serset/sqler sqler help

#查看命令MySql.BackupSqler的帮助信息
docker run --rm -it serset/sqler dotnet Sqler.dll help -c MySql.BackupSqler

```

``` txt

开始执行命令 help ...
---------------
命令说明：
---------------
help
命令说明：
-c[--command] 要查询的命令。若不指定则返回所有命令的说明。如 help 
示例： help -c help
---------------


```


---------------------------------
# 2.mysql
Sqler可以对MySql数据库进行 备份、还原、创建、删除。


## 一.连接字符串说明

### (x.1)避免问题Unable to convert MySQL date/time value to System.DateTime
读取MySql时，如果存在字段类型为date/datetime时可能会出现以下问题，“Unable to convert MySQL date/time value to System.DateTime”  
解决方法为 在链接MySQL的字符串中添加：Convert Zero Datetime=True;Allow Zero Datetime=True;  
如： "Data Source=mysql;Port=3306;Database=wordpress;User Id=root;Password=123456;CharSet=utf8;Convert Zero Datetime=True;Allow Zero Datetime=True;"


### (x.2)避免datetime类型默认值出现“Invalid default value for..."错误
执行如下sql语句：

``` sql
-- 查看sql_mode
show variables like '%sql_mode%';

-- 修改sql_mode,去掉NO_ZERO_IN_DATE,NO_ZERO_DATE:
set sql_mode='ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION';
set global sql_mode='ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION';

```


## 二.备份数据库
备份mysql数据库到指定文件，使用sqler备份方式
> sqler备份步骤为：    
> 1.构建建库脚本保存到文件(CreateDataBase.sql)    
> 2.备份所有表数据到sqlite文件(Data.sqlite3)    
> 3.压缩为zip文件    

demo：
``` bash
docker run --rm -it \
-v /root/data:/root/data  \
serset/sqler  \
dotnet Sqler.dll MySql.BackupSqler \
--filePath "/root/data/wordpress.sqler.zip" \
--ConnectionString "Data Source=mysql;Port=3306;Database=wordpress;User Id=root;Password=123456;CharSet=utf8;Convert Zero Datetime=True;Allow Zero Datetime=True;"
```

参数说明：
``` txt
MySql.BackupSqler
远程备份数据库。参数说明：备份文件名称和路径指定其一即可,若均不指定则自动生成
-fn[--fileName] (可选)备份文件名称，备份文件在当前管理的备份文件夹中。例如 "DbDev_2020-06-08_135203.zip"
-fp[--filePath] (可选)备份文件路径，例如 "/root/docker/DbDev_2020-06-08_135203.zip"
-c[--useMemoryCache] 若为false则不使用内存进行全量缓存，默认:true。缓存到内存可以加快备份速度。在数据源特别庞大时请禁用此功能（指定false）。
-ConnStr[--ConnectionString] (可选)数据库连接字符串 例如 "Data Source=.;Database=Db_Dev;UID=sa;PWD=123456;"
--DataPath (可选)Data文件夹的路径。可为相对或绝对路径，默认："Data"
示例： MySql.BackupSqler --useMemoryCache false -ConnStr "Data Source=.;Database=Db_Dev;UID=sa;PWD=123456;" --filePath "/root/docker/DbDev_2020-06-08_135203.zip"
```


## 三.还原数据库
还原mysql备份文件到数据库

demo：
``` bash
#强制还原数据库
docker run --rm -it \
-v /root/data:/root/data  \
serset/sqler  \
dotnet Sqler.dll MySql.Restore \
--filePath "/root/data/wordpress.sqler.zip" \
--ConnectionString "Data Source=mysql;Port=3306;Database=wordpress;User Id=root;Password=123456;CharSet=utf8;Convert Zero Datetime=True;Allow Zero Datetime=True;"
```

参数说明：
``` txt
MySql.Restore
通过备份文件远程还原数据库。参数说明：备份文件名称和路径指定其一即可
-f[--force] 强制还原数据库。若指定此参数，则在数据库已经存在时仍然还原数据库；否则仅在数据库尚未存在时还原数据库。
-fn[--fileName] (可选)备份文件名称，备份文件在当前管理的备份文件夹中。例如 "DbDev_2020-06-08_135203.bak"
-fp[--filePath] (可选)备份文件路径，例如 "/root/docker/DbDev_2020-06-08_135203.bak"
-ConnStr[--ConnectionString] (可选)数据库连接字符串 例如 "Data Source=.;Database=Db_Dev;UID=sa;PWD=123456;"
--DataPath (可选)Data文件夹的路径。可为相对或绝对路径，默认："Data"
示例： MySql.Restore -ConnStr "Data Source=.;Database=Db_Dev;UID=sa;PWD=123456;" --filePath "/root/docker/DbDev_2020-06-08_135203.bak"

```



# 四.创建数据库

demo：
``` bash
docker run --rm -it \
serset/sqler \
dotnet Sqler.dll MySql.CreateDataBase \
-ConnStr "Data Source=.;Database=Db_Dev;UID=sa;PWD=123456;"
```

参数说明：
``` txt
MySql.CreateDataBase
若数据库不存在，则创建数据库。参数说明：
-ConnStr[--ConnectionString] (可选)数据库连接字符串 例如 "Data Source=.;Database=Db_Dev;UID=sa;PWD=123456;"
--DataPath (可选)Data文件夹的路径。可为相对或绝对路径，默认："Data"
示例： MySql.CreateDataBase -ConnStr "Data Source=.;Database=Db_Dev;UID=sa;PWD=123456;"
```





# 六.删除数据库
demo：
``` bash
docker run --rm -it \
serset/sqler \
dotnet Sqler.dll MySql.DropDataBase \
-ConnStr "Data Source=.;Database=Db_Dev;UID=sa;PWD=123456;"
```

参数说明：
``` txt
MySql.DropDataBase
若数据库存在，则删除数据库。参数说明：
-ConnStr[--ConnectionString] (可选)数据库连接字符串 例如 "Data Source=.;Database=Db_Dev;UID=sa;PWD=123456;"
--DataPath (可选)Data文件夹的路径。可为相对或绝对路径，默认："Data"
示例： MySql.DropDataBase -ConnStr "Data Source=.;Database=Db_Dev;UID=sa;PWD=123456;" 

```



---------------------------------
# 3.mssql
Sqler可以对msql(Sql Server)数据库进行 备份、还原、创建、删除。


 

## 二.备份数据库
备份数据库到指定文件，使用sqler备份方式
> sqler备份步骤为：    
> 1.构建建库脚本保存到文件(CreateDataBase.sql)    
> 2.备份所有表数据到sqlite文件(Data.sqlite3)    
> 3.压缩为zip文件    

demo：
``` bash
docker run --rm -it \
-v /root/data:/root/data  \
serset/sqler  \
dotnet Sqler.dll SqlServer.BackupLocalBak \
--filePath "/root/data/wordpress.sqler.zip" \
--ConnectionString 'Data Source=192.168.3.221,1434;Database=Db_Dev;UID=sa;PWD=LongLongPassword1!;'
```

参数说明：
``` txt
SqlServer.BackupLocalBak
本地bak备份数据库。参数说明：备份文件名称和路径指定其一即可,若均不指定则自动生成
-fn[--fileName] (可选)备份文件名称，备份文件在当前管理的备份文件夹中。例如 "DbDev_2020-06-08_135203.bak"
-fp[--filePath] (可选)备份文件路径，例如 "/root/docker/DbDev_2020-06-08_135203.bak"
-ConnStr[--ConnectionString] (可选)数据库连接字符串 例如 "Data Source=.;Database=Db_Dev;UID=sa;PWD=123456;"
--DataPath (可选)Data文件夹的路径。可为相对或绝对路径，默认："Data"
示例： SqlServer.BackupLocalBak -ConnStr "Data Source=.;Database=Db_Dev;UID=sa;PWD=123456;" --filePath "/root/docker/DbDev_2020-06-08_135203.bak"
```


## 三.还原数据库
还原备份文件到数据库（SqlServer.Restore、SqlServer.RestoreLocalBak）

demo：
``` bash
#强制还原数据库
docker run --rm -it \
-v /root/data:/bak  \
serset/sqler  \
dotnet Sqler.dll SqlServer.RestoreLocalBak \
--filePath "/bak/wordpress.sqler.zip" \
--databasePath "/data" \
--ConnectionString 'Data Source=192.168.3.221,1434;Database=Db_Dev;UID=sa;PWD=LongLongPassword1!;'
```

参数说明：
``` txt
SqlServer.Restore
通过备份文件远程还原数据库。参数说明：备份文件名称和路径指定其一即可
-f[--force] 强制还原数据库。若指定此参数，则在数据库已经存在时仍然还原数据库；否则仅在数据库尚未存在时还原数据库。
-fn[--fileName] (可选)备份文件名称，备份文件在当前管理的备份文件夹中。例如 "DbDev_2020-06-08_135203.bak"
-fp[--filePath] (可选)备份文件路径，例如 "/root/docker/DbDev_2020-06-08_135203.bak"
-ConnStr[--ConnectionString] (可选)数据库连接字符串 例如 "Data Source=.;Database=Db_Dev;UID=sa;PWD=123456;"
-dp[--databasePath] (可选)数据库文件存放的路径 例如 "/data/mssql"
--DataPath (可选)Data文件夹的路径。可为相对或绝对路径，默认："Data"
示例： SqlServer.Restore -ConnStr "Data Source=.;Database=Db_Dev;UID=sa;PWD=123456;" --filePath "/root/docker/DbDev_2020-06-08_135203.bak"

```



# 四.创建数据库

demo：
``` bash
docker run --rm -it serset/sqler \
dotnet Sqler.dll SqlServer.CreateDataBase \
--ConnectionString 'Data Source=192.168.3.221,1434;Database=Db_Dev;UID=sa;PWD=LongLongPassword1!;'
--databasePath "/data"
```

参数说明：
``` txt
SqlServer.CreateDataBase
若数据库不存在，则创建数据库。参数说明：
-ConnStr[--ConnectionString] (可选)数据库连接字符串 例如 "Data Source=.;Database=Db_Dev;UID=sa;PWD=123456;"
-dp[--databasePath] (可选)数据库文件存放的路径 例如 "/data/mssql"
--DataPath (可选)Data文件夹的路径。可为相对或绝对路径，默认："Data"
示例： SqlServer.CreateDataBase -ConnStr "Data Source=.;Database=Db_Dev;UID=sa;PWD=123456;"
```


# 五.删除数据库

demo：
``` bash
docker run --rm -it serset/sqler \
dotnet Sqler.dll SqlServer.DropDataBase \
--ConnectionString 'Data Source=192.168.3.221,1434;Database=Db_Dev;UID=sa;PWD=LongLongPassword1!;'
```


---------------------------------
# 六.常驻后台服务

## (x.1)配置文件
	  (x.x.1)把本文件所在目录中的Data拷贝到宿主机
	  (x.x.2)修改配置文件 appsettings.json
 

## (x.2)创建容器并运行
(--name 容器名称，可自定义)
(--restart=always 自动重启)
(-v /etc/localtime:/etc/localtime)挂载宿主机localtime文件解决容器时间与主机时区不一致的问题
(-v $PWD/data:/data 将主机中当前目录下的data挂载到容器的/data)
(--net=host 网络直接使用宿主机网络)（-p 6022:6022 端口映射）

``` bash
cd /root/docker/sqler
docker run --name=sqler --restart=always -d \
-p 4570:4570 \
-v /etc/localtime:/etc/localtime \
-v $PWD/appsettings.json:/root/app/sqler/appsettings.json \
-v $PWD/Data:/root/app/sqler/Data  \
-v $PWD/Logs:/root/app/sqler/Logs  \
serset/sqler


#精简
docker run --name=sqler --restart=always -d -p 4570:4570 serset/sqler


#应用已经运行
#访问地址为 http://ip:4570 



#---------------------------------------
#常用命令

#查看容器logs
docker logs sqler

#在容器内执行命令行
docker exec -it sqler bash

#停止容器
docker stop sqler

#打开容器
docker start sqler

#重启容器
docker restart sqler

#删除容器
docker rm sqler -f


#--------------------------------------
#文件复制

#1、从容器拷贝文件到宿主机
docker cp sqler:/root/app/SqlerData/ SqlerData

#2、从宿主机拷贝文件到容器
docker cp SqlerData sqler:/root/app/SqlerData/

```






