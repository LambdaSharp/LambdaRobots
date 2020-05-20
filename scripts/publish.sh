#!/bin/bash

LAMBDAROBOTS_VERSION=1.2
FOLDER=`pwd`

lash publish \
    --aws-profile lambdasharp \
    --tier Public \
    --force-build \
    --module-version $LAMBDAROBOTS_VERSION \
    $FOLDER/src/Server \
    $FOLDER/src/Robots/HotShotRobot/ \
    $FOLDER/src/Robots/TargetRobot/ \
    $FOLDER/src/Robots/YosemiteSamRobot/
