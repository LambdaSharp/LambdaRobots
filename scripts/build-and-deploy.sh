#!/bin/bash

# TODO: don't use this
LAMBDAROBOTS_VERSION=2.0-DEV
FOLDER=`pwd`

dotnet test \
    $FOLDER/Tests/Test.LambdaRobots.Game/Test.LambdaRobots.Game.csproj
if [ $? -ne 0 ]; then
    exit $?
fi

lash deploy \
    --module-version $LAMBDAROBOTS_VERSION \
    --allow-data-loss \
    $FOLDER/src/Game \
    $FOLDER/src/Bots/HotShotBot/ \
    $FOLDER/src/Bots/TargetBot/ \
    $FOLDER/src/Bots/YosemiteSamBot/ \
    $FOLDER/src/Bots/BringYourOwnBot/
