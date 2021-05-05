docker部署sqler

 

---------------------------------
#(x.1)配置文件
  (x.1)把本文件所在目录中的Data拷贝到宿主机
  (x.2)修改配置文件 appsettings.json
 

#(x.2)创建容器并运行
(--name 容器名称，可自定义)
(--restart=always 自动重启)
(-v /etc/localtime:/etc/localtime)挂载宿主机localtime文件解决容器时间与主机时区不一致的问题
(-v $PWD/data:/data 将主机中当前目录下的data挂载到容器的/data)
(--net=host 网络直接使用宿主机网络)（-p 6022:6022 端口映射）

cd /root/docker

cd sqler
docker run --name=sqler --restart=always -d \
-p 4570:4570 \
-v /etc/localtime:/etc/localtime \
-v $PWD/appsettings.json:/root/app/sqler/appsettings.json \
-v $PWD/Data:/root/app/sqler/Data  \
-v $PWD/Logs:/root/app/sqler/Logs  \
serset/sqler
cd .. 


#精简
docker run --name=sqler --restart=always -d -p 4570:4570 serset/sqler



#(x.3)应用已经运行 
 http://ip:4570 

#---------------------------------------
#常用命令

#查看容器logs
docker logs sqler

#在容器内执行命令行
docker  exec -it sqler /bin/sh

#停止容器
docker stop sqler

#打开容器
docker start sqler

#重启容器
docker restart sqler


#删除容器
docker rm sqler -f




#----------------------------------------------------------
#命令行运行

cd /root/docker

cd sqler
docker run --rm -it  -p 4570:4570  \
-v /etc/localtime:/etc/localtime \
-v $PWD/appsettings.json:/root/app/sqler/appsettings.json \
-v $PWD/Logs:/root/app/sqler/Logs  \
-v $PWD/SqlerData:/root/app/SqlerData  \
serset/sqler  \
dotnet Sqler.dll --DataPath "../SqlerData/Local_Basis"
cd .. 



#查看帮助
docker run --rm -it serset/sqler dotnet Sqler.dll help



# mysql

## 连接字符串说明

### (x.1)避免问题Unable to convert MySQL date/time value to System.DateTime
读取MySql时，如果存在字段类型为date/datetime时的可能会出现以下问题，“Unable to convert MySQL date/time value to System.DateTime”
解决方式为 在链接MySQL的字符串中添加：Convert Zero Datetime=True;Allow Zero Datetime=True;
如： "Data Source=mysql;Port=3306;Database=wordpress;User Id=root;Password=123456;CharSet=utf8;Convert Zero Datetime=True;Allow Zero Datetime=True;"


### (x.2)避免datetime类型默认值出现“Invalid default value for..."错误
-- 查看sql_mode
show variables like '%sql_mode%';

-- 修改sql_mode,去掉NO_ZERO_IN_DATE,NO_ZERO_DATE:
set global sql_mode='ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION';




#备份数据库
docker run --rm -it \
--link mysql80:mysql \
-v /root/data:/root/data  \
serset/sqler  \
dotnet Sqler.dll MySql.BackupSqler \
--filePath "/root/data/wordpress.sqler.zip" \
--ConnectionString "Data Source=mysql;Port=3306;Database=wordpress;User Id=root;Password=123456;CharSet=utf8;Convert Zero Datetime=True;Allow Zero Datetime=True;"

 






#还原数据库
docker run --rm -it \
--link mysql80:mysql \
-v /root/data:/root/data  \
serset/sqler  \
dotnet Sqler.dll MySql.Restore \
--filePath "/root/data/wordpress.zip" \
--ConnectionString "Data Source=mysql;Port=3306;Database=wordpress;User Id=root;Password=123456;CharSet=utf8;Convert Zero Datetime=True;Allow Zero Datetime=True;"

 



#运行容器，在断开后自动关闭并清理
docker run --rm -it -p 4570:4570 serset/sqler dotnet Sqler.dll help

docker run --rm -it -p 4570:4570 serset/sqler bash
dotnet Sqler.dll help

 
 

---------------------------------------
#文件复制

#1、从容器拷贝文件到宿主机
docker cp sqler:/root/app/SqlerData/ SqlerData

#2、从宿主机拷贝文件到容器
docker cp SqlerData sqler:/root/app/SqlerData/ 




