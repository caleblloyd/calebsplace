#!/usr/bin/env bash
cd $(dirname $0)

set -e

docker exec -it cp-dev-dotnet app-test
