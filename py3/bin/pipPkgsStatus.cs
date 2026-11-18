#!/usr/bin/env python
# -*- coding: utf-8 -*-

""" #+begin_org
* ~[Summary]~ :: A =CmndSvc= that tells, for a bisos-pip package, whether its editable install needs a
  reinstall, whether PyPI needs a push, and whether bisos3 needs an update.
#+end_org """

####+BEGIN: b:py3:cs:file/dblockControls :classification "cs-mu"
""" #+begin_org
* [[elisp:(org-cycle)][| /Control Parameters Of This File/ |]] :: dblk ctrls classifications=cs-mu
#+BEGIN_SRC emacs-lisp
(setq-local b:dblockControls t) ; (setq-local b:dblockControls nil)
(put 'b:dblockControls 'py3:cs:Classification "cs-mu") ; one of cs-mu, cs-u, cs-lib, bpf-lib, pyLibPure
#+END_SRC
#+RESULTS:
: cs-mu
#+end_org """
####+END:

####+BEGIN: b:prog:file/proclamations :outLevel 1
""" #+begin_org
* *[[elisp:(org-cycle)][| Proclamations |]]* :: Libre-Halaal Software --- Part Of BISOS ---  Poly-COMEEGA Format.
** This is Libre-Halaal Software. © Neda Communications, Inc. Subject to AGPL.
** It is part of BISOS (ByStar Internet Services OS)
** Best read and edited  with Blee in Poly-COMEEGA (Polymode Colaborative Org-Mode Enhance Emacs Generalized Authorship)
#+end_org """
####+END:

####+BEGIN: b:prog:file/particulars :authors ("./inserts/authors-mb.org")
""" #+begin_org
* *[[elisp:(org-cycle)][| Particulars |]]* :: Authors, version
** This File: /bisos/git/bxRepos/bisos-pip/pycs/py3/bin/pipPkgsStatus.cs
** Authors: Mohsen BANAN, http://mohsen.banan.1.byname.net/contact
#+end_org """
####+END:

####+BEGIN: b:py3:file/particulars-csInfo :status "inUse"
""" #+begin_org
* *[[elisp:(org-cycle)][| Particulars-csInfo |]]*
#+end_org """
import typing
csInfo: typing.Dict[str, typing.Any] = { 'moduleName': ['pipPkgsStatus'], }
csInfo['version'] = '202409222627'
csInfo['status']  = 'inUse'
csInfo['panel'] = 'pipPkgsStatus-Panel.org'
csInfo['groupingType'] = 'IcmGroupingType-pkged'
csInfo['cmndParts'] = 'IcmCmndParts[common] IcmCmndParts[param]'
####+END:

""" #+begin_org
* [[elisp:(org-cycle)][| ~Description~ |]] :: [[file:/bisos/git/auth/bxRepos/blee-binders/bisos-core/PyFwrk/bisos-pip/bisos.cs/_nodeBase_/fullUsagePanel-en.org][BISOS CmndSvcs Panel]]   [[elisp:(org-cycle)][| ]]

An editable install picks up Python edits by itself; what pip and =/bisos/venv/py3/devbin= recorded at
install time does not. =bisos.pycs.pipPkgsStatus= compares that record with the repo:

1. installed editable from this repo (=.dist-info/direct_url.json=), not a PyPI copy or another clone;
2. =py3/bin= vs. =setup.py='s =scripts= (=bx-dblock= outside Emacs leaves =setup.py= stale);
3. =py3/bin= vs. the devbin symlinks, and dangling devbin links into this repo;
4. =setup.py= / =pyproject.toml= newer than the install, and =pip check=.

- =pkgLocalStatus <repo> [venv]= --- the action: =ok=, =devbinClean= (remove dangling links by hand),
  =reinstall= (=pypiProc.sh -i pkgReInstall edit <venv>=) or =regenerate= (=setup.py= first; with
  =G_emacsServerPid= set outside Emacs), with the reasons. venv defaults to =/bisos/venv/py3/dev-bisos3=.
- =pkgReleaseStatus <repo> [venv]= --- the released files (the =RECORD= of the PyPI install in venv,
  default =/bisos/venv/py3/bisos3=) vs. the repo's committed files (git =HEAD=; a script's shebang, which
  pip rewrites, is ignored), and PyPI's latest version vs. venv's. The action, the first thing to do:
  =uncommitted=, =unpushed=, =notOnPypi=, =push=, =update= or =ok=.
- =pkgsStatus [base]= --- one row per package under base (default =/bisos/git/bxRepos/bisos-pip=): in
  or out of what BISOS installs (=bisos.py3-all='s closure and =rawBisos-sbom.pcs='s =pipPkgsList=,
  with their requirements), and for those in it, both actions. Out-of-scope packages are listed, not
  checked: being a repo under base is not a reason to install one.

Read-only (PyPI's JSON API is asked without credentials). Decided in bisos-pip/overview WorkPlan
4.1; built in bisos-pip/pycs WorkPlan Stage 2.

** Status: 2.2 (local), 2.3 (release), 2.4 (all packages)
** /[[elisp:(org-cycle)][| Planned Improvements |]]/ :
*** TODO Scope from the -sbom.pcs files only; =uninstall= for installed out-of-scope packages (2.5)
#+end_org """

####+BEGIN: b:prog:file/orgTopControls :outLevel 1
""" #+begin_org
* [[elisp:(org-cycle)][| Controls |]] :: [[elisp:(delete-other-windows)][(1)]] | [[elisp:(show-all)][Show-All]]  [[elisp:(org-shifttab)][Overview]]  [[elisp:(progn (org-shifttab) (org-content))][Content]] | [[file:Panel.org][Panel]] | [[elisp:(blee:ppmm:org-mode-toggle)][Nat]] | [[elisp:(bx:org:run-me)][Run]] | [[elisp:(bx:org:run-me-eml)][RunEml]] | [[elisp:(progn (save-buffer) (kill-buffer))][S&Q]]  [[elisp:(save-buffer)][Save]]  [[elisp:(kill-buffer)][Quit]] [[elisp:(org-cycle)][| ]]
** /Version Control/ ::  [[elisp:(call-interactively (quote cvs-update))][cvs-update]]  [[elisp:(vc-update)][vc-update]] | [[elisp:(bx:org:agenda:this-file-otherWin)][Agenda-List]]  [[elisp:(bx:org:todo:this-file-otherWin)][ToDo-List]]

#+end_org """
####+END:

####+BEGIN: b:py3:file/workbench :outLevel 1
""" #+begin_org
* [[elisp:(org-cycle)][| Workbench |]] :: [[elisp:(python-check (format "/bisos/venv/py3/bisos3/bin/python -m pyclbr %s" (bx:buf-fname))))][pyclbr]] || [[elisp:(python-check (format "/bisos/venv/py3/bisos3/bin/python -m pydoc ./%s" (bx:buf-fname))))][pydoc]] || [[elisp:(python-check (format "/bisos/pipx/bin/pyflakes %s" (bx:buf-fname)))][pyflakes]] | [[elisp:(python-check (format "/bisos/pipx/bin/pychecker %s" (bx:buf-fname))))][pychecker (executes)]] | [[elisp:(python-check (format "/bisos/pipx/bin/pycodestyle %s" (bx:buf-fname))))][pycodestyle]] | [[elisp:(python-check (format "/bisos/pipx/bin/flake8 %s" (bx:buf-fname))))][flake8]] | [[elisp:(python-check (format "/bisos/pipx/bin/pylint %s" (bx:buf-fname))))][pylint]]  [[elisp:(org-cycle)][| ]]
#+end_org """
####+END:

####+BEGIN: b:py3:cs:framework/imports :basedOn "classification"
""" #+begin_org
*  _[[elisp:(blee:menu-sel:outline:popupMenu)][±]]_ _[[elisp:(blee:menu-sel:navigation:popupMenu)][Ξ]]_ [[elisp:(outline-show-branches+toggle)][|=]] [[elisp:(bx:orgm:indirectBufOther)][|>]] *[[elisp:(blee:ppmm:org-mode-toggle)][|N]]*  CsFrmWrk   [[elisp:(outline-show-subtree+toggle)][||]] *Imports* =Based on Classification=cs-mu=
#+end_org """
from bisos import b
from bisos.b import cs
from bisos.b import b_io
from bisos.common import csParam

import collections
####+END:

import typing

from bisos.pycs import pipPkgsStatus

""" #+begin_org
*  _[[elisp:(blee:menu-sel:outline:popupMenu)][±]]_ _[[elisp:(blee:menu-sel:navigation:popupMenu)][Ξ]]_ [[elisp:(outline-show-branches+toggle)][|=]] [[elisp:(bx:orgm:indirectBufOther)][|>]] *[[elisp:(blee:ppmm:org-mode-toggle)][|N]]*  CsFrmWrk   [[elisp:(outline-show-subtree+toggle)][||]] ~csuList emacs-list Specifications~  [[elisp:(blee:org:code-block/above-run)][ /Eval Below/ ]] [[elisp:(org-cycle)][| ]]
#+BEGIN_SRC emacs-lisp
(setq  b:py:cs:csuList
  (list
   ;; "bisos.b.cs.ro"
   ;; "bisos.csPlayer.bleep"
   ;; "bisos.facter.facter_csu"
   ;; "bisos.banna.bannaPortNu"
 ))
#+END_SRC
#+RESULTS:
#+end_org """

####+BEGIN: b:py3:cs:framework/csuListProc :pyImports t :csuImports t :csuParams t :csmuParams nil
""" #+begin_org
*  _[[elisp:(blee:menu-sel:outline:popupMenu)][±]]_ _[[elisp:(blee:menu-sel:navigation:popupMenu)][Ξ]]_ [[elisp:(outline-show-branches+toggle)][|=]] [[elisp:(bx:orgm:indirectBufOther)][|>]] *[[elisp:(blee:ppmm:org-mode-toggle)][|N]]*  CsFrmWrk   [[elisp:(outline-show-subtree+toggle)][||]] ~Process CSU List~ with /0/ in csuList pyImports=t csuImports=t csuParams=t
#+end_org """


csuList = [ ]

g_importedCmndsModules = cs.csuList_importedModules(csuList)

def g_extraParams():
    csParams = cs.param.CmndParamDict()
    cs.csuList_commonParamsSpecify(csuList, csParams)
    cs.argsparseBasedOnCsParams(csParams)

####+END:

####+BEGIN: b:py3:cs:main/exposedSymbols :classes ()
""" #+begin_org
*  _[[elisp:(blee:menu-sel:outline:popupMenu)][±]]_ _[[elisp:(blee:menu-sel:navigation:popupMenu)][Ξ]]_ [[elisp:(outline-show-branches+toggle)][|=]] [[elisp:(bx:orgm:indirectBufOther)][|>]] *[[elisp:(blee:ppmm:org-mode-toggle)][|N]]*  CsFrmWrk   [[elisp:(outline-show-subtree+toggle)][||]] ~CS Controls and Exposed Symbols List Specification~ with /0/ in Classes List
#+end_org """
####+END:

cs.invOutcomeReportControl(cmnd=True, ro=True)

####+BEGIN: blee:bxPanel:foldingSection :outLevel 0 :sep nil :title "CmndSvcs" :anchor ""  :extraInfo "Command Services Section"
""" #+begin_org
*  _[[elisp:(blee:menu-sel:outline:popupMenu)][±]]_ _[[elisp:(blee:menu-sel:navigation:popupMenu)][Ξ]]_ [[elisp:(outline-show-branches+toggle)][|=]] [[elisp:(bx:orgm:indirectBufOther)][|>]] *[[elisp:(blee:ppmm:org-mode-toggle)][|N]]*     [[elisp:(outline-show-subtree+toggle)][| _CmndSvcs_: |]]  Command Services Section  [[elisp:(org-shifttab)][<)]] E|
#+end_org """
####+END:

####+BEGIN: b:py3:cs:cmnd/classHead :cmndName "examples" :extent "verify" :ro "noCli" :comment "FrameWrk: CS-Main-Examples" :parsMand "" :parsOpt "" :argsMin 0 :argsMax 0 :pyInv ""
""" #+begin_org
*  _[[elisp:(blee:menu-sel:outline:popupMenu)][±]]_ _[[elisp:(blee:menu-sel:navigation:popupMenu)][Ξ]]_ [[elisp:(outline-show-branches+toggle)][|=]] [[elisp:(bx:orgm:indirectBufOther)][|>]] *[[elisp:(blee:ppmm:org-mode-toggle)][|N]]*  CmndSvc-   [[elisp:(outline-show-subtree+toggle)][||]] <<examples>>  *FrameWrk: CS-Main-Examples*  =verify= ro=noCli   [[elisp:(org-cycle)][| ]]
#+end_org """
class examples(cs.Cmnd):
    cmndParamsMandatory = [ ]
    cmndParamsOptional = [ ]
    cmndArgsLen = {'Min': 0, 'Max': 0,}
    rtInvConstraints = cs.rtInvoker.RtInvoker.new_noRo() # NO RO From CLI

    @cs.track(fnLoc=True, fnEntry=True, fnExit=True)
    def cmnd(self,
             rtInv: cs.RtInvoker,
             cmndOutcome: b.op.Outcome,
    ) -> b.op.Outcome:
        """FrameWrk: CS-Main-Examples"""
        failed = b_io.eh.badOutcome
        callParamsDict = {}
        if self.invocationValidate(rtInv, cmndOutcome, callParamsDict, None).isProblematic():
            return failed(cmndOutcome)
####+END:
        self.cmndDocStr(f""" #+begin_org
***** [[elisp:(org-cycle)][| *CmndDesc:* | ]]  Conventional top level example.
        #+end_org """)

        cs.examples.myName(cs.G.icmMyName(), cs.G.icmMyFullName())
        cs.examples.commonBrief()

        cmnd = cs.examples.cmndEnter

        literal = cs.examples.execInsert

        cs.examples.menuChapter('=Local: does the editable install need a reinstall=')
        cmnd('pkgLocalStatus', args="/bisos/git/bxRepos/bisos-pip/pycs", comment=" # into dev-bisos3")
        cmnd('pkgLocalStatus', args="/bisos/git/bxRepos/bisos-pip/webCap /bisos/venv/py3/dev-bisos3")
        cmnd('pkgLocalStatus', args=".", comment=" # the package of the current directory")

        cs.examples.menuChapter('=Release: does PyPI need a push, bisos3 an update=')
        cmnd('pkgReleaseStatus', args="/bisos/git/bxRepos/bisos-pip/pycs", comment=" # against bisos3")
        cmnd('pkgReleaseStatus', args="/bisos/git/bxRepos/bisos-pip/webCap /bisos/venv/py3/bisos3")

        cs.examples.menuChapter('=All packages: one row each=')
        cmnd('pkgsStatus', comment=" # every package under /bisos/git/bxRepos/bisos-pip")
        cmnd('pkgsStatus', args="/bisos/git/bxRepos/bisos-pip")

        cs.examples.menuChapter('=Acting on it (pkg py3 directory)=')
        literal("export G_emacsServerPid=<pid of your Blee emacs>   # only outside Emacs: bx-dblock needs it")
        literal("./pypiProc.sh -i pkgReInstall edit /bisos/venv/py3/dev-bisos3   # regenerate + reinstall")
        literal("./pypiProc.sh -p repo=main -i fullPrepBuildUploadRePrep   # push to PyPI (bisos-pip-release skill)")

        return(cmndOutcome)


####+BEGIN: b:py3:cs:cmnd/classHead :cmndName "pkgLocalStatus" :comment "" :extent "verify" :ro "cli" :parsMand "" :parsOpt "" :argsMin 1 :argsMax 2 :pyInv ""
""" #+begin_org
*  _[[elisp:(blee:menu-sel:outline:popupMenu)][±]]_ _[[elisp:(blee:menu-sel:navigation:popupMenu)][Ξ]]_ [[elisp:(outline-show-branches+toggle)][|=]] [[elisp:(bx:orgm:indirectBufOther)][|>]] *[[elisp:(blee:ppmm:org-mode-toggle)][|N]]*  CmndSvc-   [[elisp:(outline-show-subtree+toggle)][||]] <<pkgLocalStatus>>  =verify= argsMin=1 argsMax=2 ro=cli   [[elisp:(org-cycle)][| ]]
#+end_org """
class pkgLocalStatus(cs.Cmnd):
    cmndParamsMandatory = [ ]
    cmndParamsOptional = [ ]
    cmndArgsLen = {'Min': 1, 'Max': 2,}

    @cs.track(fnLoc=True, fnEntry=True, fnExit=True)
    def cmnd(self,
             rtInv: cs.RtInvoker,
             cmndOutcome: b.op.Outcome,
             argsList: typing.Optional[list[str]]=None,  # CsArgs
    ) -> b.op.Outcome:

        failed = b_io.eh.badOutcome
        callParamsDict = {}
        if self.invocationValidate(rtInv, cmndOutcome, callParamsDict, argsList).isProblematic():
            return failed(cmndOutcome)
        cmndArgsSpecDict = self.cmndArgsSpec()
####+END:
        if self.cmndDocStr(f""" #+begin_org
** [[elisp:(org-cycle)][| *CmndDesc:* | ]]  Does the package's editable install in venv need a reinstall: the action and why.
        #+end_org """): return(cmndOutcome)

        repo = self.cmndArgsGet("0", cmndArgsSpecDict, argsList)
        venv = self.cmndArgsGet("1", cmndArgsSpecDict, argsList) or pipPkgsStatus.devVenvDefault
        try:
            status = pipPkgsStatus.localStatus(repo, venv)
        except ValueError as err:
            return failed(cmndOutcome.set(opError=b.op.OpError.Failure, opErrInfo=str(err)))
        if rtInv.outs:
            _printStatus(status)
        return cmndOutcome.set(opResults=status.action)

####+BEGIN: b:py3:cs:method/args :methodName "cmndArgsSpec" :methodType "anyOrNone" :retType "bool" :deco "default" :argsList "self"
    """ #+begin_org
**  _[[elisp:(blee:menu-sel:outline:popupMenu)][±]]_ _[[elisp:(blee:menu-sel:navigation:popupMenu)][Ξ]]_ [[elisp:(outline-show-branches+toggle)][|=]] [[elisp:(bx:orgm:indirectBufOther)][|>]] *[[elisp:(blee:ppmm:org-mode-toggle)][|N]]*  Mtd-T-anyOrNone [[elisp:(outline-show-subtree+toggle)][||]] /cmndArgsSpec/ deco=default  deco=default  [[elisp:(org-cycle)][| ]]
    #+end_org """
    @cs.track(fnLoc=True, fnEntry=True, fnExit=True)
    def cmndArgsSpec(self, ):
####+END:
        """
***** Cmnd Args Specification
"""
        cmndArgsSpecDict = cs.CmndArgsSpecDict()
        cmndArgsSpecDict.argsDictAdd(
            argPosition="0",
            argName="repo",
            argDefault='',
            argChoices=[],
            argDescription="The package's repo or its py3 directory"
        )
        cmndArgsSpecDict.argsDictAdd(
            argPosition="1",
            argName="venv",
            argDefault='',
            argChoices=[],
            argDescription="The venv of the editable install; default /bisos/venv/py3/dev-bisos3"
        )
        return cmndArgsSpecDict



####+BEGIN: b:py3:cs:cmnd/classHead :cmndName "pkgReleaseStatus" :comment "" :extent "verify" :ro "cli" :parsMand "" :parsOpt "" :argsMin 1 :argsMax 2 :pyInv ""
""" #+begin_org
*  _[[elisp:(blee:menu-sel:outline:popupMenu)][±]]_ _[[elisp:(blee:menu-sel:navigation:popupMenu)][Ξ]]_ [[elisp:(outline-show-branches+toggle)][|=]] [[elisp:(bx:orgm:indirectBufOther)][|>]] *[[elisp:(blee:ppmm:org-mode-toggle)][|N]]*  CmndSvc-   [[elisp:(outline-show-subtree+toggle)][||]] <<pkgReleaseStatus>>  =verify= argsMin=1 argsMax=2 ro=cli   [[elisp:(org-cycle)][| ]]
#+end_org """
class pkgReleaseStatus(cs.Cmnd):
    cmndParamsMandatory = [ ]
    cmndParamsOptional = [ ]
    cmndArgsLen = {'Min': 1, 'Max': 2,}

    @cs.track(fnLoc=True, fnEntry=True, fnExit=True)
    def cmnd(self,
             rtInv: cs.RtInvoker,
             cmndOutcome: b.op.Outcome,
             argsList: typing.Optional[list[str]]=None,  # CsArgs
    ) -> b.op.Outcome:

        failed = b_io.eh.badOutcome
        callParamsDict = {}
        if self.invocationValidate(rtInv, cmndOutcome, callParamsDict, argsList).isProblematic():
            return failed(cmndOutcome)
        cmndArgsSpecDict = self.cmndArgsSpec()
####+END:
        if self.cmndDocStr(f""" #+begin_org
** [[elisp:(org-cycle)][| *CmndDesc:* | ]]  Does PyPI need a push, and venv (bisos3) an update: the action and why.
        #+end_org """): return(cmndOutcome)

        repo = self.cmndArgsGet("0", cmndArgsSpecDict, argsList)
        venv = self.cmndArgsGet("1", cmndArgsSpecDict, argsList) or pipPkgsStatus.releasedVenvDefault
        try:
            status = pipPkgsStatus.releaseStatus(repo, venv)
        except ValueError as err:
            return failed(cmndOutcome.set(opError=b.op.OpError.Failure, opErrInfo=str(err)))
        if rtInv.outs:
            _printStatus(status)
        return cmndOutcome.set(opResults=status.action)

####+BEGIN: b:py3:cs:method/args :methodName "cmndArgsSpec" :methodType "anyOrNone" :retType "bool" :deco "default" :argsList "self"
    """ #+begin_org
**  _[[elisp:(blee:menu-sel:outline:popupMenu)][±]]_ _[[elisp:(blee:menu-sel:navigation:popupMenu)][Ξ]]_ [[elisp:(outline-show-branches+toggle)][|=]] [[elisp:(bx:orgm:indirectBufOther)][|>]] *[[elisp:(blee:ppmm:org-mode-toggle)][|N]]*  Mtd-T-anyOrNone [[elisp:(outline-show-subtree+toggle)][||]] /cmndArgsSpec/ deco=default  deco=default  [[elisp:(org-cycle)][| ]]
    #+end_org """
    @cs.track(fnLoc=True, fnEntry=True, fnExit=True)
    def cmndArgsSpec(self, ):
####+END:
        """
***** Cmnd Args Specification
"""
        cmndArgsSpecDict = cs.CmndArgsSpecDict()
        cmndArgsSpecDict.argsDictAdd(
            argPosition="0",
            argName="repo",
            argDefault='',
            argChoices=[],
            argDescription="The package's repo or its py3 directory"
        )
        cmndArgsSpecDict.argsDictAdd(
            argPosition="1",
            argName="venv",
            argDefault='',
            argChoices=[],
            argDescription="The venv of the PyPI install; default /bisos/venv/py3/bisos3"
        )
        return cmndArgsSpecDict



####+BEGIN: b:py3:cs:cmnd/classHead :cmndName "pkgsStatus" :comment "" :extent "verify" :ro "cli" :parsMand "" :parsOpt "" :argsMin 0 :argsMax 1 :pyInv ""
""" #+begin_org
*  _[[elisp:(blee:menu-sel:outline:popupMenu)][±]]_ _[[elisp:(blee:menu-sel:navigation:popupMenu)][Ξ]]_ [[elisp:(outline-show-branches+toggle)][|=]] [[elisp:(bx:orgm:indirectBufOther)][|>]] *[[elisp:(blee:ppmm:org-mode-toggle)][|N]]*  CmndSvc-   [[elisp:(outline-show-subtree+toggle)][||]] <<pkgsStatus>>  =verify= argsMin=0 argsMax=1 ro=cli   [[elisp:(org-cycle)][| ]]
#+end_org """
class pkgsStatus(cs.Cmnd):
    cmndParamsMandatory = [ ]
    cmndParamsOptional = [ ]
    cmndArgsLen = {'Min': 0, 'Max': 1,}

    @cs.track(fnLoc=True, fnEntry=True, fnExit=True)
    def cmnd(self,
             rtInv: cs.RtInvoker,
             cmndOutcome: b.op.Outcome,
             argsList: typing.Optional[list[str]]=None,  # CsArgs
    ) -> b.op.Outcome:

        failed = b_io.eh.badOutcome
        callParamsDict = {}
        if self.invocationValidate(rtInv, cmndOutcome, callParamsDict, argsList).isProblematic():
            return failed(cmndOutcome)
        cmndArgsSpecDict = self.cmndArgsSpec()
####+END:
        if self.cmndDocStr(f""" #+begin_org
** [[elisp:(org-cycle)][| *CmndDesc:* | ]]  One row per package under base: in scope or not; for those in scope, both actions.
        #+end_org """): return(cmndOutcome)

        base = self.cmndArgsGet("0", cmndArgsSpecDict, argsList) or "/bisos/git/bxRepos/bisos-pip"
        rows = pipPkgsStatus.pkgsStatus(base)
        if not rows:
            return failed(cmndOutcome.set(opError=b.op.OpError.Failure, opErrInfo=f"{base}: no */py3/setup.py"))
        summary = _rowsSummary(rows)
        if rtInv.outs:
            _printRows(rows)  # the summary is printed as opResults
        return cmndOutcome.set(opResults=summary)

####+BEGIN: b:py3:cs:method/args :methodName "cmndArgsSpec" :methodType "anyOrNone" :retType "bool" :deco "default" :argsList "self"
    """ #+begin_org
**  _[[elisp:(blee:menu-sel:outline:popupMenu)][±]]_ _[[elisp:(blee:menu-sel:navigation:popupMenu)][Ξ]]_ [[elisp:(outline-show-branches+toggle)][|=]] [[elisp:(bx:orgm:indirectBufOther)][|>]] *[[elisp:(blee:ppmm:org-mode-toggle)][|N]]*  Mtd-T-anyOrNone [[elisp:(outline-show-subtree+toggle)][||]] /cmndArgsSpec/ deco=default  deco=default  [[elisp:(org-cycle)][| ]]
    #+end_org """
    @cs.track(fnLoc=True, fnEntry=True, fnExit=True)
    def cmndArgsSpec(self, ):
####+END:
        """
***** Cmnd Args Specification
"""
        cmndArgsSpecDict = cs.CmndArgsSpecDict()
        cmndArgsSpecDict.argsDictAdd(
            argPosition="0",
            argName="base",
            argDefault='',
            argChoices=[],
            argDescription="The directory of the package repos; default /bisos/git/bxRepos/bisos-pip"
        )
        return cmndArgsSpecDict



def _printStatus(status: pipPkgsStatus.Status, perAction: int = 5) -> None:
    """The action, then the reasons: at most perAction of each action, then a count."""
    print(f"{status.pkgName}: {status.action}   ({status.py3})")
    for action in reversed(status.order):
        texts = [text for each, text in status.reasons if each == action]
        for text in texts[:perAction]:
            print(f"  {action:12} {text}")
        if len(texts) > perAction:
            print(f"  {action:12} ... and {len(texts) - perAction} more")


def _printRows(rows: list[pipPkgsStatus.PkgRow]) -> None:
    """One line per package: name, scope, local action, release action; details by the per-package Cmnds."""
    print(f"{'package':28} {'scope':5} {'local':12} {'release':12} note")
    for row in rows:
        local = row.local.action if row.local else "-"
        release = row.release.action if row.release else "-"
        note = row.error or ("installed in the dev venv" if not row.inScope and row.installedInDev else "")
        print(f"{row.pkgName:28} {'in' if row.inScope else 'out':5} {local:12} {release:12} {note}")


def _rowsSummary(rows: list[pipPkgsStatus.PkgRow]) -> str:
    """How many packages need each action, on each side."""
    def counts(actions):
        found = {action: actions.count(action) for action in dict.fromkeys(actions) if action != "ok"}
        return ", ".join(f"{action} {count}" for action, count in sorted(found.items())) or "all ok"
    inScope = [row for row in rows if row.inScope and not row.error]
    errors = [row.pkgName for row in rows if row.error]
    return (f"{len(rows)} packages, {len(inScope)} in scope checked"
            f" --- local: {counts([row.local.action for row in inScope])}"
            f" --- release: {counts([row.release.action for row in inScope])}"
            + (f" --- errors: {' '.join(errors)}" if errors else ""))


####+BEGIN: blee:bxPanel:foldingSection :outLevel 0 :sep nil :title "Main" :anchor ""  :extraInfo "Framework DBlock"
""" #+begin_org
*  _[[elisp:(blee:menu-sel:outline:popupMenu)][±]]_ _[[elisp:(blee:menu-sel:navigation:popupMenu)][Ξ]]_ [[elisp:(outline-show-branches+toggle)][|=]] [[elisp:(bx:orgm:indirectBufOther)][|>]] *[[elisp:(blee:ppmm:org-mode-toggle)][|N]]*     [[elisp:(outline-show-subtree+toggle)][| _Main_: |]]  Framework DBlock  [[elisp:(org-shifttab)][<)]] E|
#+end_org """
####+END:

####+BEGIN: b:py3:cs:framework/main :csInfo "csInfo" :noCmndEntry "examples" :extraParamsHook "g_extraParams" :importedCmndsModules "g_importedCmndsModules"
""" #+begin_org
*  _[[elisp:(blee:menu-sel:outline:popupMenu)][±]]_ _[[elisp:(blee:menu-sel:navigation:popupMenu)][Ξ]]_ [[elisp:(outline-show-branches+toggle)][|=]] [[elisp:(bx:orgm:indirectBufOther)][|>]] *[[elisp:(blee:ppmm:org-mode-toggle)][|N]]*  CsFrmWrk   [[elisp:(outline-show-subtree+toggle)][||]] =g_csMain= (csInfo, _examples_, g_extraParams, g_importedCmndsModules)
#+end_org """

if __name__ == '__main__':
    cs.main.g_csMain(
        csInfo=csInfo,
        noCmndEntry=examples,  # specify a Cmnd name
        extraParamsHook=g_extraParams,
        importedCmndsModules=g_importedCmndsModules,
    )

####+END:

####+BEGIN: b:py3:cs:framework/endOfFile :basedOn "classification"
""" #+begin_org
* [[elisp:(org-cycle)][| *End-Of-Editable-Text* |]] :: emacs and org variables and control parameters
#+end_org """

#+STARTUP: showall

### local variables:
### no-byte-compile: t
### end:
####+END:
