{
  nuget-packageslock2nix,
  pkgs ? import <nixpkgs> {},
}: let
  dotnet = pkgs.dotnetCorePackages.sdk_10_0;
  aspnetcore = pkgs.dotnetCorePackages.aspnetcore_10_0;
in
  with pkgs;
    dotnetCorePackages.buildDotnetModule {
      pname = "splamyweb";
      version = "1.0.0";

      src = ../..;
      projectFile = "SplamyWeb/SplamyWeb.csproj";
      nugetDeps = nuget-packageslock2nix.lib {
        system = "x86_64-linux";
        name = "example";
        lockfiles = [
          ../../SplamyWeb/packages.lock.json
        ];
      };

      dotnet-sdk = dotnet;
      dotnet-aspnetcore = aspnetcore;

      buildInputs = [
        openssl
      ];

      meta = {
        mainProgram = "SplamyWeb";
      };
    }
