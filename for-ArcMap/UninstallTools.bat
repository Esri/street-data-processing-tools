REM Copyright 2015 Esri
REM Licensed under the Apache License, Version 2.0 (the "License");
REM you may not use this file except in compliance with the License.
REM You may obtain a copy of the License at
REM       http://www.apache.org/licenses/LICENSE-2.0
REM Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on
REM an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
REM See the License for the specific language governing permissions and limitations under the License.

@Echo Off
Echo Unregistering GPProcessVendorDataFunctions.dll
set COMMONPROGRAMFILESLOCATION=%COMMONPROGRAMFILES%
IF NOT "%PROCESSOR_ARCHITECTURE%" == "x86" set COMMONPROGRAMFILESLOCATION=%COMMONPROGRAMFILES(x86)%
"%COMMONPROGRAMFILESLOCATION%\ArcGIS\bin\ESRIRegasm.exe" "%~dp0\GPProcessVendorDataFunctions.dll" /p:desktop /u