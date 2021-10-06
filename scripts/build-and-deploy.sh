#!/bin/bash

LAMBDAROBOTS_VERSION=2.0-DEV
FOLDER=`pwd`

lash deploy \
    --module-version $LAMBDAROBOTS_VERSION \
    $FOLDER/src/Server \
    $FOLDER/src/Robots/HotShotRobot/ \
    $FOLDER/src/Robots/TargetRobot/ \
    $FOLDER/src/Robots/YosemiteSamRobot/
