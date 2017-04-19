#!/usr/bin/env bash
cd $(dirname $0)
cd vegeta

RATE=${1:-50}
DURATION=${2:-5s}

echo "Rate = $RATE RPS"
echo "Duration = $DURATION"

echo "GET http://dockerhost:48010/api/pixels/sse/0001-01-01T00:00:01" | vegeta attack -rate="$RATE" -duration="$DURATION" | vegeta report

