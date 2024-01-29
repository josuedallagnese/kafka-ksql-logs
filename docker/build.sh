#!/usr/bin/bash

cd ../
docker image rm account-web
docker build -t account-web .