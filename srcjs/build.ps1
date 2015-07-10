Invoke-Expression "npm install"
Invoke-Expression "bower install"
Invoke-Expression "tsd update --save"
Invoke-Expression gulp
echo "Finished portal build script"
