#! /bin/sh

CSC=$(which mono-csc || which dmcs)
$CSC /out:TestSSLServer.exe /main:TestSSLServer Src/*.cs Asn1/*.cs X500/*.cs

#python 32745.py 192.168.208.3 443
#$ sudo chmod u+x 32745.py
#$ ./32745.py 192.168.208.3 443


