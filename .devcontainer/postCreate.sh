#!/bin/bash
set -euo pipefail

echo ""
echo "=== TinyShop devcontainer setup ==="
echo ""

# ── Aspire CLI ──────────────────────────────────────────────────────────────
echo "▶ Installing Aspire CLI..."
curl -sSL https://aspire.dev/install.sh | bash

# Make aspire available in this script session immediately.
# The installer adds it to ~/.bashrc for future shells.
export PATH="$HOME/.aspire/bin:$PATH"

echo "  aspire $(aspire --version)"

# ── NuGet packages ──────────────────────────────────────────────────────────
echo ""
echo "▶ Restoring NuGet packages..."
dotnet restore src/TinyShop.sln --verbosity minimal

# ── Local dotnet tools (reportgenerator, etc.) ──────────────────────────────
echo ""
echo "▶ Restoring local dotnet tools..."
dotnet tool restore --tool-manifest src/.config/dotnet-tools.json

# ── go-sqlcmd SQL Server CLI ─────────────────────────────────────────────────
echo ""
echo "▶ Installing go-sqlcmd SQL Server CLI..."
sudo apt-get update -qq
sudo apt-get install -y --no-install-recommends \
    apt-transport-https \
    bzip2 \
    ca-certificates \
    curl \
    gnupg \
    unixodbc

curl -L https://github.com/microsoft/go-sqlcmd/releases/latest/download/sqlcmd-linux-amd64.tar.bz2 \
    | sudo tar -xjf - -C /usr/local/bin
sudo chmod +x /usr/local/bin/sqlcmd /usr/local/bin/sqlcmd_debug

echo "  sqlcmd $(/usr/local/bin/sqlcmd --version 2>/dev/null || true)"

echo ""
echo "▶ Configuring Docker socket access..."
REMOTE_USER="${REMOTE_USER:-$(id -un)}"
if getent group docker >/dev/null 2>&1; then
  echo "Adding ${REMOTE_USER} to docker group..."
  sudo usermod --append --groups docker "${REMOTE_USER}"
  sudo chown root:docker /var/run/docker.sock
  sudo chmod 660 /var/run/docker.sock
  echo "  User added to docker group. Rebuild/reopen the container if Docker access still fails."
else
  echo "  Docker group not found; socket access may still require sudo."
fi

# ── Dev certificate ─────────────────────────────────────────────────────────
echo ""
echo "▶ Trusting development certificate..."
# This may not fully work inside a container but avoids errors when running apps.
dotnet dev-certs https --trust 2>/dev/null || \
    echo "  Note: dev-cert trust skipped (normal in containers — HTTP ports are used instead)"

# ── Done ────────────────────────────────────────────────────────────────────
echo ""
echo "=== Setup complete! ==="
echo ""
echo "To start the application:"
echo "  aspire run"
echo ""
echo "Ports (HTTP):"
echo "  Aspire Dashboard → http://localhost:15218"
echo "  Store (Blazor)   → http://localhost:5158"
echo "  Products API     → http://localhost:5228"
echo ""