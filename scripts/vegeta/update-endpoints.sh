#!/bin/bash

do
  y=$(printf "%03d" $y)
  for x in $(shuf -i 0-999 -n 100)
  do
    x=$(printf "%03d" $x)
    echo "POST http://dockerhost:48010/api/pixels/$x/$y"
    echo "Content-Type: application/json"
    echo "@color.json"
  done
done
