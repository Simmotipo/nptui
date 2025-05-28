# nptui
NetPlan Terminal User Interface

# Requirements
- dotnet8 (On ubuntu 24.04 LTS, simply `apt install dotnet8`)

# Usage
- sudo dotnet nptui.dll [optional: /path/to/netplan/file]

It is recommended you set up an executable file(t au mignon) at `/usr/bin/nptui` with the following, to allow use of nptui as a command:
    !/bin/bash
    sudo dotnet /path/to/nptui.dll $1


# Roadmap
- Does not yet support creation or edit of general routes (supports loading/preserving them, but does not show them in UI, or allow editing of them)

# Changelog
# v1.1 | 28-05-25
- Can configure nameservers now

# v1.0 | 28-05-25
- Can open netplan YAML files
- Will automatically replace 'gateway4' entries with default routes.
- Supports default route (gateway) configuration.