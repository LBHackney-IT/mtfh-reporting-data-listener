#!/bin/bash

while [[ "$(curl -v -s -o /dev/null -w ''%{http_code}'' http://schemaregistry:8081)" != "200" ]];
do sleep 5;
done