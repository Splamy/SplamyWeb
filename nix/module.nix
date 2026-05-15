self: {
  config,
  lib,
  pkgs,
  ...
}:
with lib; let
  system = "x86_64-linux";
  bin-default = self.packages.${system}.splamyweb-backed;
  ui-default = self.packages.${system}.splamyweb-ui;
  cfg = config.services.myousync;
  settingsFormat = pkgs.formats.json {};
  configFile = settingsFormat.generate "appsettings.json" configOptions;
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
      default = "splamyweb";
      description = "User to run the service as.";
    };

    group = mkOption {
      type = types.str;
      default = "splamyweb";
      description = "Group to run the service as.";
    };

    dataDir = mkOption {
      type = types.path;
      default = "/var/lib/splamyweb";
      description = "Base data directory";
    };

    environmentFile = lib.mkOption {
      type = types.nullOr types.path;
      default = null;
      example = "/run/secrets/splamyweb";
    };

    settings = lib.mkOption {
      type = types.attrs;
      default = {};
      description = "appsettings json";
    };
  };

  config = mkIf cfg.enable {
    environment.systemPackages = [cfg.package];

    systemd.services.myousync = {
      description = "Splamy's web page";
      after = ["network-online.target"];
      wants = ["network-online.target"];
      wantedBy = ["multi-user.target"];
      restartTriggers = [configFile];

      environment = {};

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

    users.users = mkIf (cfg.user == "splamyweb") {
      splamyweb = {
        inherit (cfg) group;
        isSystemUser = true;
        home = cfg.dataDir;
        createHome = true;
      };
    };

    users.groups = mkIf (cfg.group == "splamyweb") {
      splamyweb = {};
    };
  };
}
