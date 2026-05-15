{
  description = "Splamyweb";
  inputs = {
    nixpkgs.url = "github:nixos/nixpkgs/nixos-unstable";
    flake-utils.url = "github:numtide/flake-utils";
	nuget-packageslock2nix = {
	  url = "github:mdarocha/nuget-packageslock2nix";
	  inputs.nixpkgs.follows = "nixpkgs";
	};
  };

  outputs = {
    self,
    nixpkgs,
    flake-utils,
    nuget-packageslock2nix,
    ...
  }:
    flake-utils.lib.eachDefaultSystem (
      system: let
        pkgs = (import nixpkgs) {
          inherit system;
        };
        splamyweb-ui = pkgs.callPackage ./nix/frontend/. {};
        splamyweb-backend = pkgs.callPackage ./nix/backend/. { };
      in rec {
        defaultPackage = packages.myousync;

        packages.splamyweb = splamyweb-backend;
        packages.splamyweb-ui = splamyweb-ui;

        devShells.default = pkgs.callPackage ./nix/shell.nix {};
      }
    )
    // {
      nixosModules.splamyweb = import ./nix/module.nix self;
    };
}
