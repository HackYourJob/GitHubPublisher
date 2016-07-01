@echo off

if not exist .paket\paket.exe (
    @echo "Downloading Paket"
    .paket\paket.bootstrapper.exe
)

@echo "Restoring dependencies"
.paket\paket.exe restore

@echo "Build server"
set encoding=utf-8
packages\FAKE\tools\FAKE.exe build.local.fsx %*