{pkgs ? import <nixpkgs> {}}:
  pkgs.buildNpmPackage (finalAttrs: {
    pname = "splamyweb-js";
    version = "1.0.0";

    src = ./../../splamyweb_js/.;
    npmDeps = pkgs.runCommand "bun-lock-conversion" {} ''
      mkdir -p $out
      ${pkgs.bun}/bin/bun install --frozen-lockfile --bun false
      cp -r node_modules $out/node_modules
      echo '{}' > $out/package-lock.json
    '';

    # Use bun for build since the project uses bun.lock
    installPhase = ''
      runHook preInstall

      mkdir -p $out
      ${pkgs.bun}/bin/bun run build
      cp -R build/* $out

      runHook postInstall
    '';

    meta = {
      description = "splamyweb-js static pages";
    };
  })
