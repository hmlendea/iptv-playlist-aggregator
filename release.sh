#!/bin/bash
DOTNET_VERSION="5.0"
RELEASE_SCRIPT_URL="https://raw.githubusercontent.com/hmlendea/deployment-scripts/master/release/dotnet/${DOTNET_VERSION}.sh"
wget --quiet -O - "${RELEASE_SCRIPT_URL}" | bash /dev/stdin ${@}
