bun install
bun run build
rm -Force -Recurse ..\SplamyWeb\wwwroot\*
New-Item -Name ..\SplamyWeb\wwwroot\.gitkeep -ItemType File
cp -Force -Recurse .\build\* ..\SplamyWeb\wwwroot\
