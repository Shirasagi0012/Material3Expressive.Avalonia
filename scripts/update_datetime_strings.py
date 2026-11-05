"""Import the date/time subset from the pinned Core locale map; --check verifies it."""
import argparse
import io
import json
from pathlib import Path
import re
import subprocess
import tarfile

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--upstream", type=Path, required=True)
parser.add_argument("--check", action="store_true")
args = parser.parse_args()
root = Path(__file__).resolve().parents[1]
commit = json.loads((root / "docs/upstream-baseline.json").read_text(encoding="utf-8"))["composeMultiplatformCore"]["commit"]
directory = "compose/material3/material3/src/skikoMain/kotlin/androidx/compose/material3/l10n"
archive = subprocess.run(["git", "-c", "gc.auto=0", "-C", str(args.upstream), "archive", commit, directory], check=True, stdout=subprocess.PIPE).stdout
with tarfile.open(fileobj=io.BytesIO(archive)) as tar:
    sources = {Path(member.name).name: tar.extractfile(member).read().decode("utf-8")
               for member in tar.getmembers() if member.isfile() and member.name.endswith(".kt")}
functions = {}
for source in sources.values():
    for name, body in re.findall(r"fun Translations\.`?(\w+)`?\(\)\s*=\s*mapOf\((.*?)\n\)", source, re.S):
        values = {}
        for key, literal in re.findall(r'Strings\.(\w+)\s+to\s+("(?:\\.|[^"\\])*"),', body):
            if key.startswith(("DatePicker", "DateInput", "DateRange", "TimePicker", "TimeInput", "TimeScroll")):
                value = json.loads(literal.replace(r"\$", "$").replace(r"\'", "'"))
                values[key] = re.sub(r"%(\d+)\$[sd]", lambda m: "{" + str(int(m[1]) - 1) + "}", value)
        functions[name] = values
translations = {}
for tag, name in re.findall(r'"([^"]*)"\s*->\s*Translations\.`?(\w+)`?\(\)', sources["Translations.kt"]):
    # The runtime maps the two Java legacy language aliases to these same records.
    if tag in ("he", "id"):
        continue
    translations[tag.replace("_", "-").lower() or "en"] = functions[name]
keys = set(translations["en"])
assert keys and all(set(values) == keys for values in translations.values())
destination = root / "src/Material3Expressive/Components/DateTimePicker/DateTimeStrings.json"
content = json.dumps(dict(sorted(translations.items())), ensure_ascii=False, indent=2) + "\n"
if args.check:
    if json.loads(destination.read_text(encoding="utf-8-sig")) != json.loads(content):
        raise SystemExit("Date/time translations differ from the pinned Core map.")
else:
    destination.write_text(content, encoding="utf-8")
print(f"Verified {len(translations)} locales × {len(keys)} strings at Core {commit}.")
