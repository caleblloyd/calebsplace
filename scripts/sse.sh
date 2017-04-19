#!/usr/bin/env bash
cd $(dirname $0)
cd vegeta

RATE=${1:-50}
DURATION=${2:-5s}

echo "Rate = $RATE RPS"
echo "Duration = $DURATION"

echo "GET http://dockerhost:48010/api/pixels/sse?limit=20" | vegeta attack -rate="$RATE" -duration="$DURATION" | vegeta report

