#!/usr/bin/env bash
cd $(dirname $0)
cd vegeta

RATE=${1:-50}
DURATION=${2:-5s}

echo "Rate = $RATE RPS"
echo "Duration = $DURATION"

# warm up the JIT Compiler
./update-endpoints.sh | vegeta attack -rate=10 -duration=1s > /dev/null 2>&1
# run the actual test
./update-endpoints.sh | vegeta attack -rate="$RATE" -duration="$DURATION" | vegeta report

