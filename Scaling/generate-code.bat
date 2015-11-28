@echo off
cls
setlocal

if exist gen-csharp rmdir gen-csharp/S /Q 
for %%a in (IDL\*.thrift) do (
	echo %%a
	thrift -gen csharp %%a
)
if errorlevel 1 goto ERROR
goto EOF

:ERROR
echo %0: codegen failed
exit /b 1

:EOF
echo %0: codegen successful
exit /b 0
