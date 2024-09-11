To upload the data by using the Telemetry_to_DB.zip file do these steps:
1. Unzip the files in your Users main directory fe. "C:\Users\<USERNAME>", you can chaeck what is your users main directory by typing "echo %USERPROFILE%" in Windows Command Prompt
2. Connect to the GL VPN using Cisco Secure Client app, that is installed on your GL laptop
3. Run Telemetry_to_DB batch file
4. If it doesn't work (connection without password requires a generated key that is different for every PC) that means that you need to connect to the RPi manually, by following these steps:
   
1. Open the powershell or cmd and type "ssh rpiApple@172.26.225.3"
2. Password for ssh is on the gmail chat
3. When you successfuly log in to the rpi go to the "Sending_telemetry" directory and type "sudo python3 app.py"
4. Go to the Telemetry_to_DB main project direcory in CMD and type "dotnet run", to run the project    
