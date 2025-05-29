# nptui
NetPlan Terminal User Interface

# Requirements
- dotnet8 (On ubuntu 24.04 LTS, simply `apt install dotnet8`)

# Installation and Usage
- Download latest `nptui` executable release.
- Copy it into `/usr/bin/` (optional)
- `sudo nptui [optional: /path/to/netplan/file]`

# Roadmap
- Need to neaten up the code lol
- Will host it in my own apt repo at some point
- Better error handling!

  https://github.com/Simmotipo/nptui/issues/12

# Changelog
# v1.4 | 29-05-25
- Fix bugs with setting invalid/empty metrics on gateway creation/edit - [#1](https://github.com/Simmotipo/nptui/issues/1) and [#3](https://github.com/Simmotipo/nptui/issues/3)
- Fix bug where colon after interface name was not added into yaml - [#10](https://github.com/Simmotipo/nptui/issues/10)
- Fix IndexOutOfRangeException when loading routes at end of file which have no metric - [#11](https://github.com/Simmotipo/nptui/issues/11)
- Strip spaces inadvertently loaded in as a part of the route loading (which could lead to default route / gateway not being detected) - [#12](https://github.com/Simmotipo/nptui/issues/12)
- If we fail to save, dump file to /tmp/nptui.bak just in case

# v1.3 | 28-05-25
- Tell user to sudo if cannot read/write file.

# v1.2 | 28-05-25
- OK now you can also route so yeh

# v1.1 | 28-05-25
- Can configure nameservers now

# v1.0 | 28-05-25
- Can open netplan YAML files
- Will automatically replace 'gateway4' entries with default routes.
- Supports default route (gateway) configuration.
