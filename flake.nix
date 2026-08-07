{
  description = "Splamyweb";
  inputs = {
    nixpkgs.url = "github:nixos/nixpkgs/nixos-unstable";
    flake-utils.url = "github:numtide/flake-utils";
  };

  outputs = {
    self,
    nixpkgs,
    flake-utils,
    ...
  }:
    flake-utils.lib.eachDefaultSystem (
      system: let
        pkgs = (import nixpkgs) {
          inherit system;
        };
        splamyweb-ui = pkgs.callPackage ./nix/frontend/. {};
        splamyweb-backend = pkgs.callPackage ./nix/backend/. {
          inherit pkgs;
        };
      in rec {
        packages.default = packages.splamyweb;

        packages.splamyweb = splamyweb-backend.bin;
        packages.splamyweb-ui = splamyweb-ui;

        packages.update-deps = splamyweb-backend.updater;

        devShells.default = pkgs.callPackage ./nix/shell.nix {};
      }
    )
    // {
      nixosModules.splamyweb = import ./nix/module.nix self;
    };
}
