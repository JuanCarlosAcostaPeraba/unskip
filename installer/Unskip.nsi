Unicode True
ManifestSupportedOS Win10
RequestExecutionLevel user
SetCompressor /SOLID lzma

!include "MUI2.nsh"
!include "LogicLib.nsh"
!include "WinVer.nsh"
!include "x64.nsh"

!ifndef APP_VERSION
  !define APP_VERSION "0.1.0-dev"
!endif
!ifndef NUMERIC_VERSION
  !define NUMERIC_VERSION "0.1.0.0"
!endif
!ifndef SOURCE_DIR
  !define SOURCE_DIR "..\artifacts\publish\win-x64"
!endif
!ifndef OUTPUT_DIR
  !define OUTPUT_DIR "..\artifacts\release"
!endif

!define APP_NAME "Unskip"
!define APP_EXE "Unskip.App.exe"
!define APP_REGISTRY_KEY "Software\Unskip"
!define UNINSTALL_REGISTRY_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\Unskip"

Name "${APP_NAME} ${APP_VERSION}"
OutFile "${OUTPUT_DIR}\Unskip-${APP_VERSION}-win-x64-setup.exe"
InstallDir "$LOCALAPPDATA\Programs\Unskip"
InstallDirRegKey HKCU "${APP_REGISTRY_KEY}" "InstallLocation"
BrandingText "Unskip - local Windows messaging"
CRCCheck force
VIProductVersion "${NUMERIC_VERSION}"
VIAddVersionKey /LANG=1033 "ProductName" "${APP_NAME}"
VIAddVersionKey /LANG=1033 "ProductVersion" "${APP_VERSION}"
VIAddVersionKey /LANG=1033 "CompanyName" "Unskip contributors"
VIAddVersionKey /LANG=1033 "FileDescription" "Unskip per-user installer"
VIAddVersionKey /LANG=1033 "FileVersion" "${APP_VERSION}"
VIAddVersionKey /LANG=1033 "LegalCopyright" "Copyright (c) Unskip contributors"

!define MUI_ABORTWARNING
!define MUI_ICON "..\src\Unskip.App\Assets\unskip.ico"
!define MUI_UNICON "..\src\Unskip.App\Assets\unskip.ico"
!define MUI_FINISHPAGE_RUN "$INSTDIR\${APP_EXE}"
!define MUI_STARTMENUPAGE_DEFAULTFOLDER "Unskip"
!define MUI_STARTMENUPAGE_REGISTRY_ROOT HKCU
!define MUI_STARTMENUPAGE_REGISTRY_KEY "${APP_REGISTRY_KEY}"
!define MUI_STARTMENUPAGE_REGISTRY_VALUENAME "StartMenuFolder"

Var StartMenuFolder

Function .onInit
  ${IfNot} ${AtLeastWin10}
    MessageBox MB_OK|MB_ICONSTOP "Unskip requires Windows 10 or later.$\r$\nUnskip requiere Windows 10 o posterior."
    Abort
  ${EndIf}
  ${IfNot} ${RunningX64}
    MessageBox MB_OK|MB_ICONSTOP "Unskip requires 64-bit Windows.$\r$\nUnskip requiere Windows de 64 bits."
    Abort
  ${EndIf}
FunctionEnd

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "..\LICENSE"
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_STARTMENU Application $StartMenuFolder
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_LANGUAGE "English"
!insertmacro MUI_LANGUAGE "Spanish"

Section "Unskip application" ApplicationSection
  SectionIn RO
  SetShellVarContext current

  RMDir /r "$INSTDIR"
  SetOutPath "$INSTDIR"
  File /r "${SOURCE_DIR}\*.*"
  WriteUninstaller "$INSTDIR\Uninstall.exe"

  WriteRegStr HKCU "${APP_REGISTRY_KEY}" "InstallLocation" "$INSTDIR"
  WriteRegStr HKCU "${UNINSTALL_REGISTRY_KEY}" "DisplayName" "${APP_NAME}"
  WriteRegStr HKCU "${UNINSTALL_REGISTRY_KEY}" "DisplayVersion" "${APP_VERSION}"
  WriteRegStr HKCU "${UNINSTALL_REGISTRY_KEY}" "DisplayIcon" "$INSTDIR\${APP_EXE}"
  WriteRegStr HKCU "${UNINSTALL_REGISTRY_KEY}" "Publisher" "Unskip contributors"
  WriteRegStr HKCU "${UNINSTALL_REGISTRY_KEY}" "URLInfoAbout" "https://github.com/JuanCarlosAcostaPeraba/unskip"
  WriteRegStr HKCU "${UNINSTALL_REGISTRY_KEY}" "HelpLink" "https://github.com/JuanCarlosAcostaPeraba/unskip/issues"
  WriteRegStr HKCU "${UNINSTALL_REGISTRY_KEY}" "UninstallString" '"$INSTDIR\Uninstall.exe"'
  WriteRegStr HKCU "${UNINSTALL_REGISTRY_KEY}" "QuietUninstallString" '"$INSTDIR\Uninstall.exe" /S'
  WriteRegDWORD HKCU "${UNINSTALL_REGISTRY_KEY}" "NoModify" 1
  WriteRegDWORD HKCU "${UNINSTALL_REGISTRY_KEY}" "NoRepair" 1

  !insertmacro MUI_STARTMENU_WRITE_BEGIN Application
    CreateDirectory "$SMPROGRAMS\$StartMenuFolder"
    CreateShortcut "$SMPROGRAMS\$StartMenuFolder\Unskip.lnk" "$INSTDIR\${APP_EXE}"
    CreateShortcut "$SMPROGRAMS\$StartMenuFolder\Uninstall Unskip.lnk" "$INSTDIR\Uninstall.exe"
  !insertmacro MUI_STARTMENU_WRITE_END
SectionEnd

Section /o "Desktop shortcut" DesktopShortcutSection
  SetShellVarContext current
  CreateShortcut "$DESKTOP\Unskip.lnk" "$INSTDIR\${APP_EXE}"
SectionEnd

LangString DESC_ApplicationSection ${LANG_ENGLISH} "Install the Unskip desktop application."
LangString DESC_ApplicationSection ${LANG_SPANISH} "Instala la aplicación de escritorio Unskip."
LangString DESC_DesktopShortcutSection ${LANG_ENGLISH} "Create a shortcut on the desktop."
LangString DESC_DesktopShortcutSection ${LANG_SPANISH} "Crea un acceso directo en el escritorio."

!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
  !insertmacro MUI_DESCRIPTION_TEXT ${ApplicationSection} $(DESC_ApplicationSection)
  !insertmacro MUI_DESCRIPTION_TEXT ${DesktopShortcutSection} $(DESC_DesktopShortcutSection)
!insertmacro MUI_FUNCTION_DESCRIPTION_END

Section "Uninstall"
  SetShellVarContext current
  Delete "$DESKTOP\Unskip.lnk"

  !insertmacro MUI_STARTMENU_GETFOLDER Application $StartMenuFolder
  Delete "$SMPROGRAMS\$StartMenuFolder\Unskip.lnk"
  Delete "$SMPROGRAMS\$StartMenuFolder\Uninstall Unskip.lnk"
  RMDir "$SMPROGRAMS\$StartMenuFolder"

  DeleteRegKey HKCU "${UNINSTALL_REGISTRY_KEY}"
  DeleteRegKey HKCU "${APP_REGISTRY_KEY}"
  RMDir /r "$INSTDIR"
SectionEnd
