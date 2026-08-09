self: {
  config,
  lib,
  pkgs,
  ...
}:
with lib; let
  system = pkgs.stdenv.hostPlatform.system;
  bin-default = self.packages.${system}.splamyweb;
  ui-default = self.packages.${system}.splamyweb-ui;
  cfg = config.services.splamyweb;
  settingsFormat = pkgs.formats.json {};
  mergedSettings = lib.mkMerge [
    {
      ConnectionStrings = {
        DefaultConnection = "Host=/run/postgresql;Database=${cfg.database}";
      };
    }
    cfg.settings
  ];
  configFile = settingsFormat.generate "appsettings.json" mergedSettings;
  configFileFolder = pkgs.linkFarm "content_root" {
    "appsettings.json" = configFile;
  };
in {
  options.services.splamyweb = {
    enable = mkEnableOption "splamyweb";

    package = mkOption {
      type = types.package;
      default = bin-default;
      defaultText = literalExpression "pkgs.splamyweb-backend";
      description = "splamyweb package to use.";
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

    database = lib.mkOption {
      type = types.str;
      default = "splamyweb";
    };
  };

  config = mkIf cfg.enable {
    environment.systemPackages = [cfg.package];

    systemd.services.splamyweb = {
      description = "Splamy's web page";
      after = ["network-online.target"];
      wants = ["network-online.target"];
      wantedBy = ["multi-user.target"];
      restartTriggers = [configFile];

      environment = {
        ASPNETCORE_WEBROOT = "${ui-default}";
        ASPNETCORE_ENVIRONMENT = "Production";
        ASPNETCORE_CONTENTROOT = "${configFileFolder}";
      };

      serviceConfig = {
        Type = "simple";
        User = cfg.user;
        Group = cfg.group;
        WorkingDirectory = cfg.dataDir;
        ExecStart = "${getExe cfg.package}";
        Restart = "on-failure";
        TimeoutSec = 15;
        EnvironmentFile = lib.mkIf (cfg.environmentFile != null) cfg.environmentFile;
        AmbientCapabilities = "CAP_NET_BIND_SERVICE";
        StateDirectory = "splamyweb";
        StateDirectoryMode = "0750";
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

    services.postgresql = mkIf (cfg.database != null) {
      ensureDatabases = [cfg.database];
      ensureUsers = [
        {
          name = cfg.database;
          ensureDBOwnership = true;
        }
      ];
    };
  };
}
