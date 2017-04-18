#!/bin/bash

for y in $(shuf -i 0-999 -n 100)
do
  for x in $(shuf -i 0-999 -n 100)
  do
    echo "POST http://dockerhost:48010/api/pixels/$x/$y"
    echo "Content-Type: application/json"
    echo "@color.json"
  done
done
