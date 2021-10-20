#!/bin/bash

LAMBDAROBOTS_VERSION=2.0

lash deploy \
    LambdaRobots.Server:$LAMBDAROBOTS_VERSION@lambdasharp \
    LambdaRobots.HotShotBot:$LAMBDAROBOTS_VERSION@lambdasharp \
    LambdaRobots.YosemiteSamBot:$LAMBDAROBOTS_VERSION@lambdasharp \
    LambdaRobots.TargetBot:$LAMBDAROBOTS_VERSION@lambdasharp
