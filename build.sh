#!/bin/bash
ROOT_DIR=$( cd -- "$( dirname -- "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )

FrameworkPathOverride=${ROOT_DIR}/../RimworldManaged dotnet build /property:Configuration=release
