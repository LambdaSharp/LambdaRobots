#!/bin/bash

LAMBDAROBOTS_VERSION=2.0-DEV
FOLDER=`pwd`

dotnet test \
    $FOLDER/Tests/Test.LambdaRobots.Server/Test.LambdaRobots.Server.csproj
if [ $? -ne 0 ]; then
    exit $?
fi

lash deploy \
    --module-version $LAMBDAROBOTS_VERSION \
    --allow-data-loss \
    $FOLDER/src/Server \
    $FOLDER/src/Bots/HotShotBot/ \
    $FOLDER/src/Bots/TargetBot/ \
    $FOLDER/src/Bots/YosemiteSamBot/
