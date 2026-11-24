"""Install bundled TMP essentials under HanaMart, preserving Unity asset GUIDs."""
from pathlib import Path
import tarfile

root = Path(__file__).resolve().parents[1]
packages = list((root / "Library/PackageCache").glob("com.unity.ugui@*/Package Resources/TMP Essential Resources.unitypackage"))
if len(packages) != 1:
    raise RuntimeError("Expected one installed TMP package")
with tarfile.open(packages[0], "r:gz") as archive:
    members = {m.name: m for m in archive.getmembers()}
    for name in sorted(members):
        if not name.endswith("/pathname"):
            continue
        source = archive.extractfile(members[name]).read().decode("utf-8")
        if not source.startswith("Assets/TextMesh Pro"):
            raise ValueError(source)
        destination = root / source.replace("Assets/TextMesh Pro", "Assets/HanaMart/ThirdParty/TextMesh Pro", 1)
        key = name.rsplit("/", 1)[0]
        if key + "/asset" in members:
            destination.parent.mkdir(parents=True, exist_ok=True)
            data = archive.extractfile(members[key + "/asset"]).read()
            if destination.exists() and destination.read_bytes() != data:
                raise FileExistsError(destination)
            destination.write_bytes(data)
        else:
            destination.mkdir(parents=True, exist_ok=True)
        if key + "/asset.meta" in members:
            destination.with_name(destination.name + ".meta").write_bytes(archive.extractfile(members[key + "/asset.meta"]).read())
print("TMP essentials installed under Assets/HanaMart/ThirdParty/TextMesh Pro")
