# nptui
NetPlan Terminal User Interface

# Installation and Usage
## Method 1: Via apt:
```
sudo mkdir -p /etc/apt/keyrings
wget -O- http://repo.rakico.xyz/ubuntu/repo-rakico-xyz.gpg | gpg --dearmor | sudo tee /etc/apt/keyrings/repo-rakico-xyz.gpg > /dev/null ; sudo chmod 644 /etc/apt/keyrings/repo-rakico-xyz.gpg
echo "deb [signed-by=/etc/apt/keyrings/repo-rakico-xyz.gpg] http://repo.rakico.xyz/ubuntu stable main" | sudo tee /etc/apt/sources.list.d/rakico.list
sudo apt install nptui
```

## Method 2: Manually
- Download latest `nptui` executable release and transfer to device.
- Copy it into `/usr/bin/` (optional)
- `chmod +x nptui` / `chmod +x /usr/bin/nptui`
- `sudo nptui [optional: /path/to/netplan/file]`

# Roadmap
- Need to neaten up the code lol
- Will host it in my own apt repo at some point
- Better error handling!

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
