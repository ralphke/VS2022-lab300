# init-db client image for TinyShop
FROM ubuntu:noble-20260410
# Install necessary dependencies and tools for SQL operations
ENV DEBIAN_FRONTEND=noninteractive
ENV ACCEPT_EULA=Y
ENV PATH="/root/.dotnet/tools:/opt/mssql-tools/bin:${PATH}"

WORKDIR /usr/src/sql
# Copy the SQL initialization script and SQL files into the init directory
COPY . /usr/src/sql/init/
# Make the initialization script executable
RUN chmod +x /usr/src/sql/init/init-db.sh
# Install necessary dependencies for SQL tools and .NET SDK
RUN set -eux; \
    apt-get update; \
    apt-get install -y --no-install-recommends \
        dotnet-sdk-10.0 \
        dotnet-runtime-10.0 \
        curl \
        ca-certificates \
        gnupg \
        apt-transport-https \
        unzip \
        bzip2 \
        libunwind8 \
        libssl3 \
        libstdc++6 \
        unixodbc \
        unixodbc-dev;
# Add Microsoft's GPG key for package verification
RUN set -eux; \
    curl -sSL https://packages.microsoft.com/keys/microsoft-2025.asc | gpg --dearmor > /usr/share/keyrings/microsoft.gpg
# Add the Microsoft repository for SQL tools
RUN set -eux; \
    echo "deb [arch=amd64 signed-by=/usr/share/keyrings/microsoft.gpg] https://packages.microsoft.com/ubuntu/25.10/prod questing main" \
      > /etc/apt/sources.list.d/mssql-release.list
# Update package lists to include the Microsoft repository
# RUN set -eux; \
#    apt-get update
# Install the ODBC driver and SQL tools from Microsoft 
RUN set -eux; \
     curl -fsSL -o /tmp/msodbcsql18.deb \
       https://packages.microsoft.com/ubuntu/25.10/prod/pool/main/m/msodbcsql18/msodbcsql18_18.6.2.1-1_amd64.deb; \
     curl -fsSL -o /tmp/mssql-tools18.deb \
       https://packages.microsoft.com/ubuntu/25.10/prod/pool/main/m/mssql-tools18/mssql-tools18_18.6.2.1-1_amd64.deb; \
     ACCEPT_EULA=${ACCEPT_EULA} apt-get install -y --no-install-recommends /tmp/msodbcsql18.deb /tmp/mssql-tools18.deb; \
     rm -f /tmp/msodbcsql18.deb /tmp/mssql-tools18.deb
# Install the go-sqlcmd tool by downloading the latest release from GitHub and extracting it to /usr/local/bin
RUN set -eux; \
    curl -L https://github.com/microsoft/go-sqlcmd/releases/latest/download/sqlcmd-linux-amd64.tar.bz2 \
    | tar -xjf - -C /usr/local/bin && \
    chmod +x /usr/local/bin/sqlcmd /usr/local/bin/sqlcmd_debug && \
    echo "  sqlcmd $(/usr/local/bin/sqlcmd --version 2>/dev/null || true)"

# Install the Microsoft SQL ODBC driver and SQL command-line tools from the repository.
# This uses package names but is not working as of now - so future releases can be picked up through repo metadata.
# RUN set -eux; \
#    ACCEPT_EULA=${ACCEPT_EULA} apt-get install -y --no-install-recommends \
#        msodbcsql18 \
#        mssql-tools18
# Install the SQLPackage tool using the .NET CLI into a tool path that is present in PATH
RUN set -eux; \
    dotnet tool install --tool-path /root/.dotnet/tools microsoft.sqlpackage
# Verify the installation of SQLPackage
RUN set -eux; \
    /root/.dotnet/tools/sqlpackage /version:true
# Clean up APT cache to reduce image size
RUN rm -rf /var/lib/apt/lists/*

# Run the SQL initialization script to set up the database
# RUN ${WORKDIR}/init-db.sh
