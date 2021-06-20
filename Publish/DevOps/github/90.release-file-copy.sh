set -e


#---------------------------------------------------------------------
#(x.1)参数
args_="

export codePath=/root/temp/svn

# "

 


#----------------------------------------------
echo "copy file"


\cp -rf $codePath/Publish/ReleaseFile/docker-deploy $codePath/Publish/release/release/docker-deploy
\cp -rf $codePath/Publish/release/release/publish/Sqler/Data $codePath/Publish/release/release/docker-deploy/sqler/Data
\cp -rf $codePath/Publish/release/release/publish/Sqler/appsettings.json $codePath/Publish/release/release/docker-deploy/sqler

 
