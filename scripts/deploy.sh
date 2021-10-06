#!/bin/bash

LAMBDAROBOTS_VERSION=2.0

lash deploy \
    LambdaRobots.Server:$LAMBDAROBOTS_VERSION@lambdasharp \
    LambdaRobots.HotShotRobot:$LAMBDAROBOTS_VERSION@lambdasharp \
    LambdaRobots.YosemiteSamRobot:$LAMBDAROBOTS_VERSION@lambdasharp \
    LambdaRobots.TargetRobot:$LAMBDAROBOTS_VERSION@lambdasharp
