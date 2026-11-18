""" #+begin_org
* *[Summary]* :: Status of a bisos-pip package's installs: does its editable install need a reinstall.
#+end_org """

""" #+begin_org
* Overview
A bisos-pip package is installed editable into a dev venv (=pypiProc.sh -i pkgReInstall edit <venv>=):
pip records where it came from, and =seedPypiProc.sh='s =vis_devBinSymlinks= links its =py3/bin/*= into
=/bisos/venv/py3/devbin=. Python edits need no reinstall; what pip and devbin recorded at install time
does. =localStatus()= compares that record with the repo:

1. installed editable from this repo (=.dist-info/direct_url.json=), not a PyPI copy or another clone;
2. =py3/bin= vs. =setup.py='s =scripts= --- they differ when =setup.py= was not regenerated
   (=bx-dblock= outside Emacs regenerates nothing);
3. =py3/bin= vs. the devbin symlinks, and devbin links into this repo left dangling; and pip's copies
   of the scripts in the venv's =bin/= (they run instead of devbin's when the venv is activated);
4. =setup.py= / =pyproject.toml= newer than the install, and =pip check=.

The action is the most demanding one found: =regenerate= (=setup.py=, then reinstall), =reinstall=,
=devbinClean= (remove dangling links by hand; a reinstall does not), or =ok=.

=releaseStatus()= compares the repo with what was released: the files of the package's install in
=/bisos/venv/py3/bisos3= (from PyPI; its =RECORD= lists them) against the repo's committed files
(git =HEAD=), and the latest version on PyPI against bisos3's. Uncommitted and unpushed work is
reported as such: only committed and pushed changes are a reason to push to PyPI. The action is the
first thing to do: =uncommitted=, =unpushed=, =notOnPypi= (first upload), =push=, =update=, or =ok=.

Read-only: reads files and git, runs =pip check=, and asks PyPI's JSON API (no credentials).
#+end_org """

import concurrent.futures
import dataclasses
import functools
import importlib.metadata
import json
import os
import pathlib
import re
import subprocess
import typing
import urllib.error
import urllib.parse
import urllib.request

devVenvDefault = pathlib.Path("/bisos/venv/py3/dev-bisos3")
releasedVenvDefault = pathlib.Path("/bisos/venv/py3/bisos3")
devbinBase = pathlib.Path("/bisos/venv/py3/devbin")

# Least to most demanding; LocalStatus.action is the highest one with a reason.
actions = ("ok", "devbinClean", "reinstall", "regenerate")


# Release side, in the order to act: commit, git push, PyPI push (or first upload), bisos3 update.
releaseActions = ("ok", "update", "push", "notOnPypi", "unpushed", "uncommitted")


@dataclasses.dataclass
class Status:
    """What a package needs, with a reason per finding; =order= ranks the actions."""
    pkgName: str
    py3: pathlib.Path
    venv: pathlib.Path
    reasons: list[tuple[str, str]] = dataclasses.field(default_factory=list)  # (action, text)
    order: typing.ClassVar[tuple[str, ...]] = actions

    def add(self, action: str, text: str) -> None:
        self.reasons.append((action, text))

    @property
    def action(self) -> str:
        return max((action for action, _ in self.reasons), key=self.order.index, default="ok")


class LocalStatus(Status):
    """Does the editable install in a dev venv need a reinstall."""


class ReleaseStatus(Status):
    """Does PyPI need a push, and the released venv (bisos3) an update."""
    order = releaseActions


def py3Dir(repo: str | pathlib.Path) -> pathlib.Path:
    """The package's =py3= directory, given the repo or its =py3=."""
    repo = pathlib.Path(repo).expanduser().absolute()
    for candidate in (repo / "py3", repo):
        if (candidate / "setup.py").is_file():
            return candidate
    raise ValueError(f"{repo}: no setup.py in it or in its py3/")


def pkgName(py3: pathlib.Path) -> str:
    """The distribution name, as setup.py gives it: a literal name=, a literal pkgName(), or
    bisos.<repo directory> (the generated pkgName(), from the setup.py's real path)."""
    text = (py3 / "setup.py").read_text()
    literal = re.search(r"^\s*name\s*=\s*['\"]([^'\"]+)['\"]", text, re.MULTILINE)
    if literal:
        return literal.group(1)
    pkgNameDef = re.search(r"def pkgName\(\):(.*?)\n\s*return ([^\n]+)", text, re.DOTALL)
    if pkgNameDef and "grandMother" not in pkgNameDef.group(2):
        return pkgNameDef.group(2).strip().strip("'\"")
    return f"bisos.{py3.resolve().parent.name}"


def canonical(name: str) -> str:
    return re.sub(r"[-_.]+", "-", name).lower()


def sitePackages(venv: pathlib.Path) -> pathlib.Path:
    found = sorted(venv.glob("lib/python3*/site-packages"))
    if not found:
        raise ValueError(f"{venv}: no lib/python3*/site-packages")
    return found[-1]


def distribution(venv: pathlib.Path, name: str) -> importlib.metadata.Distribution | None:
    """The installed distribution of =name= in =venv=, or None."""
    for dist in importlib.metadata.distributions(path=[str(sitePackages(venv))]):
        if canonical(dist.metadata["Name"] or "") == canonical(name):
            return dist
    return None


def setupScripts(py3: pathlib.Path) -> list[str]:
    """The =scripts= list of setup.py (generated by the b:py3:pypi:setup/scripts dblock)."""
    block = re.search(r"^scripts = \[(.*?)^\]", (py3 / "setup.py").read_text(), re.MULTILINE | re.DOTALL)
    return re.findall(r"'([^']+)'", block.group(1)) if block else []


def binScripts(py3: pathlib.Path) -> list[str]:
    """What =seedPypiProc.sh= puts in =scripts= and links into devbin: =ls bin/*=, without
    =__pycache__= and =*.pyc= (and directories)."""
    binDir = py3 / "bin"
    if not binDir.is_dir():
        return []
    return [f"bin/{each.name}" for each in sorted(binDir.iterdir())
            if not each.name.startswith(".") and each.name != "__pycache__"
            and not each.name.endswith("pyc") and (each.is_symlink() or not each.is_dir())]


def editableSource(dist: importlib.metadata.Distribution) -> tuple[bool, pathlib.Path | None, str]:
    """(editable, local path, url) from the install's direct_url.json."""
    raw = dist.read_text("direct_url.json")
    if raw is None:
        return False, None, ""
    record = json.loads(raw)
    url = record.get("url", "")
    path = None
    if url.startswith("file://"):
        path = pathlib.Path(urllib.request.url2pathname(urllib.parse.urlparse(url).path))
    return bool(record.get("dir_info", {}).get("editable")), path, url


@functools.lru_cache(maxsize=None)
def pipCheck(venv: pathlib.Path) -> tuple[str, ...]:
    """The problem lines of =pip check= in =venv= (one run per venv)."""
    result = subprocess.run([str(venv / "bin" / "python"), "-m", "pip", "check"],
                            capture_output=True, text=True, check=False)
    if result.returncode == 0:
        return ()
    return tuple(line for line in result.stdout.splitlines() if line.strip())


def localStatus(repo: str | pathlib.Path, venv: str | pathlib.Path = devVenvDefault) -> LocalStatus:
    """Checks 1-4 (Overview) for the package at =repo= against its install in =venv=."""
    py3 = py3Dir(repo)
    venv = pathlib.Path(venv)
    status = LocalStatus(pkgName(py3), py3, venv)

    # 1. Installed editable, from this repo
    dist = distribution(venv, status.pkgName)
    if dist is None:
        status.add("reinstall", f"not installed in {venv}")
    else:
        editable, path, url = editableSource(dist)
        if not editable:
            status.add("reinstall", f"installed {dist.version}, not editable ({url or 'from an index'})")
        elif path is None or path.resolve() != py3.resolve():
            status.add("reinstall", f"editable from {path or url}, not from {py3}")

    # 2. bin/ vs. setup.py scripts
    inBin, inSetup = binScripts(py3), setupScripts(py3)
    for script in sorted(set(inBin) - set(inSetup)):
        status.add("regenerate", f"{script} not in setup.py scripts")
    for script in sorted(set(inSetup) - set(inBin)):
        status.add("regenerate", f"setup.py scripts lists {script}, which is not in bin/")

    # 3. bin/ vs. devbin symlinks
    binDir = (py3 / "bin").resolve()
    for script in inBin:
        link = devbinBase / pathlib.PurePath(script).name
        if not link.is_symlink():
            status.add("reinstall", f"{script} not in {devbinBase}")
        elif link.resolve() != (binDir / link.name).resolve():
            status.add("reinstall", f"{link} points to {os.readlink(link)}")
    # 3b. pip's copies in the venv's bin/ (shebang rewritten): what runs when the venv is activated
    for script in sorted(set(inBin) & set(inSetup)):
        copy, source = venv / "bin" / pathlib.PurePath(script).name, py3 / script
        if copy.is_file() and source.is_file() and not sameContent(copy.read_bytes(), source.read_bytes()):
            status.add("reinstall", f"{copy} is an older copy of {script} (runs when the venv is activated)")
    if devbinBase.is_dir():
        for link in sorted(devbinBase.iterdir()):
            if link.is_symlink() and not link.exists():
                target = pathlib.Path(os.readlink(link))
                if target.parent.resolve() == binDir:
                    status.add("devbinClean", f"{link} is dangling (to {target})")

    # 4. Metadata newer than the install; unmet requirements
    if dist is not None:
        distPath = getattr(dist, "_path", None)
        if distPath is not None:
            installed = pathlib.Path(distPath).stat().st_mtime
            for meta in ("setup.py", "pyproject.toml"):
                if (py3 / meta).is_file() and (py3 / meta).stat().st_mtime > installed:
                    status.add("reinstall", f"{meta} changed after the install")
        for line in pipCheck(venv):
            if canonical(line.split()[0]) == canonical(status.pkgName):
                status.add("reinstall", f"pip check: {line}")

    return status


def pypiLatest(name: str, timeout: float = 10) -> str | None:
    """The latest version on PyPI (its JSON API), or None when PyPI does not have the package.
    Raises URLError / OSError when PyPI is not reached."""
    try:
        with urllib.request.urlopen(f"https://pypi.org/pypi/{name}/json", timeout=timeout) as response:
            return json.load(response)["info"]["version"]
    except urllib.error.HTTPError as err:
        if err.code == 404:
            return None
        raise


def versionKey(version: str):
    """A sortable form of a version string."""
    try:
        from packaging.version import Version
        return Version(version)
    except Exception:
        return tuple(int(part) if part.isdigit() else 0 for part in version.split("."))


def _git(top: pathlib.Path, *args: str) -> subprocess.CompletedProcess:
    return subprocess.run(["git", "-C", str(top), *args], capture_output=True, text=True, check=False)


def headContents(top: pathlib.Path, relPaths: list[str]) -> dict[str, bytes | None]:
    """The contents at git HEAD of relPaths (relative to top), None when absent; one git process."""
    if not relPaths:
        return {}
    proc = subprocess.run(["git", "-C", str(top), "cat-file", "--batch"],
                          input="".join(f"HEAD:{rel}\n" for rel in relPaths).encode(),
                          capture_output=True, check=False)
    out, pos, contents = proc.stdout, 0, {}
    for rel in relPaths:
        eol = out.index(b"\n", pos)
        header, pos = out[pos:eol].decode(), eol + 1
        if header.endswith(" missing"):
            contents[rel] = None
            continue
        size = int(header.split()[2])
        contents[rel], pos = out[pos:pos + size], pos + size + 1
    return contents


def _pythonShebang(line: bytes) -> bool:
    return line.startswith(b"#!") and b"python" in line


def sameContent(installed: bytes, committed: bytes) -> bool:
    """Equal, or equal but for the first line when both start with a python shebang: pip rewrites a
    script's shebang to the venv's python when it installs it."""
    if installed == committed:
        return True
    instHead, _, instRest = installed.partition(b"\n")
    commHead, _, commRest = committed.partition(b"\n")
    return _pythonShebang(instHead) and _pythonShebang(commHead) and instRest == commRest


def releaseStatus(repo: str | pathlib.Path, venv: str | pathlib.Path = releasedVenvDefault,
                  pypi: bool = True) -> ReleaseStatus:
    """The release side (Overview) for the package at =repo=, against its install in =venv=."""
    py3 = py3Dir(repo)
    venv = pathlib.Path(venv)
    status = ReleaseStatus(pkgName(py3), py3, venv)

    toplevel = _git(py3, "rev-parse", "--show-toplevel").stdout.strip()
    if not toplevel:
        status.add("uncommitted", f"{py3} is not in a git repo")
        return status
    top = pathlib.Path(toplevel)
    py3Rel = py3.resolve().relative_to(top.resolve()).as_posix()

    dist = distribution(venv, status.pkgName)
    topLevels = ((dist.read_text("top_level.txt") if dist else None) or "bisos").split()
    watched = [f"{py3Rel}/bin"] + [f"{py3Rel}/{each}" for each in topLevels]

    # Git: only committed and pushed changes are a reason to push to PyPI
    for line in _git(top, "status", "--porcelain", "--untracked-files=all", "--", *watched).stdout.splitlines():
        if "__pycache__" not in line and not line.endswith(".pyc"):
            status.add("uncommitted", f"{line[3:]} ({line[:2].strip()})")
    ahead = _git(top, "rev-list", "--count", "@{u}..HEAD")
    if ahead.returncode != 0:
        status.add("unpushed", "the branch has no upstream")
    elif int(ahead.stdout.strip() or 0):
        status.add("unpushed", f"{ahead.stdout.strip()} commit(s) not pushed")

    # PyPI vs. the released venv
    reached, latest = False, None
    if pypi:
        try:
            latest, reached = pypiLatest(status.pkgName), True
        except (urllib.error.URLError, OSError) as err:
            status.add("ok", f"PyPI not reached ({err}); versions not compared")
        if reached and latest is None:
            status.add("notOnPypi", "not on PyPI")
    if dist is None:
        if latest:
            status.add("update", f"not installed in {venv} (PyPI has {latest}); files not compared")
        return status
    if editableSource(dist)[0]:
        status.add("ok", f"{venv} has an editable install; files not compared")
        return status
    if latest and versionKey(latest) > versionKey(dist.version):
        status.add("update", f"{venv} has {dist.version}, PyPI {latest}; files compared after the update")
        return status

    # Released files (bisos3's RECORD) vs. committed files (git HEAD)
    released: dict[str, pathlib.Path] = {}
    for each in dist.files or []:
        parts = each.parts
        if "__pycache__" in parts or each.name.endswith(".pyc"):
            continue
        if parts[0] in topLevels:
            released[f"{py3Rel}/{each.as_posix()}"] = pathlib.Path(each.locate())
        elif parts[0] == ".." and len(parts) >= 2 and parts[-2] == "bin":
            released[f"{py3Rel}/bin/{parts[-1]}"] = pathlib.Path(each.locate())
    binRel = pathlib.PurePosixPath(py3Rel) / "bin"
    committed = set()
    for rel in _git(top, "ls-tree", "-r", "--name-only", "HEAD", "--", *watched).stdout.splitlines():
        path = pathlib.PurePosixPath(rel)
        if "__pycache__" in path.parts or path.name.endswith("pyc") or path.name.startswith("."):
            continue
        if path.parent == binRel or (not rel.startswith(f"{binRel}/") and path.suffix == ".py"):
            committed.add(rel)
    contents = headContents(top, sorted(set(released) | committed))

    def short(rel: str) -> str:
        return rel[len(py3Rel) + 1:]

    for rel, installedPath in sorted(released.items()):
        if contents[rel] is None:
            status.add("push", f"{short(rel)} is in {dist.version}, removed from the repo")
        elif installedPath.is_file() and not sameContent(installedPath.read_bytes(), contents[rel]):
            status.add("push", f"{short(rel)} changed since {dist.version}")
    for rel in sorted(committed - set(released)):
        status.add("push", f"{short(rel)} not in {dist.version}")
    return status


# What BISOS installs (interim, until WorkPlan 2.5): bisos.py3-all's closure (old) and the pipPkgsList
# of these -sbom.pcs files (the direction), each with its requirements.
scopeSboms = [pathlib.Path("/bisos/git/bxRepos/bisos/bsip4/bin/rawBisos-sbom.pcs")]
scopeRoots = ["bisos.py3-all"]


def pkgRepos(base: str | pathlib.Path) -> dict[str, pathlib.Path]:
    """Canonical package name -> py3 dir, for every child of base with a py3/setup.py."""
    repos = {}
    for setup in sorted(pathlib.Path(base).glob("*/py3/setup.py")):
        try:
            repos[canonical(pkgName(setup.parent))] = setup.parent
        except (OSError, ValueError):
            continue
    return repos


def setupRequires(py3: pathlib.Path) -> list[str]:
    """The names in setup.py's =requires= list (generated by the b:py3:pypi:setup/requires dblock)."""
    block = re.search(r"^requires = \[(.*?)^\]", (py3 / "setup.py").read_text(), re.MULTILINE | re.DOTALL)
    if not block:
        return []
    return [re.split(r"[<>=!~ \[;]", each)[0] for each in re.findall(r"['\"]([^'\"]+)['\"]", block.group(1))]


def sbomPipPkgs(sbom: pathlib.Path, base: pathlib.Path) -> list[str]:
    """The pip packages an -sbom.pcs names (=pp(...)= / =pipPkg(...)=); a =bisos-pip/<dir>= entry is the
    package of that repo directory under base."""
    names = []
    for each in re.findall(r"\b(?:pp|pipPkg)\(\s*['\"]([^'\"]+)['\"]", sbom.read_text()):
        if "/" in each:
            py3 = base / pathlib.PurePath(each).name / "py3"
            if (py3 / "setup.py").is_file():
                names.append(pkgName(py3))
        else:
            names.append(each)
    return names


def installScope(base: str | pathlib.Path, repos: dict[str, pathlib.Path] | None = None) -> set[str]:
    """Canonical names of the packages BISOS installs: scopeRoots and scopeSboms' packages, with the
    requirements of each that has a repo under base."""
    base = pathlib.Path(base)
    repos = pkgRepos(base) if repos is None else repos
    todo = list(scopeRoots)
    for sbom in scopeSboms:
        if sbom.is_file():
            todo += sbomPipPkgs(sbom, base)
    scope: set[str] = set()
    while todo:
        name = canonical(todo.pop())
        if name in scope:
            continue
        scope.add(name)
        if name in repos:
            todo += setupRequires(repos[name])
    return scope


@dataclasses.dataclass
class PkgRow:
    """One package of pkgsStatus: its scope, and both sides when in scope."""
    pkgName: str
    py3: pathlib.Path
    inScope: bool
    local: LocalStatus | None = None
    release: ReleaseStatus | None = None
    error: str = ""
    installedInDev: bool = False


def pkgsStatus(base: str | pathlib.Path = "/bisos/git/bxRepos/bisos-pip",
               devVenv: str | pathlib.Path = devVenvDefault,
               releasedVenv: str | pathlib.Path = releasedVenvDefault,
               workers: int = 8) -> list[PkgRow]:
    """Both sides for every in-scope package under base; out-of-scope ones are listed, not checked
    (being a repo under base is not a reason to install a package). A failure is a row's error."""
    repos = pkgRepos(base)
    scope = installScope(base, repos)
    devVenv, releasedVenv = pathlib.Path(devVenv), pathlib.Path(releasedVenv)

    def one(name: str, py3: pathlib.Path) -> PkgRow:
        row = PkgRow(pkgName(py3), py3, name in scope)
        try:
            row.installedInDev = distribution(devVenv, row.pkgName) is not None
            if row.inScope:
                row.local = localStatus(py3, devVenv)
                row.release = releaseStatus(py3, releasedVenv)
        except Exception as err:  # one package's failure is its row, not the walk's
            row.error = f"{type(err).__name__}: {err}"
        return row

    pipCheck(devVenv)  # once, before the threads
    with concurrent.futures.ThreadPoolExecutor(max_workers=workers) as pool:
        return list(pool.map(lambda item: one(*item), sorted(repos.items())))
