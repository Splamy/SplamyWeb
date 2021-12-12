yarn build
rm -Force -Recurse ..\SplamyWeb\wwwroot\*
cp -Force -Recurse .\build\* ..\SplamyWeb\wwwroot\
