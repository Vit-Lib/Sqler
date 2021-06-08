set -e

# cd /root/docker/jenkins/workspace/sqler/svn/Publish/DevOps/sqler; bash 30.release-build.sh


# export GIT_SSH_SECRET=xxxxxx

#(x.1)当前路径 
curWorkDir=$PWD
curPath=$(dirname $0)

cd $curPath/../../..
codePath=$PWD
# codePath=/root/docker/jenkins/workspace/sqler/svn


echo "(x.2)get version"
#version=1.1.0.53
cd $codePath
version=`grep '<Version>' Sqler/Sqler.csproj | grep -o '[0-9\.]\+'`

# echo $version


#----------------------------------------------
echo "(x.2)构建最终文件夹"
mkdir -p $codePath/Publish/Sqler
mkdir -p $codePath/Publish/git

cp -rf  $codePath/Publish/04.服务站点 $codePath/Publish/Sqler/04.服务站点
cp -rf  $codePath/Publish/06.Docker $codePath/Publish/Sqler/06.Docker
cp -rf  $codePath/Publish/06.Docker/制作镜像/sqler/app $codePath/Publish/Sqler/04.服务站点/Sqler

 
docker run --rm -i \
-v $codePath/Publish:/root/file \
serset/filezip dotnet FileZip.dll zip -i /root/file/Sqler -o /root/file/git/Sqler${version}.zip

 

#----------------------------------------------
echo "(x.3)提交release文件到github"
# releaseFile=$codePath/Publish/git/Sqler${version}.zip

#复制ssh key
cd $codePath/Publish
echo "${GIT_SSH_SECRET}" > $codePath/Publish/git/serset
chmod 600 $codePath/Publish/git/serset

#推送到github
docker run -i --rm -v $PWD/git:/root/git serset/git-client bash -c " \
set -e
ssh-agent bash -c \"
ssh-add /root/git/serset
ssh -T git@github.com -o StrictHostKeyChecking=no
git config --global user.email 'serset@yeah.com'
git config --global user.name 'lith'
mkdir -p /root/code
cd /root/code
git clone git@github.com:serset/release.git /root/code
mkdir -p /root/code/sqler
cp /root/git/Sqler${version}.zip /root/code/sqler
git add sqler/Sqler${version}.zip
git commit  -m  'auto commit ${version}'
git push -u origin master \" "




#(x.5)
cd $curWorkDir

 
