; example1.nsi
;
; This script is perhaps one of the simplest NSIs you can make. All of the
; optional settings are left to their default settings. The installer simply 
; prompts the user asking them where to install, and drops a copy of example1.nsi
; there. 

;--------------------------------


!include "MUI2.nsh"
!include "x64.nsh"                  ; Macros for x64 machines

; The name of the installer
Name "CANopenEditor"

; The file to write
OutFile "CANopenEditor-Setup.exe"

; Show install details
ShowInstDetails show

; The default installation directory
InstallDir "$PROGRAMFILES\OpenEdsEditor\"

; Request application privileges for Windows Vista
;RequestExecutionLevel Admin

SetOverwrite on


!define MUI_HEADERIMAGE
!define MUI_HEADERIMAGE_BITMAP "${NSISDIR}\Contrib\Graphics\Header\nsis.bmp" ; optional
!define MUI_ABORTWARNING
  
;--------------------------------

; Pages


!insertmacro MUI_PAGE_LICENSE "License-GPLv3.txt"
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES



;Page instfiles

; The stuff to install
Section "OpenEdsEditor" Secopeneds ;No components page, name is not important

  ; Set output path to the installation directory.
  SetOutPath $INSTDIR
  
  ; Put file there
  File /r EDSEditorGUI2\bin\Release\net8.0\win-x64\publish\*
  File EDSEditorGUI\Index_8287_16x.ico
  File License-GPLv3.txt
   
  SetShellVarContext all
  CreateDirectory "$SMPROGRAMS\CANopenEditor"
  CreateShortCut "$SMPROGRAMS\CANopenEditor\CANopenEditor.lnk" $INSTDIR\EDSEditorGUI2.exe "" $INSTDIR\Index_8287_16x.ico 0
     
  ;Create uninstaller
  WriteUninstaller "$INSTDIR\Uninstall.exe"
  
  CreateShortCut "$SMPROGRAMS\CANopenEditor\Uninstall.lnk" $INSTDIR\Uninstall.exe
  
SectionEnd ; end the section

;Language strings
LangString DESC_Secopeneds ${LANG_ENGLISH} "The Open EDS editor"

 
!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
!insertmacro MUI_DESCRIPTION_TEXT ${Secopeneds} $(DESC_Secopeneds)
!insertmacro MUI_FUNCTION_DESCRIPTION_END


Function .onInit

  ;Extract InstallOptions files
  ;$PLUGINSDIR will automatically be removed when the installer closes
  
  InitPluginsDir
  
  Push $0
  Pop $0
  
FunctionEnd


Section "Uninstall"

  ;ADD YOUR OWN FILES HERE...
  
  Delete "$INSTDIR\*"
  Delete "$INSTDIR\Profiles\*"
  RMDir "$INSTDIR\Profiles"
  RMDir "$INSTDIR"
  
  SetShellVarContext all

  Delete "$SMPROGRAMS\OpenEDSEditor\OpenEDSEditor.lnk" 
  RMDir "$SMPROGRAMS\OpenEDSEditor"

SectionEnd


