echo '复制文件'

::制作镜像
xcopy "..\..\04.服务站点\Sqler"  "..\..\06.Docker\制作镜像\sqler\sqler"  /e /i /r /y


:: 部署文件
xcopy "..\..\04.服务站点\Sqler\Data"  "..\..\06.Docker\部署文件\Data" /e /i /r /y
xcopy  "..\..\04.服务站点\Sqler\appsettings.json" "..\..\06.Docker\部署文件" 

