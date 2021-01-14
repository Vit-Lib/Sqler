echo '复制文件'

::制作镜像
mklink /j "..\..\06.Docker\制作镜像\sqler\root\app\Sqler" "..\..\04.服务站点\Sqler" 


:: 部署文件
mklink /j "..\..\06.Docker\部署文件\Data" "..\..\04.服务站点\Sqler\Data" 
xcopy  "..\..\04.服务站点\Sqler\appsettings.json" "..\..\06.Docker\部署文件" 

