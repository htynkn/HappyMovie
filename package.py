#!/usr/bin/env python
import xml.etree.ElementTree as ET
from datetime import datetime
from pathlib import Path
from hashlib import md5
import json
import re
import subprocess
import shutil

tree = ET.parse("Jellyfin.Plugin.HappyMovie/Jellyfin.Plugin.HappyMovie.csproj")
version = tree.find("./PropertyGroup/AssemblyVersion").text
targetAbi = tree.find("./ItemGroup/*[@Include='Jellyfin.Model']").attrib["Version"]
targetAbi = re.sub("-\w+", "", targetAbi) # Remove trailing release candidate version.
timestamp = datetime.now().strftime("%Y-%m-%dT%H:%M:%SZ")

meta = {
    "category": "Metadata",
    "guid": "a3a07da4-ae5a-4d4a-a843-5aa7e3ba0a62",
    "name": "HappyMovie",
    "description": "Get metadata from tmdb.",
    "owner": "htynkn",
    "overview": "Get metadata from tmdb.",
    "targetAbi": f"{targetAbi}.0",
    "timestamp": timestamp,
    "version": version
}

Path(f"release/{version}").mkdir(parents=True, exist_ok=True)
print(json.dumps(meta, indent=4), file=open(f"release/{version}/meta.json", "w"))

subprocess.run([
    "dotnet",
    "build",
    "Jellyfin.Plugin.HappyMovie/Jellyfin.Plugin.HappyMovie.csproj",
    "--configuration",
    "Release"
])

shutil.copy("Jellyfin.Plugin.HappyMovie/bin/Release/net8.0/Jellyfin.Plugin.HappyMovie.dll", f"release/{version}/")
shutil.copy(f"{Path.home()}/.nuget/packages/yove.proxy/1.1.1/lib/netstandard2.0/Yove.Proxy.dll", f"release/{version}/")
shutil.copy(f"{Path.home()}/.nuget/packages/tmdblib/2.2.0/lib/netstandard2.0/TMDbLib.dll", f"release/{version}/")

shutil.make_archive(f"release/happymovie_{version}", "zip", f"release/{version}/")

entry = {
    "checksum": md5(open(f"release/happymovie_{version}.zip", "rb").read()).hexdigest(),
    "changelog": "",
    "targetAbi": f"{targetAbi}.0",
    "sourceUrl": f"https://github.com/htynkn/HappyMovie/releases/download/{version}/happymovie_{version}.zip",
    "timestamp": timestamp,
    "version": version
}

manifest = json.loads(open("manifest.json", "r").read())
manifest[0]["versions"].insert(0, entry)
print(json.dumps(manifest, indent=4), file=open("manifest.json", "w"))
