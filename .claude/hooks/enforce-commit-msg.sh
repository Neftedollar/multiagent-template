#!/bin/bash
export PATH="$PATH:$HOME/.dotnet/tools"
exec multiagent-setup hook enforce-commit-msg
