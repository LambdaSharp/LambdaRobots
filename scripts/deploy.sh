#!/bin/bash

LAMBDAROBOTS_VERSION=1.2

lash deploy \
    LambdaRobots.Server:$LAMBDAROBOTS_VERSION@lambdasharp \
    LambdaRobots.HotShotRobot:$LAMBDAROBOTS_VERSION@lambdasharp \
    LambdaRobots.YosemiteSamRobot:$LAMBDAROBOTS_VERSION@lambdasharp \
    LambdaRobots.TargetRobot:$LAMBDAROBOTS_VERSION@lambdasharp
