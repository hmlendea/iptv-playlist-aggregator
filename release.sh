#!/bin/bash
DOTNET_VERSION="8.0"
RELEASE_SCRIPT_URL="https://raw.githubusercontent.com/hmlendea/deployment-scripts/master/release/dotnet/${DOTNET_VERSION}.sh"
curl -sSL "${RELEASE_SCRIPT_URL}" | bash -s ${1} --no-trim