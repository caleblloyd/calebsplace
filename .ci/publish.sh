#!/usr/bin/env bash
cd $(dirname $0)

display_usage() {
    echo -e "\nUsage:\n$0 [prod] [version]\n"
}

# check whether user had supplied -h or --help . If yes display usage
if [[ ( $# == "--help") ||  $# == "-h" ]]
then
    display_usage
    exit 0
fi

# check number of arguments
if [ $# -ne 2 ] || ( [ $1 != "prod" ] )
then
    display_usage
    exit 1
fi

# stop on errors
set -e

echo "Publishing to environment: $1 version: $2"

# copy dotnet assets
echo "Building backend Assets"
cd ../build/docker/prod
docker exec -it cp-dev-dotnet app-publish
rm -rf dotnet/stage/dotnet/App
mv ../../../dotnet/src/App/Common/publish dotnet/stage/dotnet/App

# copy nginx assets
echo "Building frontend Assets"
rm -rf nginx/stage/*
cp -r ../dev/nginx/stage/* nginx/stage/*
mkdir -p nginx/stage/var/www
cp -r ../../../ui/* nginx/stage/var/www

# build images
echo "Building Images"
export TAG=$2
docker-compose build

# push docker images
gcloud docker -- push gcr.io/caleb-lloyd/calebsplace-dotnet:$2
gcloud docker -- push gcr.io/caleb-lloyd/calebsplace-nginx:$2

# deploy to kubernetes
kubectl set image deployment/calebsplace-$1 \
    dotnet=gcr.io/caleb-lloyd/calebsplace-dotnet:$2 \
    nginx=gcr.io/caleb-lloyd/calebsplace-nginx:$2
