#!/bin/bash

LAMBDAROBOTS_VERSION=2.0
FOLDER=`pwd`

lash publish \
    --aws-profile lambdasharp \
    --tier Release \
    --force-build \
    --module-version $LAMBDAROBOTS_VERSION \
    $FOLDER/src/Server \
    $FOLDER/src/Bots/HotShotBot/ \
    $FOLDER/src/Bots/TargetBot/ \
    $FOLDER/src/Bots/YosemiteSamBot/
