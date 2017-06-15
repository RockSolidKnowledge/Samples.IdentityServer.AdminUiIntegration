#!/bin/bash
db=$1
if [ "$#" -ne 1 ]
  then
    db="db"
fi
./wait-for-it.sh $db:3306 && dotnet Rsk.Samples.IdentityServer4.AdminUiIntegration.dll