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
