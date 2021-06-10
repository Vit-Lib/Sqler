set -e



#(x.1)当前路径 
curWorkDir=$PWD
curPath=$(dirname $0)

cd $curPath/../..
codePath=$PWD
# codePath=/root/docker/jenkins/workspace/sqler/svn

name=sqler

echo "(x.2)get version"
version=`grep '<Version>' ${codePath} -r --include *.csproj | grep -oP '>(.*)<' | tr -d '<>'`
# echo $version



#----------------------------------------------
#(x.2)环境变量
# releaseFile=$codePath/Publish/git/${name}${version}.zip

filePath="$codePath/Publish/git/${name}${version}.zip"
#name=Vit.Library
#version=2.5



fileType="${filePath##*.}"
echo "release_name=${name}-${version}" >> $GITHUB_ENV
echo "release_tag=${version}" >> $GITHUB_ENV

echo "release_draft=false" >> $GITHUB_ENV
echo "release_prerelease=true" >> $GITHUB_ENV

echo "release_body=" >> $GITHUB_ENV

echo "release_assetPath=${filePath}" >> $GITHUB_ENV
echo "release_assetName=${name}-${version}.${fileType}" >> $GITHUB_ENV
echo "release_contentType=application/${fileType}" >> $GITHUB_ENV





 
#----------------------------------------------
#(x.9)
cd $curWorkDir