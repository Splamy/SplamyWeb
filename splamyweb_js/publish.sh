#!/bin/bash
yarn install
yarn build
rm -rf ../SplamyWeb/wwwroot/*
touch ../SplamyWeb/wwwroot/.gitkeep
cp -r ./build/* ../SplamyWeb/wwwroot/
