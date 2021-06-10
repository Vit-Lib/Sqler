set -e

sh 20.docker-build.sh

sh 90.release-build.sh

sh 91.release-github.sh
