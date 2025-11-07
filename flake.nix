{
  description = "F# Assistant Agent with LiteLLM and Elmish UI";

  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";
    flake-utils.url = "github:numtide/flake-utils";
  };

  outputs = { self, nixpkgs, flake-utils }:
    flake-utils.lib.eachDefaultSystem (system:
      let
        pkgs = import nixpkgs {
          inherit system;
        };

        # Python environment with LiteLLM
        pythonEnv = pkgs.python311.withPackages (ps: with ps; [
          litellm
          uvicorn
          fastapi
        ]);

        # F# development tools
        dotnetPkg = pkgs.dotnet-sdk_8;

      in
      {
        devShells.default = pkgs.mkShell {
          buildInputs = with pkgs; [
            # .NET for F#
            dotnetPkg

            # Node.js for Fable/Elmish
            nodejs_20
            nodePackages.npm

            # Python for LiteLLM
            pythonEnv

            # Database
            sqlite

            # Whitelisted utilities
            tldr
            tree
            git

            # Development tools
            curl
            jq
            watchexec  # For auto-reloading during development
          ];

          shellHook = ''
            echo "🤖 F# Assistant Agent Development Environment"
            echo "=============================================="
            echo ""
            echo "Tools available:"
            echo "  • dotnet $(dotnet --version)"
            echo "  • node $(node --version)"
            echo "  • python $(python --version)"
            echo "  • sqlite3 $(sqlite3 --version | head -n1)"
            echo ""
            echo "Commands:"
            echo "  • 'dotnet run --project src/Agent.Web' - Start backend"
            echo "  • 'npm run start' - Start frontend dev server"
            echo "  • 'python -m litellm' - Start LiteLLM proxy"
            echo ""

            # Set up .NET tools
            export DOTNET_ROOT="${dotnetPkg}"
            export DOTNET_CLI_TELEMETRY_OPTOUT=1

            # Create data directory
            mkdir -p data
          '';
        };

        packages.default = pkgs.stdenv.mkDerivation {
          pname = "assistant-agent";
          version = "0.1.0";
          src = ./.;

          buildInputs = [ dotnetPkg ];

          buildPhase = ''
            dotnet publish src/Agent.Web/Agent.Web.fsproj \
              -c Release \
              -o $out/bin \
              --self-contained false
          '';

          installPhase = ''
            mkdir -p $out
            cp -r $out/bin $out/
          '';
        };
      }
    );
}
