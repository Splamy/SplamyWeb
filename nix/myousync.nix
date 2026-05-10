self: {
  config,
  lib,
  pkgs,
  ...
}:
with lib; let
  system = "x86_64-linux";
  bin-default = self.packages.${system}.myousync;
  ui-default = self.packages.${system}.myousync-ui;
  cfg = config.services.myousync;
  settingsFormat = pkgs.formats.toml {};
  configOptions =    lib.recursiveUpdate    (    )    cfg.settings;
  configFile = settingsFormat.generate "myousync.toml" configOptions;
in {
  options.services.myousync = {
    enable = mkEnableOption "myousync";

    package = mkOption {
      type = types.package;
      default = bin-default;
      defaultText = literalExpression "pkgs.myousync";
      description = "myousync package to use.";
    };

    user = mkOption {
      type = types.str;
      default = "myousync";
      description = "User to run the service as.";
    };

    group = mkOption {
      type = types.str;
      default = "myousync";
      description = "Group to run the service as.";
    };

    dataDir = mkOption {
      type = types.path;
      default = "/var/lib/myousync";
      description = ''
        Base data directory,
      '';
    };

    environmentFile = lib.mkOption {
      type = types.nullOr types.path;
      default = null;
      example = "/run/secrets/myousync";
      description = ''
        To set the youtube auth data, point `environmentFile` at a file containing:
        ```
        EXAMPLE=VALUE
        ```
      '';
    };

    settings = lib.mkOption {
      type = types.attrs;
      default = {};
      description = ''
        The root myousync.toml configuration. Nix specific config will overwrite values in this.
      '';
    };
  };

  config = mkIf cfg.enable {
    environment.systemPackages = [cfg.package];

    systemd.services.myousync = {
      description = "myousync music syncronization service";
      after = ["network-online.target"];
      wants = ["network-online.target"];
      wantedBy = ["multi-user.target"];
      restartTriggers = [configFile];

      environment = {
        RUST_BACKTRACE = "1"; # TODO remove
      };

      serviceConfig = {
        Type = "simple";
        User = cfg.user;
        Group = cfg.group;
        WorkingDirectory = cfg.dataDir;
        # ExecStart = "${getExe cfg.package}";
        ExecStart = "${getExe cfg.package} ${configFile}";
        Restart = "on-failure";
        TimeoutSec = 15;
        EnvironmentFile = lib.mkIf (cfg.environmentFile != null) cfg.environmentFile;
      };
    };

    users.users = mkIf (cfg.user == "myousync") {
      myousync = {
        inherit (cfg) group;
        isSystemUser = true;
        home = cfg.dataDir;
        createHome = true;
      };
    };

    users.groups = mkIf (cfg.group == "myousync") {
      myousync = {};
    };

    networking.firewall = mkIf cfg.openFirewall {
      allowedTCPPorts = [
        cfg.port
      ];
    };
  };
}
