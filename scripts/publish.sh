#!/bin/bash

LAMBDAROBOTS_VERSION=2.0
FOLDER=`pwd`

lash publish \
    --aws-profile lambdasharp \
    --tier Release \
    --force-build \
    --module-version $LAMBDAROBOTS_VERSION \
    $FOLDER/src/Server \
    $FOLDER/src/Robots/HotShotRobot/ \
    $FOLDER/src/Robots/TargetRobot/ \
    $FOLDER/src/Robots/YosemiteSamRobot/
