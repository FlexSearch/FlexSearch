Invoke-Expression "npm install"
Invoke-Expression "npm -g install bower"
Invoke-Expression "npm -g install tsd"
Invoke-Expression "npm -g install gulp"
echo "Installing bower"
Invoke-Expression "bower install"
echo "Installing tsd"
Invoke-Expression "tsd update --save"
echo "Installing gulp"
Invoke-Expression gulp
Invoke-Expression "gulp swagger"
echo "Finished portal build script"

$lastexitcode