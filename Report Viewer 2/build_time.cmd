@echo off
for /F "usebackq tokens=1,2 delims==" %%i in (`wmic os get LocalDateTime /VALUE 2^>NUL`) do if '.%%i.'=='.LocalDateTime.' set ldt=%%j
set ldt=%ldt:~0,4%-%ldt:~4,2%-%ldt:~6,2% %ldt:~8,2%:%ldt:~10,2%

:: make a text file to remote
echo %ldt% > "\\10.10.10.3\Share\Tenny\Piwik-Matomo 文件與工具\Matomo 報告產生工具\build.txt"

:: print to standard output
echo namespace Report_Viewer_2 
echo { 
echo     public static class Build 
echo     { 
echo         public static string Timestamp = "%ldt%";
echo     }
echo }