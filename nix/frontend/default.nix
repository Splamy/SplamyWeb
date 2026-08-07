{pkgs ? import <nixpkgs> {}}:
pkgs.buildNpmPackage (finalAttrs: {
  pname = "splamyweb-js";
  version = "1.0.0";

  src = ./../../splamyweb_js/.;

  npmDepsHash = "sha256-tA+o+fT6+vYpxN7MQWj9q9G3NzktfD4xihrU9c6eLnI=";
  npmPackFlags = ["--ignore-scripts"];

  installPhase = ''
    runHook preInstall

    mkdir -p "$out"
    cp -R build/. "$out/"

    runHook postInstall
  '';

  meta = {
    description = "splamyweb-js static pages";
  };
})
