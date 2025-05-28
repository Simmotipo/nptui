# nptui
NetPlan Terminal User Interface

# Requirements
- dotnet8 (On ubuntu 24.04 LTS, simply `apt install dotnet8`)

# Installation and Usage
- Download latest `nptui` executable release.
- Copy it into `/usr/bin/`
- `sudo nptui [optional: /path/to/netplan/file]`

# Roadmap
- Need to neaten up the code lol
- Will host it in my own apt repo at some point

# Changelog
# v1.2 | 28-05-25
- OK now you can also route so yeh

# v1.1 | 28-05-25
- Can configure nameservers now

# v1.0 | 28-05-25
- Can open netplan YAML files
- Will automatically replace 'gateway4' entries with default routes.
- Supports default route (gateway) configuration.