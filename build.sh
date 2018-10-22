#!/bin/sh
set -e

TARGET="Debug"
TARGET_UNSIGNED="$TARGET"
if [ "$1" = "release" ]; then
  TARGET="Release"
  TARGET_UNSIGNED="Release-Unsigned"
fi

BUILD_BASE="build"
BUILD_MTG="MTG-DIST"

BUILD="$BUILD_BASE/$BUILD_MTG"
BUILD_ZIP="$BUILD_BASE/$BUILD_MTG.zip"

rm -rf $BUILD_BASE
mkdir -p $BUILD

xbuild /p:Configuration=$TARGET

deps=()

while read line; do
  if [[ "$line" == "#"* ]]; then continue; fi
  deps+=("$(echo "$line" | sed -e "s/{TARGET}/$TARGET/" -e "s/{TARGET-UNSIGNED}/$TARGET_UNSIGNED/")")
done < build-files

echo "${deps[@]}"

for d in ${deps[@]}; do
  cp -r "$d" "$BUILD"
done

zip_name="$(mktemp -u -p "." .build-XXXXXXXXX.zip)"
pushd $BUILD
zip -r "$zip_name" *
popd
mv "$BUILD/$zip_name" "$BUILD_ZIP"
