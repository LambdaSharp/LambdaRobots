#!/bin/bash

FOLDER=`pwd`

lash deploy \
    $FOLDER/src/Server \
    $FOLDER/src/Robots/HotShotRobot/ \
    $FOLDER/src/Robots/TargetRobot/ \
    $FOLDER/src/Robots/YosemiteSamRobot/
