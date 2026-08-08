{pkgs ? import <nixpkgs> {}}: let
  dotnet = pkgs.dotnetCorePackages.sdk_10_0;
  aspnetcore = pkgs.dotnetCorePackages.aspnetcore_10_0;
  runtimeId = pkgs.dotnetCorePackages.systemToDotnetRid pkgs.stdenv.hostPlatform.system;
in
  with pkgs; {
    bin = dotnetCorePackages.buildDotnetModule {
      pname = "splamyweb";
      version = "1.0.0";

      src = ../..;
      projectFile = "SplamyWeb/SplamyWeb.csproj";
      nugetDeps = ./deps.json;

      dotnetRestoreFlags = ["--force-evaluate"];

      dotnet-sdk = dotnet;
      dotnet-aspnetcore = aspnetcore;
      dotnet-runtime = aspnetcore;

      buildInputs = [
        openssl
      ];

      meta = {
        mainProgram = "SplamyWeb";
      };
    };

    updater = pkgs.writeShellApplication {
      name = "update-deps";
      runtimeInputs = [
        dotnet
        pkgs.coreutils
        pkgs.nuget-to-json
      ];
      text = ''
        packages=$(mktemp -d)
        deps=$(mktemp)
        trap 'rm -rf "$packages" "$deps"' EXIT

        dotnet restore SplamyWeb/SplamyWeb.csproj \
          -p:ContinuousIntegrationBuild=true \
          -p:Deterministic=true \
          -p:NuGetAudit=false \
          --runtime ${runtimeId} \
          --force-evaluate \
          --packages "$packages"
        nuget-to-json "$packages" > "$deps"
        mv "$deps" nix/backend/deps.json
      '';
    };
  }
