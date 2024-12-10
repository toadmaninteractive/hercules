@ECHO OFF

pushd %~dp0

REM Command file for Sphinx documentation

if "%SPHINXBUILD%" == "" (
	set SPHINXBUILD=sphinx-build
)
set SOURCEDIR=ManualSource
set BUILDDIR=Manual
set SPHINXPROJ=HerculesHelp


%SPHINXBUILD% -b html %SOURCEDIR% %BUILDDIR% %SPHINXOPTS%

PAUSE