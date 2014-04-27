#!/bin/sh
#Compiles the xbuild solution.
cd `dirname $0`

#Handle Windows-style paths in project files
export MONO_IOMAP=all

#Restore NuGet packages
mono --runtime=v4.0 .nuget/NuGet.exe restore NanoByte.Common_MonoDevelop.sln

xbuild NanoByte.Common_MonoDevelop.sln /nologo /v:q
