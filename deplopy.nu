dotnet publish SplamyWeb -c Release --sc -r linux-x64 /p:PublishSingleFile=true -o ./publish
rclone copy ./publish splamy_web:splamy_web -P
