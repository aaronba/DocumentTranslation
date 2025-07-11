#!/bin/bash

# Wrapper script to run doctr without console input issues
# This approach uses nohup and stdbuf to completely detach from console

cd "$(dirname "$0")"/../DocumentTranslation.CLI/bin/Debug/net8.0

# Set environment variables
export DOTNET_CONSOLE="false"
export CI="true"
export TERM="dumb"

# Use stdbuf to disable buffering and nohup to detach from terminal
# Redirect all input from /dev/null and capture output
stdbuf -i0 -o0 -e0 nohup ./doctr "$@" < /dev/null 2>&1
