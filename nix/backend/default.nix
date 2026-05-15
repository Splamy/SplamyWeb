{pkgs ? import <nixpkgs> {}}: let
  dotnet = pkgs.dotnetCorePackages.sdk_10_0;
  aspnetcore = pkgs.dotnetCorePackages.aspnetcore_10_0;
in
  with pkgs; {
    bin = dotnetCorePackages.buildDotnetModule {
      pname = "splamyweb";
      version = "1.0.0";

      src = ../..;
      projectFile = "SplamyWeb/SplamyWeb.csproj";
      nugetDeps = ./deps.json;

      # dotnetRestoreFlags = ["--use-lock-file" "--locked-mode"];

      dotnet-sdk = dotnet;
      dotnet-aspnetcore = aspnetcore;

      buildInputs = [
        openssl
      ];

      meta = {
        mainProgram = "SplamyWeb";
      };
    };

    updater = pkgs.writeShellApplication {
      name = "update-deps";
      text = ''
        ${pkgs.lib.getExe dotnet} restore --packages SplamyWeb/bin/out
        ${pkgs.lib.getExe pkgs.nuget-to-json} SplamyWeb/bin/out > nix/backend/deps.json
        echo "Updated nix/backend/deps.json"
      '';
    };
  }
