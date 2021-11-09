#!/bin/bash

for i in 1 .. 3
do
	echo 'Loading...'
	sleep 1
done

echo 'Successfull'

while true; do
	sleep 1
	echo 'Keeping alive'
done
