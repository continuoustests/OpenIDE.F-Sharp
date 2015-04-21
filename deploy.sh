#!/bin/bash

ROOT=$(cd $(dirname "$0"); pwd)
BINARYDIR=$(cd $(dirname "$0"); pwd)/build_output
DEPLOYDIR=$(cd $(dirname "$0"); pwd)/ReleaseBinaries
LIB=$(cd $(dirname "$0"); pwd)/lib

if [ -d $BINARYDIR ]; then
{
    rm -r $BINARYDIR/
}
fi
if [ -d $DEPLOYDIR ]; then
{
    rm -r $DEPLOYDIR/
}
fi

mkdir $BINARYDIR
mkdir $DEPLOYDIR

mkdir $DEPLOYDIR/f#-files
mkdir $DEPLOYDIR/f#-files/preserved-data
mkdir $DEPLOYDIR/f#-files/preserved-data/create
mkdir $DEPLOYDIR/f#-files/preserved-data/new
mkdir $DEPLOYDIR/f#-files/preserved-data/script-template
mkdir $DEPLOYDIR/f#-files/preserved-data/rscript-template

xbuild src/OpenIDE.F-Sharp.sln /target:rebuild /property:OutDir=$BINARYDIR/ /p:Configuration=Release;

cp $ROOT/resources/f#.oilnk $DEPLOYDIR/f#.oilnk
cp $ROOT/resources/package.json $DEPLOYDIR/f#-files/package.json
cp $BINARYDIR/f#.exe $DEPLOYDIR/f#-files/f#.exe
cp -r $ROOT/resources/templates/script/* $DEPLOYDIR/f#-files/preserved-data/script-template
cp $BINARYDIR/build.exe $DEPLOYDIR/f#-files/preserved-data/script-template
cp -r $ROOT/resources/templates/rscript/* $DEPLOYDIR/f#-files/preserved-data/rscript-template
cp $BINARYDIR/build.exe $DEPLOYDIR/f#-files/preserved-data/rscript-template

cp -r $ROOT/resources/create/* $DEPLOYDIR/f#-files/preserved-data/create
cp -r $ROOT/resources/new/* $DEPLOYDIR/f#-files/preserved-data/new


# Building packages
echo "Building packages.."
oi package build $DEPLOYDIR/f\# $DEPLOYDIR
